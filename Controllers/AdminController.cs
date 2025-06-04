using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetInt32("RolID") != 1)
                return RedirectToAction("Giris", "Kullanici");

            var model = new AdminDashboardViewModel();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Kullanıcı sayıları
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Kullanici", conn))
            {
                model.ToplamKullanici = (int)cmd.ExecuteScalar();
            }
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Kullanici WHERE AktifMi = 1", conn))
            {
                model.AktifKullanici = (int)cmd.ExecuteScalar();
            }
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Kullanici WHERE AktifMi = 0", conn))
            {
                model.PasifKullanici = (int)cmd.ExecuteScalar();
            }

            // Rezervasyon durumları
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Rezervasyon", conn))
            {
                model.ToplamRezervasyon = (int)cmd.ExecuteScalar();
            }
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Rezervasyon WHERE DurumID = (SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi = 'Beklemede')", conn))
            {
                model.BekleyenRezervasyon = (int)cmd.ExecuteScalar();
            }
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Rezervasyon WHERE DurumID = (SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi = 'Onaylandi')", conn))
            {
                model.OnaylananRezervasyon = (int)cmd.ExecuteScalar();
            }
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Rezervasyon WHERE DurumID = (SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi = 'İptal')", conn))
            {
                model.IptalEdilenRezervasyon = (int)cmd.ExecuteScalar();
            }

            // Ödeme toplamları ve sayısı
            using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Tutar), 0) FROM Odeme", conn))
            {
                model.ToplamGelir = (decimal)cmd.ExecuteScalar();
            }
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Odeme", conn))
            {
                model.ToplamOdemeSayisi = (int)cmd.ExecuteScalar();
            }

            return View("~/Views/Admin/AdminDashboard.cshtml", model);
        }

        [HttpGet]
        public IActionResult Kullanicilar(string? arama, int? rolID, int? aktiflik)
        {
            List<Kullanici> kullanicilar = new();
            Dictionary<int, string> rolAdlari = new();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var rolCmd = new SqlCommand("SELECT RolID, RolAdi FROM Rol", conn);
            using var rolReader = rolCmd.ExecuteReader();
            while (rolReader.Read())
            {
                var rolAdi = rolReader["RolAdi"]?.ToString() ?? "";
                rolAdlari.Add((int)rolReader["RolID"], rolAdi);
            }
            rolReader.Close();

            using var cmd = new SqlCommand("sp_KullaniciListele", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@arama", (object?)arama ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@rolID", (object?)rolID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@aktiflik", (object?)aktiflik ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                kullanicilar.Add(new Kullanici
                {
                    KullaniciID = reader["KullaniciID"] != DBNull.Value ? (int)reader["KullaniciID"] : 0,
                    Ad = reader["Ad"]?.ToString() ?? "",
                    Soyad = reader["Soyad"]?.ToString() ?? "",
                    Eposta = reader["Eposta"]?.ToString() ?? "",
                    Telefon = reader["Telefon"]?.ToString() ?? "",
                    SifreHash = reader["SifreHash"]?.ToString() ?? "",
                    KayitTarihi = reader["KayitTarihi"] != DBNull.Value ? (DateTime)reader["KayitTarihi"] : default,
                    RolID = reader["RolID"] != DBNull.Value ? (int)reader["RolID"] : 0,
                    AktifMi = reader["AktifMi"] != DBNull.Value && (bool)reader["AktifMi"]
                });
            }

            ViewBag.Roller = rolAdlari.Select(x => new Rol { RolID = x.Key, RolAdi = x.Value }).ToList();
            ViewBag.RolAdlari = rolAdlari;

            return View("~/Views/Admin/KullaniciListele.cshtml", kullanicilar);
        }

        [HttpGet]
        public IActionResult AktiflikDegistir(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            string query = "SELECT * FROM Kullanici WHERE KullaniciID = @id";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return NotFound();

            var kullanici = new Kullanici
            {
                KullaniciID = (int)reader["KullaniciID"],
                Ad = reader["Ad"]?.ToString() ?? "",
                Soyad = reader["Soyad"]?.ToString() ?? "",
                AktifMi = (bool)reader["AktifMi"]
            };

            ViewBag.IsActive = kullanici.AktifMi;

            return View(kullanici);
        }


        [HttpPost]
        public IActionResult AktiflikDegistir(int KullaniciID, string? Aciklama)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // Aktiflik durumunu değiştir
                string updateQuery = "UPDATE Kullanici SET AktifMi = CASE AktifMi WHEN 1 THEN 0 ELSE 1 END WHERE KullaniciID = @KullaniciID";
                using (var cmdUpdate = new SqlCommand(updateQuery, conn, transaction))
                {
                    cmdUpdate.Parameters.AddWithValue("@KullaniciID", KullaniciID);
                    cmdUpdate.ExecuteNonQuery();
                }

                // Yeni durumu al
                bool yeniDurum;
                string selectQuery = "SELECT AktifMi FROM Kullanici WHERE KullaniciID = @KullaniciID";
                using (var cmdSelect = new SqlCommand(selectQuery, conn, transaction))
                {
                    cmdSelect.Parameters.AddWithValue("@KullaniciID", KullaniciID);
                    yeniDurum = (bool)cmdSelect.ExecuteScalar();
                }

                // KullaniciDurum tablosuna kayıt ekle
                string insertDurum = @"
            INSERT INTO KullaniciDurum (KullaniciID, Durum, BaslangicTarihi, Aciklama)
            VALUES (@KullaniciID, @Durum, GETDATE(), @Aciklama)";
                using (var cmdInsert = new SqlCommand(insertDurum, conn, transaction))
                {
                    cmdInsert.Parameters.AddWithValue("@KullaniciID", KullaniciID);
                    cmdInsert.Parameters.AddWithValue("@Durum", yeniDurum);
                    cmdInsert.Parameters.AddWithValue("@Aciklama", Aciklama ?? "");
                    cmdInsert.ExecuteNonQuery();
                }

                transaction.Commit();

                TempData["Basarili"] = "Kullanıcının aktiflik durumu başarıyla değiştirildi.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Hata"] = "Aktiflik değiştirilemedi: " + ex.Message;
            }

            return RedirectToAction("Kullanicilar");
        }


        [HttpGet]
        public IActionResult Sil(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            string query = "SELECT * FROM Kullanici WHERE KullaniciID = @id";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return NotFound();

            var kullanici = new Kullanici
            {
                KullaniciID = reader["KullaniciID"] != DBNull.Value ? (int)reader["KullaniciID"] : 0,
                Ad = reader["Ad"]?.ToString() ?? "",
                Soyad = reader["Soyad"]?.ToString() ?? "",
                Eposta = reader["Eposta"]?.ToString() ?? ""
            };

            return View("KullaniciSil", kullanici);
        }

        [HttpPost, ActionName("Sil")]
        [ValidateAntiForgeryToken]
        public IActionResult SilOnay(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // KullaniciDurum kayıtlarını sil
                using (var cmdDurum = new SqlCommand("DELETE FROM KullaniciDurum WHERE KullaniciID = @id", conn, transaction))
                {
                    cmdDurum.Parameters.AddWithValue("@id", id);
                    cmdDurum.ExecuteNonQuery();
                }

                // Alt tablolardan silme işlemi
                using (var cmdEvSahibi = new SqlCommand("DELETE FROM EvSahibi WHERE EvSahibiID = @id", conn, transaction))
                {
                    cmdEvSahibi.Parameters.AddWithValue("@id", id);
                    cmdEvSahibi.ExecuteNonQuery();
                }

                using (var cmdKiraci = new SqlCommand("DELETE FROM Kiraci WHERE KiraciID = @id", conn, transaction))
                {
                    cmdKiraci.Parameters.AddWithValue("@id", id);
                    cmdKiraci.ExecuteNonQuery();
                }

                // Ana kullanıcı tablosundan silme
                using (var cmdKullanici = new SqlCommand("DELETE FROM Kullanici WHERE KullaniciID = @id", conn, transaction))
                {
                    cmdKullanici.Parameters.AddWithValue("@id", id);
                    int affectedRows = cmdKullanici.ExecuteNonQuery();
                    if (affectedRows == 0)
                        throw new Exception("Silinecek kullanıcı bulunamadı.");
                }

                transaction.Commit();
                TempData["Basarili"] = "Kullanıcı başarıyla silindi.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Hata"] = "Kullanıcı silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Kullanicilar");
        }



        [HttpGet]
        public IActionResult Duzenle(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            string query = "SELECT * FROM Kullanici WHERE KullaniciID = @id";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return NotFound();

            var kullanici = new Kullanici
            {
                KullaniciID = reader["KullaniciID"] != DBNull.Value ? (int)reader["KullaniciID"] : 0,
                Ad = reader["Ad"]?.ToString() ?? "",
                Soyad = reader["Soyad"]?.ToString() ?? "",
                Eposta = reader["Eposta"]?.ToString() ?? "",
                Telefon = reader["Telefon"]?.ToString() ?? "",
                RolID = reader["RolID"] != DBNull.Value ? (int)reader["RolID"] : 0,
                AktifMi = reader["AktifMi"] != DBNull.Value && (bool)reader["AktifMi"]
            };

            var roller = new Dictionary<int, string>();
            using var rolConn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            rolConn.Open();
            using var rolCmd = new SqlCommand("SELECT RolID, RolAdi FROM Rol", rolConn);
            using var rolReader = rolCmd.ExecuteReader();
            while (rolReader.Read())
            {
                roller.Add((int)rolReader["RolID"], rolReader["RolAdi"].ToString()!);
            }

            ViewBag.Roller = roller;

            return View("~/Views/Admin/KullaniciDuzenle.cshtml", kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(Kullanici kullanici)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/KullaniciDuzenle.cshtml", kullanici);

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // Mevcut rolü al
                int mevcutRolID = 0;
                using (var cmd = new SqlCommand("SELECT RolID FROM Kullanici WHERE KullaniciID = @KullaniciID", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);
                    mevcutRolID = (int)cmd.ExecuteScalar();
                }

                // Kullanıcıyı güncelle
                string updateQuery = @"
                    UPDATE Kullanici SET 
                    Ad = @Ad,
                    Soyad = @Soyad,
                    Eposta = @Eposta,
                    Telefon = @Telefon,
                    RolID = @RolID,
                    AktifMi = @AktifMi
                    WHERE KullaniciID = @KullaniciID";

                using (var cmd = new SqlCommand(updateQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ad", kullanici.Ad ?? "");
                    cmd.Parameters.AddWithValue("@Soyad", kullanici.Soyad ?? "");
                    cmd.Parameters.AddWithValue("@Eposta", kullanici.Eposta ?? "");
                    cmd.Parameters.AddWithValue("@Telefon", kullanici.Telefon ?? "");
                    cmd.Parameters.AddWithValue("@RolID", kullanici.RolID);
                    cmd.Parameters.AddWithValue("@AktifMi", kullanici.AktifMi);
                    cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);

                    cmd.ExecuteNonQuery();
                }

                // Eğer rol değiştiyse alt tabloları güncelle
                if (mevcutRolID != kullanici.RolID)
                {
                    // Eski rolün alt tablosundan sil
                    if (mevcutRolID == 2) // Ev Sahibi
                    {
                        using var cmd = new SqlCommand("DELETE FROM EvSahibi WHERE EvSahibiID = @KullaniciID", conn, transaction);
                        cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);
                        cmd.ExecuteNonQuery();
                    }
                    else if (mevcutRolID == 3) // Kiracı
                    {
                        using var cmd = new SqlCommand("DELETE FROM Kiraci WHERE KiraciID = @KullaniciID", conn, transaction);
                        cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);
                        cmd.ExecuteNonQuery();
                    }

                    // Yeni rolün alt tablosuna ekle
                    if (kullanici.RolID == 2) // Ev Sahibi
                    {
                        using var cmd = new SqlCommand("INSERT INTO EvSahibi (EvSahibiID, Aciklama) VALUES (@KullaniciID, '')", conn, transaction);
                        cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);
                        cmd.ExecuteNonQuery();
                    }
                    else if (kullanici.RolID == 3) // Kiracı
                    {
                        using var cmd = new SqlCommand("INSERT INTO Kiraci (KiraciID) VALUES (@KullaniciID)", conn, transaction);
                        cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);
                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                TempData["Basarili"] = "Kullanıcı başarıyla güncellendi.";
            }
            catch
            {
                transaction.Rollback();
                TempData["Hata"] = "Kullanıcı güncellenirken bir hata oluştu.";
                return View("~/Views/Admin/KullaniciDuzenle.cshtml", kullanici);
            }

            return RedirectToAction("Kullanicilar");
        }
    }
}
