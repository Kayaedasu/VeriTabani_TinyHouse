using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    [Route("Ilan")]
    public class AdminIlanController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminIlanController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // İlanları Listele
        [HttpGet("")]
        public IActionResult Index()
        {
            var ilanlar = new List<Ilan>();
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string sql = @"
                SELECT EvID, EvSahibiID, KonumID, DurumID, Baslik, Aciklama, Fiyat, EklenmeTarihi 
                FROM TinyHouse";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                ilanlar.Add(new Ilan
                {
                    IlanID = reader["EvID"] != DBNull.Value ? (int)reader["EvID"] : 0,
                    EvSahibiID = reader["EvSahibiID"] != DBNull.Value ? (int)reader["EvSahibiID"] : 0,
                    KonumID = reader["KonumID"] != DBNull.Value ? (int)reader["KonumID"] : 0,
                    DurumID = reader["DurumID"] != DBNull.Value ? (int)reader["DurumID"] : 0,
                    Baslik = reader["Baslik"] != DBNull.Value ? reader["Baslik"].ToString()! : string.Empty,
                    Aciklama = reader["Aciklama"] != DBNull.Value ? reader["Aciklama"].ToString()! : string.Empty,
                    Fiyat = reader["Fiyat"] != DBNull.Value ? (decimal)reader["Fiyat"] : 0m,
                    EklenmeTarihi = reader["EklenmeTarihi"] != DBNull.Value ? (System.DateTime)reader["EklenmeTarihi"] : default
                });
            }

            return View("~/Views/Ilan/Index.cshtml", ilanlar);
        }

        // İlan Detay - Düzenleme Formu (GET)
        [HttpGet("Duzenle/{id}")]
        public IActionResult Duzenle(int id)
        {
            Ilan? ilan = null;
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string sql = "SELECT * FROM TinyHouse WHERE EvID = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                ilan = new Ilan
                {
                    IlanID = reader["EvID"] != DBNull.Value ? (int)reader["EvID"] : 0,
                    EvSahibiID = reader["EvSahibiID"] != DBNull.Value ? (int)reader["EvSahibiID"] : 0,
                    KonumID = reader["KonumID"] != DBNull.Value ? (int)reader["KonumID"] : 0,
                    DurumID = reader["DurumID"] != DBNull.Value ? (int)reader["DurumID"] : 0,
                    Baslik = reader["Baslik"] != DBNull.Value ? reader["Baslik"].ToString()! : string.Empty,
                    Aciklama = reader["Aciklama"] != DBNull.Value ? reader["Aciklama"].ToString()! : string.Empty,
                    Fiyat = reader["Fiyat"] != DBNull.Value ? (decimal)reader["Fiyat"] : 0m,
                    EklenmeTarihi = reader["EklenmeTarihi"] != DBNull.Value ? (System.DateTime)reader["EklenmeTarihi"] : default
                };
            }

            if (ilan == null)
                return NotFound();

            return View("~/Views/Ilan/Duzenle.cshtml", ilan);
        }

        // İlan Güncelle (POST)
        [HttpPost("Duzenle/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(int id, Ilan model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Ilan/Duzenle.cshtml", model);

            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string sql = @"EXEC sp_IlanGuncelle @EvID, @Baslik, @Aciklama, @Fiyat, @DurumID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EvID", model.IlanID);
            cmd.Parameters.AddWithValue("@Baslik", model.Baslik);
            cmd.Parameters.AddWithValue("@Aciklama", model.Aciklama);
            cmd.Parameters.AddWithValue("@Fiyat", model.Fiyat);
            cmd.Parameters.AddWithValue("@DurumID", model.DurumID);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // İlan Silme Onay Sayfası (GET)
        [HttpGet("Sil/{id}")]
        public IActionResult Sil(int id)
        {
            Ilan? ilan = null;
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string sql = "SELECT EvID, Baslik FROM TinyHouse WHERE EvID = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                ilan = new Ilan
                {
                    IlanID = reader["EvID"] != DBNull.Value ? (int)reader["EvID"] : 0,
                    Baslik = reader["Baslik"] != DBNull.Value ? reader["Baslik"].ToString()! : string.Empty
                };
            }

            if (ilan == null)
                return NotFound();

            return View("~/Views/Ilan/Sil.cshtml", ilan);
        }

        // İlan Sil (POST)
        [HttpPost("Sil/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult SilOnay(int id)
        {
            int evSahibiID = 0;
            string ilanBaslik = "";
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            // İlan sahibini ve başlığı al
            string getOwnerSql = "SELECT EvSahibiID, Baslik FROM TinyHouse WHERE EvID = @id";
            using (var cmdOwner = new SqlCommand(getOwnerSql, conn))
            {
                cmdOwner.Parameters.AddWithValue("@id", id);

                using var reader = cmdOwner.ExecuteReader();

                if (reader.Read())
                {
                    evSahibiID = reader["EvSahibiID"] != DBNull.Value ? (int)reader["EvSahibiID"] : 0;
                    ilanBaslik = reader["Baslik"] != DBNull.Value ? reader["Baslik"].ToString()! : string.Empty;
                }
            }

            // İlanı sil
            string deleteSql = "DELETE FROM TinyHouse WHERE EvID = @id";
            using (var cmdDelete = new SqlCommand(deleteSql, conn))
            {
                cmdDelete.Parameters.AddWithValue("@id", id);
                cmdDelete.ExecuteNonQuery();
            }

            // Bildirim ekle
            string bildirimSql = @"
                INSERT INTO Bildirim (KullaniciID, Baslik, Mesaj, Okundu, OlusturmaTarihi)
                VALUES (@kullaniciID, @baslik, @mesaj, 0, GETDATE())";
            using (var cmdBildirim = new SqlCommand(bildirimSql, conn))
            {
                cmdBildirim.Parameters.AddWithValue("@kullaniciID", evSahibiID);
                cmdBildirim.Parameters.AddWithValue("@baslik", "İlan Silindi");
                cmdBildirim.Parameters.AddWithValue("@mesaj", $"\"{ilanBaslik}\" başlıklı ilanınız admin tarafından silindi.");

                cmdBildirim.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
