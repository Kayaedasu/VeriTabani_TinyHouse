using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using TinyHouse.Models;

namespace TinyHouseAdmin.Controllers
{
    public class TestController(IConfiguration configuration) : Controller
    {
        private readonly IConfiguration _configuration = configuration;

        public IActionResult BaglantiTest()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Veritabanı bağlantı dizesi tanımlı değil.");

            try
            {
                using var conn = new SqlConnection(connStr);
                conn.Open();

                string sql = "SELECT COUNT(*) FROM Kullanici";
                using var cmd = new SqlCommand(sql, conn);
                int kullaniciSayisi = (int)(cmd.ExecuteScalar() ?? 0); // null koruması eklenmiş

                return Content($"✅ Bağlantı başarılı! Kullanici tablosunda {kullaniciSayisi} kayıt var.");
            }
            catch (Exception ex)
            {
                return Content($"❌ Bağlantı HATASI: {ex.Message}");
            }
        }
    }
}
