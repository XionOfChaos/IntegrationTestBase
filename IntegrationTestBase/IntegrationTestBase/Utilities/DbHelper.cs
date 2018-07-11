using System.Data.SqlClient;
using System.IO;
using System.Configuration;

namespace IntegrationTestBase
{
    public static class DbHelper
    {
        private static readonly string MasterDatabaseConnectionString;

        static DbHelper()
        {
            MasterDatabaseConnectionString = GenerateDatabaseConnectionString("master");
        }

        /// <summary>
        /// Generates and returns a database name for use as the database.  
        /// This name will be unique.
        /// </summary>
        /// <returns></returns>
        internal static string GenerateTestDatabaseName()
        {
            // custom unique database name used per test fixture
            string dbName = "TestDB_" + Path.GetTempFileName();
            return dbName;
        }

        public static string GenerateDatabaseConnectionString(string databaseName)
        {
            var baseConnString = ConfigurationManager.ConnectionStrings["IntegrationTests.Base"].ConnectionString;
            var cnb = new SqlConnectionStringBuilder(baseConnString) {InitialCatalog = databaseName};
            return cnb.ToString();
        }

        /// <summary>
        /// Restores the given db name from the backup file.  This will terminate any connections to the db.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="fullSourcePath"></param>
        public static void RestoreDatabase(string dbName, string fullSourcePath)
        {
            using (var cn = new SqlConnection(MasterDatabaseConnectionString))
            using (var cm = cn.CreateCommand())
            {
                cn.Open();
                cm.Parameters.AddWithValue("@dbName", dbName);
                var sqlDbName = dbName.Replace("]", "]]");

                //Get the logical source filenames.
                var sourceDataFileName = "";
                var sourceLogFileName = "";

                cm.CommandText = "RESTORE FILELISTONLY FROM DISK = N'" + fullSourcePath + "'";
                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string fileName = dr.GetString(0);
                        string type = dr.GetString(2);

                        if (type == "D")
                            sourceDataFileName = fileName;
                        else
                            sourceLogFileName = fileName;
                    }
                }

                //Create DB if not found.
                cm.CommandText = string.Format(
                   "IF NOT EXISTS (SELECT * FROM sys.databases WHERE Name = @dbName) CREATE DATABASE [{0}]", sqlDbName);
                cm.ExecuteNonQuery();

                //Get the paths to the dest db data/log files.
                cm.CommandText = @"
SELECT f.type_desc, f.physical_name 
FROM sys.databases d 
JOIN sys.master_files f ON d.database_id = f.database_id
WHERE d.Name = @dbName
";
                string destDataFilePath = "";
                string destLogFilePath = "";

                using (var dr = cm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string type = dr.GetString(0);
                        string filePath = dr.GetString(1);

                        if (type == "ROWS")
                            destDataFilePath = filePath;
                        else
                            destLogFilePath = filePath;
                    }
                }

                //Set single user.. Probably shouldn't have to do this?...As long as we call ClearAllPools in the
                //post integration test handler, to ensure that pooled connections aren't held between tests.
                cm.CommandText = string.Format(
                   "IF EXISTS (SELECT * FROM sys.databases WHERE Name = @dbName) ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", sqlDbName);
                cm.ExecuteNonQuery();

                //Restore it.
                cm.CommandText = @"RESTORE DATABASE [" + sqlDbName + "] FROM DISK = N'" + fullSourcePath + "' " +
                   "WITH REPLACE, FILE = 1, " +
                   "MOVE N'" + sourceDataFileName + "' TO N'" + destDataFilePath + "', " +
                   "MOVE N'" + sourceLogFileName + "' TO N'" + destLogFilePath + "' ";
                cm.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Backs up the given db to the given backup file.  If the folder for the file doesn't exist, it is created.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="fullDestPath"></param>
        public static void BackupDatabase(string dbName, string fullDestPath)
        {
            //Ensure the full dest path exists
            var dir = Path.GetDirectoryName(fullDestPath);
            Directory.CreateDirectory(dir);

            using (var cn = new SqlConnection(MasterDatabaseConnectionString))
            using (var cm = cn.CreateCommand())
            {
                cn.Open();

                cm.CommandText = @"BACKUP DATABASE [" + dbName.Replace("]", "]]") + "] TO DISK = N'" + fullDestPath + "'";
                cm.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Deletes the given database.
        /// </summary>
        /// <param name="dbName"></param>
        public static void DeleteDatabase(string dbName)
        {
            using (var cn = new SqlConnection(MasterDatabaseConnectionString))
            using (var cm = cn.CreateCommand())
            {
                cn.Open();
                cm.Parameters.AddWithValue("@dbName", dbName);
                string sqlDbName = dbName.Replace("]", "]]");
                cm.CommandText = string.Format("IF EXISTS (SELECT * FROM sys.databases WHERE Name = @dbName) ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", sqlDbName);
                cm.ExecuteNonQuery();
                cm.CommandText = string.Format("IF EXISTS (SELECT * FROM sys.databases WHERE Name = @dbName) DROP DATABASE [{0}]", sqlDbName);
                cm.ExecuteNonQuery();
            }
        }
    }
}
