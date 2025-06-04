using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    [Route("Odeme")]
    public class AdminOdemeController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminOdemeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Geçmiş ödemeler
        [HttpGet("Gecmis")]
        public IActionResult Gecmis()
        {
            var odemeListesi = new List<OdemeViewModel>();
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            string sql = @"
                SELECT OdemeID, RezervasyonID, Tutar, OdemeTarihi, OdemeYontemi
                FROM Odeme
                ORDER BY OdemeTarihi DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                odemeListesi.Add(new OdemeViewModel
                {
                    OdemeID = reader.GetInt32(reader.GetOrdinal("OdemeID")),
                    RezervasyonID = reader.GetInt32(reader.GetOrdinal("RezervasyonID")),
                    Tutar = reader.GetDecimal(reader.GetOrdinal("Tutar")),
                    OdemeTarihi = reader.GetDateTime(reader.GetOrdinal("OdemeTarihi")),
                    OdemeYontemi = reader.GetString(reader.GetOrdinal("OdemeYontemi"))
                });
            }

            return View("~/Views/Odeme/Gecmis.cshtml", odemeListesi);
        }
    }
}
