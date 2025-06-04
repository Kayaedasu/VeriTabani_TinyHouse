using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using TinyHouse.Models;
using TinyHouse.Data;

namespace TinyHouse.Repositories
{
    public class RezervasyonRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public RezervasyonRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Ev sahibine ait rezervasyonları getirir
        public async Task<IEnumerable<EvSahibiRezervasyon>> GetByEvSahibiIdAsync(int evSahibiId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
        SELECT 
            r.*, 
            rd.DurumAdi, 
            CONCAT(k.Ad, ' ', k.Soyad) AS KiraciAdi,
            t.EvSahibiID,
            (SELECT TOP 1 FotoUrl FROM EvFoto WHERE EvID = t.EvID) AS EvFotoUrl
        FROM Rezervasyon r
        INNER JOIN TinyHouse t ON r.EvID = t.EvID
        INNER JOIN RezervasyonDurum rd ON r.DurumID = rd.DurumID
        INNER JOIN Kiraci kc ON r.KiraciID = kc.KiraciID
        INNER JOIN Kullanici k ON kc.KiraciID = k.KullaniciID
        WHERE t.EvSahibiID = @EvSahibiID
        ORDER BY r.OlusturmaTarihi DESC";

            return await conn.QueryAsync<EvSahibiRezervasyon>(sql, new { EvSahibiID = evSahibiId });
        }


        // ID'ye göre rezervasyon getirir (EvSahibiID'yi JOIN ile getiriyoruz)
        public async Task<Rezervasyon> GetByIdAsync(int id)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
        SELECT 
            r.*, 
            rd.DurumAdi,
            CONCAT(k.Ad, ' ', k.Soyad) AS KiraciAdi,
            t.EvSahibiID,
            (SELECT TOP 1 FotoUrl FROM EvFoto WHERE EvID = t.EvID) AS EvFotoUrl
        FROM Rezervasyon r
        INNER JOIN TinyHouse t ON r.EvID = t.EvID
        INNER JOIN RezervasyonDurum rd ON r.DurumID = rd.DurumID
        INNER JOIN Kiraci kc ON r.KiraciID = kc.KiraciID
        INNER JOIN Kullanici k ON kc.KiraciID = k.KullaniciID
        WHERE r.RezervasyonID = @RezervasyonID";

            return await conn.QuerySingleOrDefaultAsync<Rezervasyon>(sql, new { RezervasyonID = id });
        }


        // Rezervasyon durumunu günceller
        public async Task GuncelleDurumAsync(int rezervasyonId, string yeniDurumAdi)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string durumIdSorgu = "SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi = @DurumAdi";
            int durumId = await conn.QuerySingleOrDefaultAsync<int>(durumIdSorgu, new { DurumAdi = yeniDurumAdi });

            string sql = "UPDATE Rezervasyon SET DurumID = @DurumID WHERE RezervasyonID = @RezervasyonID";
            await conn.ExecuteAsync(sql, new { DurumID = durumId, RezervasyonID = rezervasyonId });
        }

        // EvID'ye göre rezervasyonları getirir
        public async Task<IEnumerable<EvSahibiRezervasyon>> GetByEvIdAsync(int evId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT 
                    r.*, 
                    rd.DurumAdi, 
                    CONCAT(k.Ad, ' ', k.Soyad) AS KiraciAdi,
                    t.EvSahibiID,
                    (SELECT TOP 1 FotoUrl FROM EvFoto WHERE EvID = t.EvID) AS EvFotoUrl
                FROM Rezervasyon r
                INNER JOIN TinyHouse t ON r.EvID = t.EvID
                INNER JOIN RezervasyonDurum rd ON r.DurumID = rd.DurumID
                INNER JOIN Kiraci kc ON r.KiraciID = kc.KiraciID
                INNER JOIN Kullanici k ON kc.KiraciID = k.KullaniciID
                WHERE t.EvID = @EvID
                ORDER BY r.OlusturmaTarihi DESC";

            return await conn.QueryAsync<EvSahibiRezervasyon>(sql, new { EvID = evId });
        }

        // Ödeme durumunu günceller
        public async Task OdemeDurumuGuncelleAsync(int rezervasyonId, bool odendi)
        {
            using var conn = _connectionFactory.CreateConnection();

            string sql = "UPDATE Rezervasyon SET OdemeDurumu = @Odendi WHERE RezervasyonID = @RezervasyonID";
            await conn.ExecuteAsync(sql, new { Odendi = odendi, RezervasyonID = rezervasyonId });
        }
    }
}
