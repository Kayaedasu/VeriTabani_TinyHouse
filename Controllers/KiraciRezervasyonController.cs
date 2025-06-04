using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    [Route("Rezervasyon")]
    public class KiraciRezervasyonController : Controller
    {
        private readonly IConfiguration _configuration;

        public KiraciRezervasyonController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Yap")]
        public IActionResult Yap(int evID, DateTime baslangic, DateTime bitis, string aciklama)
        {
            int? kiraciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kiraciID == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                // *** BAĞLANTI DİZESİ BURADA DOĞRU ÇEKİLİYOR ***
                var conStr = _configuration.GetConnectionString("DefaultConnection");
                using (var conn = new SqlConnection(conStr))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("sp_RezervasyonYap", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@KiraciID", kiraciID.Value);
                        cmd.Parameters.AddWithValue("@EvID", evID);
                        cmd.Parameters.AddWithValue("@BaslangicTarihi", baslangic);
                        cmd.Parameters.AddWithValue("@BitisTarihi", bitis);
                        cmd.Parameters.AddWithValue("@Aciklama", aciklama ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Basarili"] = "Rezervasyon başarıyla yapıldı.";
            }
            catch (SqlException ex)
            {
                TempData["Hata"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Hata"] = "Rezervasyon işlemi sırasında bir hata oluştu.";
            }

            return RedirectToAction("EvDetay", "Kiraci", new { id = evID });
        }

        [HttpGet("DoluTarihler")]
        public IActionResult DoluTarihler(int evId)
        {
            var doluRezervasyonlar = new List<object>();

            // *** BAĞLANTI DİZESİ BURADA DOĞRU ÇEKİLİYOR ***
            var conStr = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(conStr))
            {
                conn.Open();
                string sql = @"
                    SELECT BaslangicTarihi, BitisTarihi
                    FROM Rezervasyon
                    WHERE EvID = @EvID
                    AND DurumID IN (SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi IN ('Beklemede', 'Onaylandi'))
                ";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@EvID", evId);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            doluRezervasyonlar.Add(new
                            {
                                baslangic = ((DateTime)dr["BaslangicTarihi"]).ToString("yyyy-MM-dd"),
                                bitis = ((DateTime)dr["BitisTarihi"]).ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
            }

            return Json(doluRezervasyonlar);
        }
    }
}
