using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    [Route("Odeme")]
    public class KiraciOdemeController : Controller
    {
        private readonly string _conStr;

        public KiraciOdemeController(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");
        }

        [HttpGet("Yeni")]
        public IActionResult Yeni(int rezervasyonId)
        {
            ViewBag.RezervasyonID = rezervasyonId;
            return View("~/Views/Odeme/Yeni.cshtml");
        }

        [HttpPost("Yeni")]
        [ValidateAntiForgeryToken]
        public IActionResult Yeni(int rezervasyonId, string OdemeYontemi, string KartSahibi, string KartNumarasi, string SonKullanmaTarihi, string CVV)
        {
            try
            {
                using (var conn = new SqlConnection(_conStr))
                {
                    conn.Open();

                    decimal tutar = 0;
                    using (var cmd = new SqlCommand("SELECT Fiyat FROM TinyHouse WHERE EvID = (SELECT EvID FROM Rezervasyon WHERE RezervasyonID = @RezervasyonID)", conn))
                    {
                        cmd.Parameters.AddWithValue("@RezervasyonID", rezervasyonId);
                        var sonuc = cmd.ExecuteScalar();
                        if (sonuc != null && sonuc != DBNull.Value)
                            tutar = Convert.ToDecimal(sonuc);
                        else
                            throw new Exception("Rezervasyona ait ev bulunamadı.");
                    }

                    string kartNumarasiGizli = null;
                    if (OdemeYontemi == "Kredi Kartı" && !string.IsNullOrEmpty(KartNumarasi) && KartNumarasi.Length >= 4)
                        kartNumarasiGizli = KartNumarasi.Substring(0, 4) + "****";
                    else if (!string.IsNullOrEmpty(KartNumarasi))
                        kartNumarasiGizli = KartNumarasi.Substring(0, Math.Min(4, KartNumarasi.Length)) + "****";

                    DateTime? sonKullanma = null;
                    if (!string.IsNullOrEmpty(SonKullanmaTarihi) && DateTime.TryParse(SonKullanmaTarihi, out DateTime dt))
                        sonKullanma = dt;

                    string sqlOdemeEkle = @"INSERT INTO Odeme (RezervasyonID, OdemeYontemi, KartSahibi, KartNumarasi, SonKullanmaTarihi, CVV, Tutar, OdemeTarihi)
                                    VALUES (@RezervasyonID, @OdemeYontemi, @KartSahibi, @KartNumarasi, @SonKullanmaTarihi, @CVV, @Tutar, GETDATE())";

                    using (var cmdOdeme = new SqlCommand(sqlOdemeEkle, conn))
                    {
                        cmdOdeme.Parameters.AddWithValue("@RezervasyonID", rezervasyonId);
                        cmdOdeme.Parameters.AddWithValue("@OdemeYontemi", OdemeYontemi ?? (object)DBNull.Value);
                        cmdOdeme.Parameters.AddWithValue("@KartSahibi", KartSahibi ?? (object)DBNull.Value);
                        cmdOdeme.Parameters.AddWithValue("@KartNumarasi", kartNumarasiGizli ?? (object)DBNull.Value);
                        cmdOdeme.Parameters.AddWithValue("@SonKullanmaTarihi", sonKullanma ?? (object)DBNull.Value);
                        cmdOdeme.Parameters.AddWithValue("@CVV", CVV ?? (object)DBNull.Value);
                        cmdOdeme.Parameters.AddWithValue("@Tutar", tutar);

                        cmdOdeme.ExecuteNonQuery();
                    }

                    string sqlRezervasyonGuncelle = @"
                        UPDATE Rezervasyon
                        SET OdemeDurumu = 'Ödeme Yapıldı',
                            ToplamOdeme = ISNULL(ToplamOdeme, 0) + @Tutar,
                            DurumID = 2
                        WHERE RezervasyonID = @RezervasyonID";

                    using (var cmdRezervasyon = new SqlCommand(sqlRezervasyonGuncelle, conn))
                    {
                        cmdRezervasyon.Parameters.AddWithValue("@Tutar", tutar);
                        cmdRezervasyon.Parameters.AddWithValue("@RezervasyonID", rezervasyonId);
                        int rowsUpdated = cmdRezervasyon.ExecuteNonQuery();

                        if (rowsUpdated == 0)
                            throw new Exception("Rezervasyon güncellenemedi.");
                    }

                    TempData["Basarili"] = "Ödemeniz başarıyla alındı.";
                    return RedirectToAction("Rezervasyonlarim", "Kiraci");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Ödeme sırasında hata oluştu: " + ex.Message;
                ViewBag.RezervasyonID = rezervasyonId;
                return View();
            }
        }
    }
}
