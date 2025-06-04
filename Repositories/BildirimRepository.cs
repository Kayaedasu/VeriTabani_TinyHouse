using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using TinyHouse.Models;
using TinyHouse.Data;

namespace TinyHouse.Repositories
{
    public class BildirimRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public BildirimRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Kullanıcıya ait bildirimleri listele
        public async Task<IEnumerable<Bildirim>> GetByKullaniciIdAsync(int kullaniciId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT BildirimID, KullaniciID, Baslik, Mesaj, Okundu, OlusturmaTarihi
                FROM Bildirim
                WHERE KullaniciID = @KullaniciID
                ORDER BY OlusturmaTarihi DESC";

            return await conn.QueryAsync<Bildirim>(sql, new { KullaniciID = kullaniciId });
        }

        // Bildirim ekle
        public async Task AddAsync(Bildirim bildirim)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
                INSERT INTO Bildirim (KullaniciID, Baslik, Mesaj, Okundu, OlusturmaTarihi)
                VALUES (@KullaniciID, @Baslik, @Mesaj, @Okundu, @OlusturmaTarihi)";

            await conn.ExecuteAsync(sql, bildirim);
        }

        // Bildirimi okundu yap (güncelle)
        public async Task MarkAsReadAsync(int bildirimId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = "UPDATE Bildirim SET Okundu = 1 WHERE BildirimID = @BildirimID";

            await conn.ExecuteAsync(sql, new { BildirimID = bildirimId });
        }
    }
}
