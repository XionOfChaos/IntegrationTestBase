using System;

namespace IntegrationTestBase
{
    public interface IIntegrationTestContext : IDisposable
    {
        void Initialize(string databaseName);
    }
}
