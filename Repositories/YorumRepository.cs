using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using TinyHouse.Models;
using TinyHouse.Data;

namespace TinyHouse.Repositories
{
    public class YorumRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public YorumRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Ev sahibinin ilanlarına ait yorumları ve varsa cevaplarını getirir
        public async Task<IEnumerable<Yorum>> GetYorumlarByEvSahibiAsync(int evSahibiId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT 
                    Y.*, 
                    YC.CevapID, 
                    YC.CevapMetni, 
                    YC.CevapTarihi
                FROM Yorum Y
                INNER JOIN Rezervasyon R ON Y.RezervasyonID = R.RezervasyonID
                INNER JOIN TinyHouse T ON R.EvID = T.EvID
                LEFT JOIN YorumCevap YC ON Y.YorumID = YC.YorumID
                WHERE T.EvSahibiID = @EvSahibiID
                ORDER BY Y.YorumTarihi DESC";

            return await conn.QueryAsync<Yorum, YorumCevap, Yorum>(
                sql,
                (yorum, cevap) =>
                {
                    yorum.Cevap = cevap;
                    return yorum;
                },
                new { EvSahibiID = evSahibiId },
                splitOn: "CevapID");
        }


        // Yeni cevap eklemek için
        public async Task YorumCevapEkleAsync(YorumCevap cevap)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
                INSERT INTO YorumCevap (YorumID, EvSahibiID, CevapMetni, CevapTarihi)
                VALUES (@YorumID, @EvSahibiID, @CevapMetni, GETDATE())";

            await conn.ExecuteAsync(sql, cevap);
        }

        // İlanda bulunan yorumları getirir
        public async Task<IEnumerable<Yorum>> GetByEvIdAsync(int evId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT 
                    Y.*, 
                    YC.CevapID, 
                    YC.CevapMetni, 
                    YC.CevapTarihi
                FROM Yorum Y
                LEFT JOIN YorumCevap YC ON Y.YorumID = YC.YorumID
                INNER JOIN Rezervasyon R ON Y.RezervasyonID = R.RezervasyonID
                WHERE R.EvID = @EvID
                ORDER BY Y.YorumTarihi DESC";

            return await conn.QueryAsync<Yorum, YorumCevap, Yorum>(
                sql,
                (yorum, cevap) =>
                {
                    yorum.Cevap = cevap;
                    return yorum;
                },
                new { EvID = evId },
                splitOn: "CevapID");
        }
        // YorumID'ye göre tek yorum getirir
        public async Task<Yorum?> GetByIdAsync(int yorumId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            string sql = @"
        SELECT 
            Y.*, 
            YC.CevapID, 
            YC.CevapMetni, 
            YC.CevapTarihi
        FROM Yorum Y
        LEFT JOIN YorumCevap YC ON Y.YorumID = YC.YorumID
        WHERE Y.YorumID = @YorumID";

            var result = await conn.QueryAsync<Yorum, YorumCevap, Yorum>(
                sql,
                (yorum, cevap) =>
                {
                    yorum.Cevap = cevap;
                    return yorum;
                },
                new { YorumID = yorumId },
                splitOn: "CevapID");

            return result.FirstOrDefault();
        }

    }
}
