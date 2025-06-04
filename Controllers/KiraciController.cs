using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace TinyHouse.Controllers
{
    public class KiraciController : Controller
    {
        private readonly string _conStr;

        public KiraciController(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");
        }

        // Tüm actionlardan önce çağrılır: okunmamış bildirim sayısını session'a yazar
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID != null)
            {
                using var conn = new SqlConnection(_conStr);
                conn.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM Bildirim WHERE KullaniciID = @id AND Okundu = 0", conn);
                cmd.Parameters.AddWithValue("@id", kullaniciID.Value);
                int okunmamis = (int)cmd.ExecuteScalar();
                HttpContext.Session.SetInt32("OkunmamisBildirimsayisi", okunmamis);
            }
            base.OnActionExecuting(context);
        }

        public IActionResult Privacy() => View();

        // Kiracı ev arama/listeme
        public IActionResult Evler(string sehir = "", decimal? minFiyat = null, decimal? maxFiyat = null, string siralama = "")
        {
            var evler = new List<Ev>();
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();

                var sql = @"
            SELECT E.EvID, E.Baslik, E.Aciklama, E.Fiyat, 
                   K.Sehir + ', ' + K.Ilce AS Konum, D.DurumAdi
            FROM TinyHouse E
            INNER JOIN Konum K ON E.KonumID = K.KonumID
            INNER JOIN TinyHouseDurum D ON E.DurumID = D.DurumID
            WHERE D.DurumAdi = 'Aktif'";

                if (!string.IsNullOrEmpty(sehir))
                    sql += " AND K.Sehir LIKE @sehir";
                if (minFiyat.HasValue)
                    sql += " AND E.Fiyat >= @minFiyat";
                if (maxFiyat.HasValue)
                    sql += " AND E.Fiyat <= @maxFiyat";

                if (!string.IsNullOrEmpty(siralama))
                {
                    if (siralama == "fiyat_az")
                        sql += " ORDER BY E.Fiyat ASC";
                    else if (siralama == "fiyat_cok")
                        sql += " ORDER BY E.Fiyat DESC";
                }

                using var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(sehir))
                    cmd.Parameters.AddWithValue("@sehir", $"%{sehir}%");
                if (minFiyat.HasValue)
                    cmd.Parameters.AddWithValue("@minFiyat", minFiyat.Value);
                if (maxFiyat.HasValue)
                    cmd.Parameters.AddWithValue("@maxFiyat", maxFiyat.Value);

                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    evler.Add(new Ev
                    {
                        EvID = (int)dr["EvID"],
                        Baslik = dr["Baslik"]?.ToString() ?? "",
                        Aciklama = dr["Aciklama"]?.ToString() ?? "",
                        Fiyat = (decimal)dr["Fiyat"],
                        Konum = dr["Konum"]?.ToString() ?? "",
                        Durum = dr["DurumAdi"]?.ToString() ?? "",
                        Resimler = new List<string>()
                    });
                }
            }

            foreach (var ev in evler)
            {
                using var conn2 = new SqlConnection(_conStr);
                conn2.Open();
                var resimCmd = new SqlCommand("SELECT FotoUrl FROM EvFoto WHERE EvID = @EvID", conn2);
                resimCmd.Parameters.AddWithValue("@EvID", ev.EvID);
                using var resimDr = resimCmd.ExecuteReader();
                while (resimDr.Read())
                {
                    var url = resimDr["FotoUrl"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(url))
                        ev.Resimler.Add(url);
                }
            }

            return View(evler);
        }


        // Ev detay (yorumlar, resimler, rezervasyon)
        public IActionResult EvDetay(int id)
        {
            Ev? ev = null;
            // Temel ev bilgileri
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                var sql = @"
                    SELECT E.EvID, E.Baslik, E.Aciklama, E.Fiyat,
                           K.Sehir + ', ' + K.Ilce AS Konum, D.DurumAdi
                    FROM TinyHouse E
                    INNER JOIN Konum K ON E.KonumID = K.KonumID
                    INNER JOIN TinyHouseDurum D ON E.DurumID = D.DurumID
                    WHERE E.EvID = @id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ev = new Ev
                            {
                                EvID = (int)dr["EvID"],
                                Baslik = dr["Baslik"]?.ToString() ?? "",
                                Aciklama = dr["Aciklama"]?.ToString() ?? "",
                                Fiyat = (decimal)dr["Fiyat"],
                                Konum = dr["Konum"]?.ToString() ?? "",
                                Durum = dr["DurumAdi"]?.ToString() ?? "",
                                Resimler = new List<string>(),
                                Yorumlar = new List<Yorum>()
                            };
                        }
                    }
                }
            }
            if (ev != null)
            {
                // Resimler (Ayrı connection)
                using (var resimConn = new SqlConnection(_conStr))
                {
                    resimConn.Open();
                    using (var resimCmd = new SqlCommand("SELECT FotoUrl FROM EvFoto WHERE EvID = @EvID", resimConn))
                    {
                        resimCmd.Parameters.AddWithValue("@EvID", ev.EvID);
                        using (var resimDr = resimCmd.ExecuteReader())
                        {
                            while (resimDr.Read())
                                ev.Resimler.Add(resimDr["FotoUrl"]?.ToString() ?? "");
                        }
                    }
                }

                // Yorumlar (Ayrı connection)
                var yorumlar = new List<Yorum>();
                using (var yorumConn = new SqlConnection(_conStr))
                {
                    yorumConn.Open();
                    var yorumSql = @"
                        SELECT Y.Puan, Y.YorumMetni, Y.YorumTarihi
                        FROM Yorum Y
                        INNER JOIN Rezervasyon R ON Y.RezervasyonID = R.RezervasyonID
                        WHERE R.EvID = @EvID
                        ORDER BY Y.YorumTarihi DESC";
                    using (var yorumCmd = new SqlCommand(yorumSql, yorumConn))
                    {
                        yorumCmd.Parameters.AddWithValue("@EvID", ev.EvID);
                        using (var yorumDr = yorumCmd.ExecuteReader())
                        {
                            while (yorumDr.Read())
                            {
                                yorumlar.Add(new Yorum
                                {
                                    Puan = (int)yorumDr["Puan"],
                                    YorumMetni = yorumDr["YorumMetni"]?.ToString() ?? "",
                                    YorumTarihi = (DateTime)yorumDr["YorumTarihi"]
                                });
                            }
                        }
                    }
                }
                ev.Yorumlar = yorumlar;

                // Kullanıcının bu eve ait rezervasyonu (Ayrı connection)
                int? gecerliRezervasyonID = null;
                using (var rezervasyonConn = new SqlConnection(_conStr))
                {
                    rezervasyonConn.Open();
                    var rezervasyonSql = @"
                        SELECT TOP 1 RezervasyonID 
                        FROM Rezervasyon 
                        WHERE EvID = @EvID 
                          AND KiraciID = @KiraciID 
                          AND DurumID IN (2, 4)
                        ORDER BY BaslangicTarihi DESC";
                    using (var rezervasyonCmd = new SqlCommand(rezervasyonSql, rezervasyonConn))
                    {
                        rezervasyonCmd.Parameters.AddWithValue("@EvID", ev.EvID);
                        int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
                        if (kullaniciID != null)
                        {
                            rezervasyonCmd.Parameters.AddWithValue("@KiraciID", kullaniciID.Value);
                            var result = rezervasyonCmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                                gecerliRezervasyonID = Convert.ToInt32(result);
                        }
                    }
                }
                ViewBag.GecerliRezervasyonID = gecerliRezervasyonID;
            }

            if (ev == null)
                return NotFound();
            return View(ev);
        }

        // Kiracı dashboardu (istatistik ve popüler evler)
        public IActionResult Dashboard()
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null)
                return RedirectToAction("Giris", "Kullanici");
            ViewBag.Ad = HttpContext.Session.GetString("AdSoyad") ?? "Kiracı";
            var populerEvler = new List<Ev>();
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                string sql = @"
                    SELECT TOP 5 E.EvID, E.Baslik, E.Aciklama, E.Fiyat,
                           K.Sehir + ', ' + K.Ilce AS Konum, D.DurumAdi,
                           dbo.fn_OrtalamaPuanHesapla(E.EvID) AS OrtalamaPuan
                    FROM TinyHouse E
                    INNER JOIN Konum K ON E.KonumID = K.KonumID
                    INNER JOIN TinyHouseDurum D ON E.DurumID = D.DurumID
                    WHERE D.DurumAdi = 'Aktif'
                    ORDER BY OrtalamaPuan DESC";
                using var cmd = new SqlCommand(sql, conn);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    populerEvler.Add(new Ev
                    {
                        EvID = (int)dr["EvID"],
                        Baslik = dr["Baslik"]?.ToString() ?? "",
                        Aciklama = dr["Aciklama"]?.ToString() ?? "",
                        Fiyat = (decimal)dr["Fiyat"],
                        Konum = dr["Konum"]?.ToString() ?? "",
                        Durum = dr["DurumAdi"]?.ToString() ?? "",
                        OrtalamaPuan = dr["OrtalamaPuan"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["OrtalamaPuan"]),
                        Resimler = new List<string>()
                    });
                }
                // Popüler evler için resim ekle
                foreach (var ev in populerEvler)
                {
                    using (var resimConn = new SqlConnection(_conStr))
                    {
                        resimConn.Open();
                        string resimSql = "SELECT FotoUrl FROM EvFoto WHERE EvID = @EvID";
                        using (var resimCmd = new SqlCommand(resimSql, resimConn))
                        {
                            resimCmd.Parameters.AddWithValue("@EvID", ev.EvID);
                            using (var resimDr = resimCmd.ExecuteReader())
                            {
                                while (resimDr.Read())
                                    ev.Resimler.Add(resimDr["FotoUrl"]?.ToString() ?? "");
                            }
                        }
                    }
                }
            }
            int toplamRezervasyon = 0;
            decimal toplamOdeme = 0;
            DateTime? sonRezervasyonTarihi = null;
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Rezervasyon WHERE KiraciID = @KiraciID", conn))
                {
                    cmd.Parameters.AddWithValue("@KiraciID", kullaniciId.Value);
                    toplamRezervasyon = (int)cmd.ExecuteScalar();
                }
                using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(Tutar),0) FROM Odeme O
                    INNER JOIN Rezervasyon R ON O.RezervasyonID = R.RezervasyonID
                    WHERE R.KiraciID = @KiraciID", conn))
                {
                    cmd.Parameters.AddWithValue("@KiraciID", kullaniciId.Value);
                    toplamOdeme = (decimal)cmd.ExecuteScalar();
                }
                using (var cmd = new SqlCommand("SELECT MAX(BaslangicTarihi) FROM Rezervasyon WHERE KiraciID = @KiraciID", conn))
                {
                    cmd.Parameters.AddWithValue("@KiraciID", kullaniciId.Value);
                    var result = cmd.ExecuteScalar();
                    sonRezervasyonTarihi = result == DBNull.Value ? (DateTime?)null : (DateTime)result;
                }
            }
            var model = new DashboardViewModel
            {
                PopulerEvler = populerEvler,
                ToplamRezervasyon = toplamRezervasyon,
                ToplamOdeme = toplamOdeme,
                SonRezervasyon = sonRezervasyonTarihi
            };
            return View(model);
        }

        // Profilim
        public IActionResult Profilim()
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return RedirectToAction("Giris", "Kullanici");
            var model = new Profil();
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT Ad,Soyad, Eposta, Telefon, R.RolAdi
                    FROM Kullanici K
                    INNER JOIN Rol R ON K.RolID = R.RolID
                    WHERE KullaniciID = @id", conn);
                cmd.Parameters.AddWithValue("@id", kullaniciID.Value);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model.Ad = reader["Ad"]?.ToString() ?? "";
                    model.Soyad = reader["Soyad"]?.ToString() ?? "";
                    model.Eposta = reader["Eposta"]?.ToString() ?? "";
                    model.Telefon = reader["Telefon"]?.ToString() ?? "";
                    model.Rol = reader["RolAdi"]?.ToString() ?? "";
                }
            }
            return View(model);
        }

        // Rezervasyonlarım
        public IActionResult Rezervasyonlarim()
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return RedirectToAction("Giris", "Kullanici");
            var rezervasyonlar = new List<Rezervasyon>();
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                string sql = @"
                    SELECT R.*, E.Baslik
                    FROM Rezervasyon R
                    INNER JOIN TinyHouse E ON R.EvID = E.EvID
                    WHERE R.KiraciID = @id
                    ORDER BY R.OlusturmaTarihi DESC";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", kullaniciID.Value);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    rezervasyonlar.Add(new Rezervasyon
                    {
                        RezervasyonID = (int)dr["RezervasyonID"],
                        Baslik = dr["Baslik"]?.ToString() ?? "",
                        BaslangicTarihi = (DateTime)dr["BaslangicTarihi"],
                        BitisTarihi = (DateTime)dr["BitisTarihi"],
                        DurumID = (int)dr["DurumID"],
                        ToplamOdeme = dr["ToplamOdeme"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["ToplamOdeme"]),
                        OdemeDurumu = dr["OdemeDurumu"]?.ToString() ?? ""
                    });
                }
            }
            return View(rezervasyonlar);
        }

        // Rezervasyon iptal
        [HttpPost]
        public IActionResult RezervasyonIptal(int rezervasyonID)
        {
            int? kullaniciID = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciID == null)
                return RedirectToAction("Giris", "Kullanici");
            using (var conn = new SqlConnection(_conStr))
            {
                conn.Open();
                string kontrolSql = "SELECT COUNT(*) FROM Rezervasyon WHERE RezervasyonID = @rezervasyonID AND KiraciID = @kullaniciID";
                using var kontrolCmd = new SqlCommand(kontrolSql, conn);
                kontrolCmd.Parameters.AddWithValue("@rezervasyonID", rezervasyonID);
                kontrolCmd.Parameters.AddWithValue("@kullaniciID", kullaniciID.Value);
                int count = (int)kontrolCmd.ExecuteScalar();
                if (count == 0)
                {
                    TempData["Hata"] = "Bu işlem sizin rezervasyonunuza ait değil.";
                    return RedirectToAction("Rezervasyonlarim");
                }
                string durumSql = "SELECT DurumID FROM RezervasyonDurum WHERE DurumAdi = 'Iptal'";
                using var durumCmd = new SqlCommand(durumSql, conn);
                object durumObj = durumCmd.ExecuteScalar();
                if (durumObj == null)
                    return BadRequest("İptal durumu bulunamadı.");
                int iptalDurumID = (int)durumObj;
                string updateSql = "UPDATE Rezervasyon SET DurumID = @durumID WHERE RezervasyonID = @rezervasyonID";
                using var updateCmd = new SqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@durumID", iptalDurumID);
                updateCmd.Parameters.AddWithValue("@rezervasyonID", rezervasyonID);
                updateCmd.ExecuteNonQuery();
                TempData["Basarili"] = "Rezervasyonunuz iptal edildi.";
                return RedirectToAction("Rezervasyonlarim");
            }
        }
    }
}
