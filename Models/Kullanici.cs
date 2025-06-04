using System;

namespace TinyHouse.Models
{
    public class Kullanici
    {
        public int KullaniciID { get; set; }

        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;

        public DateTime KayitTarihi { get; set; }

        public int RolID { get; set; }
        public bool AktifMi { get; set; }
    }
}
