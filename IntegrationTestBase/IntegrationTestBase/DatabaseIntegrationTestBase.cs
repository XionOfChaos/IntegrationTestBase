using System;
using System.IO;
using IntegrationTestBase.State;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace IntegrationTestBase
{
    /// <summary>
    /// Base class for organisation level integration tests that require a database.
    /// The class will generate a unique org level database name for the test class, with all tests within
    /// the class sharing the same database.  If a unique database is required per test, create a new
    /// test class for each individual test.
    /// 
    /// If the test fails, the database will be backed up and copied to the TestArtifcats folder.
    /// The database will always be deleted after the test run.
    /// 
    /// For this reason the local user must have database create/drop/backup rights on the server used.
    /// </summary>
    public class DatabaseIntegrationTestBase : IntegrationTestBase
    {
        protected string DatabaseName { get; private set; }
        protected string DatabaseConnectionString { get; private set; }

        /// <summary>
        /// If true, this suite of tests initialises the db before each test is run.
        /// If false, db is only initialised for first test.  Note that the db name will then be
        /// the same for all tests in the class, and will be based on the first test to run.  Probably
        /// should be refactored so if using shared db, it's based on fixture (class) name not test.
        /// Defaults to false.
        /// </summary>
        /// <remarks>
        /// TODO: this should probaly default to true, most tests want total isolation.
        /// </remarks>
        protected virtual bool InitIntegrationPerTest => true;

        [TestFixtureSetUp]
        public virtual void SetupFixture()
        {
            if (!InitIntegrationPerTest)
                SetupIntegration();
        }

        [SetUp]
        public virtual void Setup()
        {
            if (InitIntegrationPerTest)
                SetupIntegration();
        }

        /// <summary>
        /// Override this one to do any pre integration test work.  Note that this will
        /// be called either before each test, or before each fixture, based on the InitPerTest value.
        /// </summary>
        protected virtual void SetupIntegration()
        {
            DatabaseName = DbHelper.GenerateTestDatabaseName();
            File.WriteAllText(Path.Combine(GetArtifactFolder(), "DBInfo.txt"), "Test DB: " + DatabaseName);
            DatabaseConnectionString = DbHelper.GenerateDatabaseConnectionString(DatabaseName);
        }


        [TestFixtureTearDown]
        protected virtual void TearDownFixture()
        {
            if (!InitIntegrationPerTest)
                TearDownIntegration();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            if (InitIntegrationPerTest)
                TearDownIntegration();
        }

        /// <summary>
        /// Override to do any post integration test work.  This follows same rules as SetupIntegration.
        /// </summary>
        protected virtual void TearDownIntegration()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                BackupDatabaseToTestArtifactsFolder();
            }
            DbHelper.DeleteDatabase(DatabaseName);
        }

        /// <summary>
        /// Generates a connection string element in XML to connect to a database using the supplied connection string.
        /// </summary>
        protected string GenerateDatabaseConnectionStringElement(string databaseName)
        {
            var connString = string.Format(
               "<add name=\"IntegrationTests\" connectionString=\"{0}\" providerName=\"System.Data.SqlClient\" />",
               DbHelper.GenerateDatabaseConnectionString(databaseName));
            return connString;
        }

        /// <summary>
        /// Creates a backup of the database and copies the backup to the TestArtifacts folder.
        /// </summary>
        protected void BackupDatabaseToTestArtifactsFolder()
        {
            string backupFilename = DatabaseName + ".bak";
            string backupPath = GetArtifactFolder();
            backupFilename = Path.Combine(backupPath, backupFilename);

            DbHelper.BackupDatabase(DatabaseName, backupFilename);
        }

        /// <summary>
        /// Restores the db to a given state.  Should only be called at the start of the test, before anything has been run.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        protected TState RestoreState<TState>() where TState : TestState
        {
            return (TState)RestoreState(typeof(TState));
        }

        /// <summary>
        /// Restores the db to a given state.  Should only be called at the start of the test, before anything has been run.
        /// </summary>
        protected TestState RestoreState(Type stateType)
        {
            TestState state = (TestState)Activator.CreateInstance(stateType, new TestState.BaseArgs { DatabaseName = DatabaseName});
            state.RestoreToState();
            return state;
        }


        /// <summary>
        /// Creates a context with the given state
        /// </summary>
        /// <param name="orgCode"></param>
        /// <returns></returns>
        protected ContextAndStateContainer<TState> CreateTestContext<TState>(IIntegrationTestContext context) where TState : TestState
        {
            var state = RestoreState<TState>();            
            context.Initialize(DatabaseName);

            state.LoadStateEntities();
            return new ContextAndStateContainer<TState>(context, state);
        }

        protected class ContextAndStateContainer<TState> : IDisposable where TState : TestState
        {
            public IIntegrationTestContext Context { get; private set; }
            public TState State { get; private set; }

            public ContextAndStateContainer(IIntegrationTestContext context, TState state)
            {
                State = state;
                Context = context;
            }

            public void Dispose()
            {
                Context.Dispose();
            }
        }

    }
}
