using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace TinyHouse.Data
{
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        // IConfiguration otomatik DI ile gelir
        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("Connection string is missing or invalid.");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
