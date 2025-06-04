using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using TinyHouse.Models;
using TinyHouse.Data;

namespace TinyHouse.Repositories
{
    public class KonumRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public KonumRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<Konum> GetAll()
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Konum";
            return conn.Query<Konum>(sql);
        }
    }
}
