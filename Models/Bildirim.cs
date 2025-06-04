using System;

namespace TinyHouse.Models
{
    public class Bildirim
    {
        public int BildirimID { get; set; }
        public int KullaniciID { get; set; }

        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;

        public bool Okundu { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
