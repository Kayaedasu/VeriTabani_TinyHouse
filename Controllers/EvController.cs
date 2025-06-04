using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace TinyHouse.Controllers
{
    public class EvController : Controller
    {
        private readonly string _conStr;

        // Bağlantı dizesi dependency injection ile alınır
        public EvController(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");
        }

        public IActionResult Index()
        {
            var evler = new List<Ev>();

            using var conn = new SqlConnection(_conStr);
            conn.Open();
            string sql = @"
                SELECT E.EvID, E.Baslik, E.Aciklama, E.Fiyat, 
                       K.Sehir + ', ' + K.Ilce AS Konum,
                       D.DurumAdi
                FROM TinyHouse E
                INNER JOIN Konum K ON E.KonumID = K.KonumID
                INNER JOIN TinyHouseDurum D ON E.DurumID = D.DurumID
                WHERE D.DurumAdi = 'Aktif'
            ";
            using var cmd = new SqlCommand(sql, conn);
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
                    Durum = dr["DurumAdi"]?.ToString() ?? ""
                });
            }

            return View(evler);
        }

        public IActionResult Detay(int id)
        {
            Ev? ev = null;

            using var conn = new SqlConnection(_conStr);
            conn.Open();
            string sql = @"
                SELECT E.EvID, E.Baslik, E.Aciklama, E.Fiyat,
                       K.Sehir + ', ' + K.Ilce AS Konum,
                       D.DurumAdi, 
                FROM TinyHouse E
                INNER JOIN Konum K ON E.KonumID = K.KonumID
                INNER JOIN TinyHouseDurum D ON E.DurumID = D.DurumID
                WHERE E.EvID = @id
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var dr = cmd.ExecuteReader();

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
                  
                };
            }

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        public IActionResult PopulerEvler()
        {
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
            }

            // Her ev için resimleri EvFoto tablosundan çek
            foreach (var ev in populerEvler)
            {
                using var conn2 = new SqlConnection(_conStr);
                conn2.Open();
                string resimSql = "SELECT FotoUrl FROM EvFoto WHERE EvID = @EvID";
                using var resimCmd = new SqlCommand(resimSql, conn2);
                resimCmd.Parameters.AddWithValue("@EvID", ev.EvID);

                using var resimDr = resimCmd.ExecuteReader();
                while (resimDr.Read())
                {
                    var resimUrl = resimDr["FotoUrl"]?.ToString();
                    if (!string.IsNullOrEmpty(resimUrl))
                        ev.Resimler.Add(resimUrl);
                }
            }

            return PartialView("_PopulerEvlerPartial", populerEvler);
        }
    }
}
