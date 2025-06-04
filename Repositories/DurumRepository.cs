using System.Collections.Generic;
using System.Data;
using Dapper;
using TinyHouse.Models;
using TinyHouse.Data;

namespace TinyHouse.Repositories
{
    public class DurumRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public DurumRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<TinyHouseDurum> GetAll()
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM TinyHouseDurum";
            return conn.Query<TinyHouseDurum>(sql);
        }
    }
}
