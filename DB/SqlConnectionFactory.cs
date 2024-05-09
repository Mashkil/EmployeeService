using Microsoft.Data.Sqlite;

namespace EmployeeService.DB
{
    public class SqlConnectionFactory
    {
        private readonly string connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(connectionString);
        }
    }
}