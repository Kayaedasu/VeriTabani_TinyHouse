using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace TinyHouse.Controllers
{
    public class BildirimController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly EmailSender _emailSender;
        private readonly string _conStr;
        private string conStr;

        // Constructor injection ile iki dependency birlikte alınır
        public BildirimController(IConfiguration configuration, EmailSender emailSender)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _conStr = _configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");
        }

        public IActionResult Index()
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return RedirectToAction("Giris", "Kullanici"); // "Login" değil!

            var bildirimler = new List<Bildirim>();

            // Bildirimleri çek
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                using var cmd = new SqlCommand("SELECT * FROM Bildirim WHERE KullaniciID = @id ORDER BY OlusturmaTarihi DESC", conn);
                cmd.Parameters.AddWithValue("@id", kullaniciID.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bildirimler.Add(new Bildirim
                    {
                        BildirimID = reader["BildirimID"] != DBNull.Value ? (int)reader["BildirimID"] : 0,
                        KullaniciID = reader["KullaniciID"] != DBNull.Value ? (int)reader["KullaniciID"] : 0,
                        Baslik = reader["Baslik"]?.ToString() ?? "",
                        Mesaj = reader["Mesaj"]?.ToString() ?? "",
                        Okundu = reader["Okundu"] != DBNull.Value && (bool)reader["Okundu"],
                        OlusturmaTarihi = reader["OlusturmaTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["OlusturmaTarihi"]) : DateTime.Now
                    });
                }
            }

            // Bildirimleri okundu olarak işaretle
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                using var updateCmd = new SqlCommand("UPDATE Bildirim SET Okundu = 1 WHERE KullaniciID = @id AND Okundu = 0", conn);
                updateCmd.Parameters.AddWithValue("@id", kullaniciID.Value);
                updateCmd.ExecuteNonQuery();
            }

            // Okunmamış bildirim sayısını session'a yaz
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Bildirim WHERE KullaniciID = @id AND Okundu = 0", conn);
                cmd.Parameters.AddWithValue("@id", kullaniciID.Value);
                int okunmamis = (int)cmd.ExecuteScalar();
                HttpContext.Session.SetInt32("OkunmamisBildirimsayisi", okunmamis);
            }

            return View(bildirimler);
        }

        [HttpGet]
        public int OkunmamisSayisi()
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null) return 0;

            using var conn = new SqlConnection(_conStr);
            conn.Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Bildirim WHERE KullaniciID = @id AND Okundu = 0", conn);
            cmd.Parameters.AddWithValue("@id", kullaniciID.Value);
            return (int)cmd.ExecuteScalar();
        }

        public IActionResult Oku()
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return RedirectToAction("Giris", "Kullanici");

            using var conn = new SqlConnection(_conStr);
            conn.Open();

            using var updateCmd = new SqlCommand("UPDATE Bildirim SET Okundu = 1 WHERE KullaniciID = @id AND Okundu = 0", conn);
            updateCmd.Parameters.AddWithValue("@id", kullaniciID.Value);
            updateCmd.ExecuteNonQuery();

            using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Bildirim WHERE KullaniciID = @id AND Okundu = 0", conn);
            countCmd.Parameters.AddWithValue("@id", kullaniciID.Value);
            int okunmamis = (int)countCmd.ExecuteScalar();
            HttpContext.Session.SetInt32("OkunmamisBildirimsayisi", okunmamis);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IsaretleOkundu([FromBody] BildirimleriGuncelle model)
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return Unauthorized();

            using var conn = new SqlConnection(_conStr);
            conn.Open();
            using var cmd = new SqlCommand("UPDATE Bildirim SET Okundu = 1 WHERE BildirimID = @id AND KullaniciID = @kullaniciID", conn);
            cmd.Parameters.AddWithValue("@id", model.Id);
            cmd.Parameters.AddWithValue("@kullaniciID", kullaniciID.Value);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Bildirim WHERE KullaniciID = @kullaniciID AND Okundu = 0", conn);
                countCmd.Parameters.AddWithValue("@kullaniciID", kullaniciID.Value);
                int okunmamis = (int)countCmd.ExecuteScalar();
                HttpContext.Session.SetInt32("OkunmamisBildirimsayisi", okunmamis);

                return Ok();
            }
            return BadRequest();
        }

        public async Task<IActionResult> BildirimEkleVeMailGonder(int kullaniciID, string baslik, string mesaj)
        {
            string kullaniciEmail = GetKullaniciEmail(kullaniciID);

            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                using var cmd = new SqlCommand(@"INSERT INTO Bildirim (KullaniciID, Baslik, Mesaj, Okundu, OlusturmaTarihi)
                                   VALUES (@kullaniciID, @baslik, @mesaj, 0, GETDATE())", conn);
                cmd.Parameters.AddWithValue("@kullaniciID", kullaniciID);
                cmd.Parameters.AddWithValue("@baslik", baslik);
                cmd.Parameters.AddWithValue("@mesaj", mesaj);
                await cmd.ExecuteNonQueryAsync();
            }

            try
            {
                await _emailSender.SendEmailAsync(kullaniciEmail, baslik, mesaj);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mail gönderme hatası: " + ex.Message);
            }

            return RedirectToAction("Index");
        }

        private string GetKullaniciEmail(int kullaniciID)
        {
            using (var conn = new SqlConnection(conStr))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Eposta FROM Kullanici WHERE KullaniciID = @id", conn);
                cmd.Parameters.AddWithValue("@id", kullaniciID);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }

        public async Task<IActionResult> TestBildirimEkleVeMail()
        {
            int testKullaniciID = 2;
            string baslik = "Test Bildirim Başlığı";
            string mesaj = "Bu, test amaçlı veritabanına eklenen ve mail olarak gönderilen bildirimin içeriğidir.";

            try
            {
                using (var conn = new SqlConnection(_conStr))
                {
                    conn.Open();
                    using var cmd = new SqlCommand(@"INSERT INTO Bildirim (KullaniciID, Baslik, Mesaj, Okundu, OlusturmaTarihi)
                                   VALUES (@kullaniciID, @baslik, @mesaj, 0, GETDATE())", conn);
                    cmd.Parameters.AddWithValue("@kullaniciID", testKullaniciID);
                    cmd.Parameters.AddWithValue("@baslik", baslik);
                    cmd.Parameters.AddWithValue("@mesaj", mesaj);
                    await cmd.ExecuteNonQueryAsync();
                }

                string kullaniciEmail = GetKullaniciEmail(testKullaniciID);
                await _emailSender.SendEmailAsync(kullaniciEmail, baslik, mesaj);

                return Content("Bildirim veritabanına eklendi ve e-posta gönderildi.");
            }
            catch (Exception ex)
            {
                return Content("Hata oluştu: " + ex.Message);
            }
        }
    }
}
