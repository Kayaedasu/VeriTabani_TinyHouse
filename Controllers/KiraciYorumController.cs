using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{

    [Route("Yorum")]
    public class KiraciYorumController : Controller
    {
        private readonly string _conStr;

        public KiraciYorumController(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");
        }
        [Route("Yorumlarim")]
        // Kullanıcının yorumlarını listele
        public IActionResult Yorumlarim()
        {
            List<Yorum> yorumlar = new List<Yorum>();

            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return RedirectToAction("Giris", "Kullanici");

            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                string sql = @"
                    SELECT Y.YorumID, Y.Puan, Y.YorumMetni, Y.YorumTarihi, E.Baslik as EvBaslik
                    FROM Yorum Y
                    INNER JOIN Rezervasyon R ON Y.RezervasyonID = R.RezervasyonID
                    INNER JOIN TinyHouse E ON R.EvID = E.EvID
                    WHERE R.KiraciID = @KiraciID
                    ORDER BY Y.YorumTarihi DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@KiraciID", kullaniciID.Value);

                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    yorumlar.Add(new Yorum
                    {
                        YorumID = (int)dr["YorumID"],
                        Puan = (int)dr["Puan"],
                        YorumMetni = dr["YorumMetni"]?.ToString() ?? string.Empty,
                        YorumTarihi = (DateTime)dr["YorumTarihi"],
                        Baslik = dr["EvBaslik"]?.ToString() ?? string.Empty
                    });
                }
            }

            return View("~/Views/Yorum/Yorumlarim.cshtml", yorumlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(int EvID, int RezervasyonID, int Puan, string YorumMetni)
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
            {
                TempData["Hata"] = "Yorum yapmak için giriş yapmalısınız.";
                return RedirectToAction("Giris", "Kullanici");
            }

            // 1. RezervasyonID'nin geçerli olup olmadığını kontrol et
            if (RezervasyonID <= 0)
            {
                TempData["Hata"] = "Rezervasyon geçersiz, yorum eklenemedi.";
                return RedirectToAction("EvDetay", "Kiraci", new { id = EvID });
            }

            try
            {
                using (var conn = new SqlConnection(_conStr))
                {
                    conn.Open();

                    // 2. Rezervasyon bu kullanıcıya mı ait ve geçerli mi kontrol et
                    string kontrolSql = @"
                        SELECT COUNT(*) 
                        FROM Rezervasyon 
                        WHERE RezervasyonID = @RezervasyonID 
                          AND KiraciID = @KiraciID 
                          AND DurumID IN (
                              SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi IN ('Onaylandı', 'Tamamlandı')
                          )";

                    using var kontrolCmd = new SqlCommand(kontrolSql, conn);
                    kontrolCmd.Parameters.AddWithValue("@RezervasyonID", RezervasyonID);
                    kontrolCmd.Parameters.AddWithValue("@KiraciID", kullaniciID.Value);

                    int varMi = (int)kontrolCmd.ExecuteScalar();
                    if (varMi == 0)
                    {
                        TempData["Hata"] = "Yalnızca onaylanmış veya tamamlanmış rezervasyonlar için yorum yapabilirsiniz.";
                        return RedirectToAction("EvDetay", "Kiraci", new { id = EvID });
                    }

                    // 3. Yorum ekleme işlemi
                    string sql = @"
                        INSERT INTO Yorum (EvID, RezervasyonID, Puan, YorumMetni, YorumTarihi, KiraciID) 
                        VALUES (@EvID, @RezervasyonID, @Puan, @YorumMetni, GETDATE(), @KiraciID)";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@EvID", EvID);
                    cmd.Parameters.AddWithValue("@RezervasyonID", RezervasyonID);
                    cmd.Parameters.AddWithValue("@Puan", Puan);
                    cmd.Parameters.AddWithValue("@YorumMetni", YorumMetni ?? string.Empty);
                    cmd.Parameters.AddWithValue("@KiraciID", kullaniciID.Value);

                    cmd.ExecuteNonQuery();
                }

                TempData["Basarili"] = "✅Yorumunuz başarıyla eklendi.";
                return RedirectToAction("EvDetay", "Kiraci", new { id = EvID });
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Yorum eklenirken hata oluştu: " + ex.Message;
                return RedirectToAction("EvDetay", "Kiraci", new { id = EvID });
            }
        }
    }
}
