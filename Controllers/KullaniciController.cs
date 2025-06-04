using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    public class KullaniciController : Controller
    {
        private readonly IConfiguration _configuration;

        public KullaniciController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Giris()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Giris(string eposta, string sifre)
        {
            string sifreHash = Sha256IleHashle(sifre);
            Kullanici? kullanici = KullaniciGetirEpostaVeSifreIle(eposta, sifreHash);

            if (kullanici == null)
            {
                ViewBag.Hata = "E-posta veya şifre hatalı!";
                return View();
            }

            if (!kullanici.AktifMi)
            {
                ViewBag.Hata = "Hesabınız pasif. Lütfen yöneticinizle iletişime geçin.";
                return View();
            }

            HttpContext.Session.SetInt32("KullaniciID", kullanici.KullaniciID);
            HttpContext.Session.SetInt32("RolID", kullanici.RolID);

            switch (kullanici.RolID)
            {
                case 1: // Admin
                    return RedirectToAction("AdminDashboard", "Admin");
                case 2: // Ev Sahibi
                    return RedirectToAction("Ilanlar", "EvSahibi");
                case 3: // Kiracı
                    return RedirectToAction("Dashboard", "Kiraci");
                default:
                    // Eğer rol tanımlı değilse girişe geri döner
                    return RedirectToAction("Giris");
            }
        }

        [HttpGet]
        public IActionResult Kayit()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Kayit(Kullanici yeniKullanici)
        {
            yeniKullanici.SifreHash = Sha256IleHashle(yeniKullanici.SifreHash ?? "");

            using SqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // E-posta zaten kayıtlı mı kontrol et
                using (SqlCommand kontrolCmd = new SqlCommand("SELECT COUNT(*) FROM Kullanici WHERE Eposta = @Eposta", conn, transaction))
                {
                    kontrolCmd.Parameters.AddWithValue("@Eposta", yeniKullanici.Eposta ?? "");
                    int epostaSayisi = (int)kontrolCmd.ExecuteScalar();

                    if (epostaSayisi > 0)
                    {
                        TempData["Hata"] = "Bu e-posta adresi zaten kayıtlı.";
                        transaction.Rollback();
                        return View();
                    }
                }

                string query = @"
            INSERT INTO Kullanici (Ad, Soyad, Telefon, Eposta, SifreHash, KayitTarihi, RolID, AktifMi)
            VALUES (@Ad, @Soyad, @Telefon, @Eposta, @SifreHash, GETDATE(), @RolID, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int yeniKullaniciID;

                using (SqlCommand cmd = new(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ad", yeniKullanici.Ad ?? "");
                    cmd.Parameters.AddWithValue("@Soyad", yeniKullanici.Soyad ?? "");
                    cmd.Parameters.AddWithValue("@Telefon", yeniKullanici.Telefon ?? "");
                    cmd.Parameters.AddWithValue("@Eposta", yeniKullanici.Eposta ?? "");
                    cmd.Parameters.AddWithValue("@SifreHash", yeniKullanici.SifreHash);
                    cmd.Parameters.AddWithValue("@RolID", yeniKullanici.RolID);

                    yeniKullaniciID = (int)cmd.ExecuteScalar();
                }

                if (yeniKullanici.RolID == 2) // Ev Sahibi ise
                {
                    string evSahibiInsert = "INSERT INTO EvSahibi (EvSahibiID, Aciklama) VALUES (@EvSahibiID, '')";
                    using var evSahibiCmd = new SqlCommand(evSahibiInsert, conn, transaction);
                    evSahibiCmd.Parameters.AddWithValue("@EvSahibiID", yeniKullaniciID);
                    evSahibiCmd.ExecuteNonQuery();
                }
                else if (yeniKullanici.RolID == 3) // Kiracı ise
                {
                    string kiraciInsert = "INSERT INTO Kiraci (KiraciID) VALUES (@KiraciID)";
                    using var kiraciCmd = new SqlCommand(kiraciInsert, conn, transaction);
                    kiraciCmd.Parameters.AddWithValue("@KiraciID", yeniKullaniciID);
                    kiraciCmd.ExecuteNonQuery();
                }

                transaction.Commit();

                TempData["Basarili"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
                return RedirectToAction("Giris");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Hata"] = "Kayıt sırasında hata oluştu: " + ex.Message;
                return View();
            }
        }



        [HttpGet]
        public IActionResult SifreGuncelle()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SifreGuncelle(string eposta, string yeniSifre, string yeniSifreTekrar)
        {
            if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(yeniSifre) || string.IsNullOrEmpty(yeniSifreTekrar))
            {
                ViewBag.Hata = "Lütfen tüm alanları doldurun.";
                return View();
            }

            if (yeniSifre != yeniSifreTekrar)
            {
                ViewBag.Hata = "Yeni şifreler uyuşmuyor!";
                return View();
            }

            using SqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Kullanıcı var mı kontrol et
            string kullaniciQuery = "SELECT KullaniciID FROM Kullanici WHERE Eposta = @Eposta";
            int? kullaniciID = null;

            using (SqlCommand cmd = new(kullaniciQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Eposta", eposta);
                var result = cmd.ExecuteScalar();
                if (result == null)
                {
                    ViewBag.Hata = "Bu e-posta ile kayıtlı kullanıcı bulunamadı.";
                    return View();
                }
                kullaniciID = Convert.ToInt32(result);
            }

            string yeniHash = Sha256IleHashle(yeniSifre);

            // Şifreyi güncelle
            string updateQuery = "UPDATE Kullanici SET SifreHash = @YeniHash WHERE KullaniciID = @KullaniciID";
            using (SqlCommand updateCmd = new(updateQuery, conn))
            {
                updateCmd.Parameters.AddWithValue("@YeniHash", yeniHash);
                updateCmd.Parameters.AddWithValue("@KullaniciID", kullaniciID);
                updateCmd.ExecuteNonQuery();
            }

            ViewBag.Basarili = "Şifre başarıyla güncellendi!";
            return View();
        }


        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Giris");
        }

        private Kullanici? KullaniciGetirEpostaVeSifreIle(string eposta, string sifreHash)
        {
            using SqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            string query = "SELECT * FROM Kullanici WHERE Eposta = @eposta AND SifreHash = @sifreHash";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@eposta", eposta);
            cmd.Parameters.AddWithValue("@sifreHash", sifreHash);

            using SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Kullanici
                {
                    KullaniciID = (int)reader["KullaniciID"],
                    Ad = reader["Ad"]?.ToString() ?? "",
                    Soyad = reader["Soyad"]?.ToString() ?? "",
                    Telefon = reader["Telefon"]?.ToString() ?? "",
                    Eposta = reader["Eposta"]?.ToString() ?? "",
                    SifreHash = reader["SifreHash"]?.ToString() ?? "",
                    KayitTarihi = (DateTime)reader["KayitTarihi"],
                    RolID = (int)reader["RolID"],
                    AktifMi = (bool)reader["AktifMi"]
                };
            }

            return null;
        }

        private static string Sha256IleHashle(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Hashlenmek istenen değer boş olamaz.", nameof(input));
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = SHA256.HashData(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
