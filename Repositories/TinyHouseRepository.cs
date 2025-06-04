using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TinyHouse.Models;
using TinyHouse.Data;

namespace TinyHouse.Repositories
{
    public class TinyHouseRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public TinyHouseRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Tüm TinyHouse'ları getir (Filtre yok)
        public async Task<IEnumerable<EvSahibiTinyHouse>> GetAllAsync()
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM TinyHouse";
            return await conn.QueryAsync<EvSahibiTinyHouse>(sql);
        }

        // ID'ye göre tek bir TinyHouse'ı getir
        public async Task<EvSahibiTinyHouse> GetByIdAsync(int id)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM TinyHouse WHERE EvID = @EvID";
            return await conn.QuerySingleOrDefaultAsync<EvSahibiTinyHouse>(sql, new { EvID = id });
        }

        // Yeni bir TinyHouse ekle
        public async Task<int> AddAsync(EvSahibiTinyHouse ev)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO TinyHouse (EvSahibiID, KonumID, DurumID, Baslik, Aciklama, Fiyat, EklenmeTarihi)
                VALUES (@EvSahibiID, @KonumID, @DurumID, @Baslik, @Aciklama, @Fiyat, @EklenmeTarihi);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var evId = await conn.QuerySingleAsync<int>(sql, ev);
            return evId;
        }

        // TinyHouse'u sil
        public async Task DeleteAsync(int evId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "DELETE FROM TinyHouse WHERE EvID = @EvID";
            await conn.ExecuteAsync(sql, new { EvID = evId });
        }

        // TinyHouse'u güncelle
        public async Task UpdateAsync(EvSahibiTinyHouse ev)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = @"
                UPDATE TinyHouse
                SET KonumID = @KonumID,
                    DurumID = @DurumID,
                    Baslik = @Baslik,
                    Aciklama = @Aciklama,
                    Fiyat = @Fiyat
                WHERE EvID = @EvID";
            await conn.ExecuteAsync(sql, ev);
        }

        // Ev fotoğraflarını EvID'ye göre getir
        public async Task<IEnumerable<EvFoto>> GetFotosByEvIdAsync(int evId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM EvFoto WHERE EvID = @EvID";
            return await conn.QueryAsync<EvFoto>(sql, new { EvID = evId });
        }

        // Yeni bir fotoğraf ekle
        public async Task EvFotoEkleAsync(EvFoto foto)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = "INSERT INTO EvFoto (EvID, FotoUrl) VALUES (@EvID, @FotoUrl)";
            await conn.ExecuteAsync(sql, foto);
        }

        // Ev fotoğraflarını sil
        public async Task EvFotoSilAsync(int evId)
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();

            var fotos = await conn.QueryAsync<EvFoto>("SELECT * FROM EvFoto WHERE EvID = @EvID", new { EvID = evId });
            foreach (var foto in fotos)
            {
                var dosyaYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", foto.FotoUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (File.Exists(dosyaYolu))
                {
                    File.Delete(dosyaYolu);
                }
            }

            string sql = "DELETE FROM EvFoto WHERE EvID = @EvID";
            await conn.ExecuteAsync(sql, new { EvID = evId });
        }

        // Ortalama puanları al
        public async Task<Dictionary<int, double>> GetOrtalamaPuanlarAsync()
        {
            using IDbConnection conn = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT R.EvID, AVG(CAST(Y.Puan AS FLOAT)) AS OrtalamaPuan
                FROM Yorum Y
                INNER JOIN Rezervasyon R ON Y.RezervasyonID = R.RezervasyonID
                GROUP BY R.EvID";

            var result = await conn.QueryAsync<(int EvID, double OrtalamaPuan)>(sql);
            return result.ToDictionary(x => x.EvID, x => x.OrtalamaPuan);
        }
    }
}
