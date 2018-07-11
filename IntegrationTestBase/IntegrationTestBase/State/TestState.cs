using System;
using System.IO;

namespace IntegrationTestBase.State
{
    public abstract class TestState
    {
        private string _dbName;        
        private string _stateBackupFolder;

        public class BaseArgs 
        {
            public string DatabaseName { get; set; }
        }

        /// <summary>
        /// Creates a state controller that is hooked to the given db name.
        /// </summary>
        /// <param name="dbName"></para>
        public TestState(BaseArgs args)
        {
            _dbName = args.DatabaseName;            
            _stateBackupFolder = Path.Combine(IntegrationTestBase.GetTestTempBasePath(), "DbStateBackups");
        }

        public void RestoreToState(bool forceRecreate = false, bool backupAfterCreate = true)
        {
            var backupFilePath = Path.Combine(_stateBackupFolder, $"{GetType().FullName}.bak");
            if (!forceRecreate && File.Exists(backupFilePath))
            {
                DbHelper.RestoreDatabase(_dbName, backupFilePath);
            }
            else
            {
                //State didn't exist, create it.                
                CreateState();
                                
                if (backupAfterCreate)                
                    DbHelper.BackupDatabase(_dbName, backupFilePath);
                
            }
        }

        /// <summary>
        /// Override to create the additional items required for this state.  Call base.CreateState first, to ensure any additional state
        /// gets created.
        /// </summary>
        protected virtual void CreateState()
        {
        }

        /// <summary>
        /// Override to load any helper entities.  This should be called after a context has already been created, and will just populate
        /// any member variables for the loaded state.
        /// </summary>
        public virtual void LoadStateEntities()
        {
        }

        /// <summary>
        /// Helper to instantiate and restore state, using a random test db name.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        public static void RestoreState<TState>() where TState : TestState
        {
            string dbName = DbHelper.GenerateTestDatabaseName();
            TestState testState = (TestState)Activator.CreateInstance(typeof(TState), dbName);
            testState.RestoreToState();
        }
    }
}
