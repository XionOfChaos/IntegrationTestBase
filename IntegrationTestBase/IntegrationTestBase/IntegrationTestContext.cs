using System;

namespace IntegrationTestBase
{
    /// <summary>
    /// Abstract class to hold your Test Fixture IOC 
    /// </summary>
    public abstract class IntegrationTestContext : IIntegrationTestContext
    {
        protected string _dbName;

        public void Initialize(string databaseName)
        {
            _dbName = databaseName;
        }


        public virtual void Dispose()
        {
            
        }
    }
}
