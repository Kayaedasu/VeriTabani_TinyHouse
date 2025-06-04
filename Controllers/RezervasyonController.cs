using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;


namespace TinyHouse.Controllers
{
    public class RezervasyonController : Controller
    {
        private readonly IConfiguration _configuration;

        public RezervasyonController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Rezervasyonları listele
        public IActionResult Index()
        {
            string? connStrNullable = _configuration.GetConnectionString("DefaultConnection");
            if (connStrNullable == null)
                throw new InvalidOperationException("DefaultConnection connection string bulunamadı.");
            string connStr = connStrNullable;

            var rezervasyonlar = new List<RezervasyonViewModel>();

            try
            {
                using var conn = new SqlConnection(connStr);
                conn.Open();

                string sql = @"
                    SELECT 
                        r.RezervasyonID, 
                        r.BaslangicTarihi, 
                        r.BitisTarihi, 
                        CASE WHEN o.OdemeID IS NULL THEN 0 ELSE 1 END AS OdemeYapildiMi,
                        r.DurumID,
                        d.DurumAdi,
                        k.KullaniciID, 
                        k.Ad AS KiraciAdi, 
                        k.Soyad AS KiraciSoyadi,
                        t.EvID, 
                        t.Baslik AS EvBasligi
                    FROM Rezervasyon r
                    INNER JOIN Kullanici k ON r.KiraciID = k.KullaniciID
                    INNER JOIN TinyHouse t ON r.EvID = t.EvID
                    LEFT JOIN Odeme o ON r.RezervasyonID = o.RezervasyonID
                    INNER JOIN RezervasyonDurum d ON r.DurumID = d.DurumID
                    ORDER BY r.RezervasyonID DESC";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    rezervasyonlar.Add(new RezervasyonViewModel
                    {
                        RezervasyonID = reader.GetInt32(reader.GetOrdinal("RezervasyonID")),
                        BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("BaslangicTarihi")),
                        BitisTarihi = reader.GetDateTime(reader.GetOrdinal("BitisTarihi")),
                        OdemeYapildiMi = reader.GetInt32(reader.GetOrdinal("OdemeYapildiMi")) == 1,
                        DurumID = reader.GetInt32(reader.GetOrdinal("DurumID")),
                        DurumAdi = reader.GetString(reader.GetOrdinal("DurumAdi")),
                        KiraciID = reader.GetInt32(reader.GetOrdinal("KullaniciID")),
                        KiraciAdi = !reader.IsDBNull(reader.GetOrdinal("KiraciAdi")) ? reader.GetString(reader.GetOrdinal("KiraciAdi")) : string.Empty,
                        KiraciSoyadi = !reader.IsDBNull(reader.GetOrdinal("KiraciSoyadi")) ? reader.GetString(reader.GetOrdinal("KiraciSoyadi")) : string.Empty,
                        EvID = reader.GetInt32(reader.GetOrdinal("EvID")),
                        EvBasligi = !reader.IsDBNull(reader.GetOrdinal("EvBasligi")) ? reader.GetString(reader.GetOrdinal("EvBasligi")) : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                // Loglama yapılabilir
                TempData["ErrorMessage"] = "Rezervasyonlar yüklenirken hata oluştu: " + ex.Message;
                rezervasyonlar = new List<RezervasyonViewModel>();
            }

            return View("Index", rezervasyonlar);
        }

        // İptal onay sayfası (GET)
        public IActionResult IptalOnay(int id)
        {
            string? connStrNullable = _configuration.GetConnectionString("DefaultConnection");
            if (connStrNullable == null)
                throw new InvalidOperationException("DefaultConnection connection string bulunamadı.");
            string connStr = connStrNullable;

            RezervasyonViewModel? rezervasyon = null;

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string sql = @"
                SELECT 
                    r.RezervasyonID, 
                    r.BaslangicTarihi, 
                    r.BitisTarihi, 
                    CASE WHEN o.OdemeID IS NULL THEN 0 ELSE 1 END AS OdemeYapildiMi,
                    r.DurumID,
                    d.DurumAdi,
                    k.KullaniciID, 
                    k.Ad AS KiraciAdi, 
                    k.Soyad AS KiraciSoyadi,
                    t.EvID, 
                    t.Baslik AS EvBasligi
                FROM Rezervasyon r
                INNER JOIN Kullanici k ON r.KiraciID = k.KullaniciID
                INNER JOIN TinyHouse t ON r.EvID = t.EvID
                LEFT JOIN Odeme o ON r.RezervasyonID = o.RezervasyonID
                INNER JOIN RezervasyonDurum d ON r.DurumID = d.DurumID
                WHERE r.RezervasyonID = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                rezervasyon = new RezervasyonViewModel
                {
                    RezervasyonID = reader.GetInt32(reader.GetOrdinal("RezervasyonID")),
                    BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("BaslangicTarihi")),
                    BitisTarihi = reader.GetDateTime(reader.GetOrdinal("BitisTarihi")),
                    OdemeYapildiMi = reader.GetInt32(reader.GetOrdinal("OdemeYapildiMi")) == 1,
                    DurumID = reader.GetInt32(reader.GetOrdinal("DurumID")),
                    DurumAdi = reader.GetString(reader.GetOrdinal("DurumAdi")),
                    KiraciID = reader.GetInt32(reader.GetOrdinal("KullaniciID")),
                    KiraciAdi = !reader.IsDBNull(reader.GetOrdinal("KiraciAdi")) ? reader.GetString(reader.GetOrdinal("KiraciAdi")) : string.Empty,
                    KiraciSoyadi = !reader.IsDBNull(reader.GetOrdinal("KiraciSoyadi")) ? reader.GetString(reader.GetOrdinal("KiraciSoyadi")) : string.Empty,
                    EvID = reader.GetInt32(reader.GetOrdinal("EvID")),
                    EvBasligi = !reader.IsDBNull(reader.GetOrdinal("EvBasligi")) ? reader.GetString(reader.GetOrdinal("EvBasligi")) : string.Empty
                };
            }

            if (rezervasyon == null)
                return NotFound();

            return View("IptalOnay", rezervasyon);
        }

        // İptal onayı sonrası (POST)
        [HttpPost, ActionName("IptalOnay")]
        [ValidateAntiForgeryToken]
        public IActionResult IptalOnayConfirmed(int id)
        {
            string? connStrNullable = _configuration.GetConnectionString("DefaultConnection");
            if (connStrNullable == null)
                throw new InvalidOperationException("DefaultConnection connection string bulunamadı.");
            string connStr = connStrNullable;

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string updateSql = "UPDATE Rezervasyon SET DurumID = (SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi = 'İptal') WHERE RezervasyonID = @id";

            using var cmd = new SqlCommand(updateSql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Rezervasyon başarıyla iptal edildi.";

            return RedirectToAction("Index");
        }
    }
}
