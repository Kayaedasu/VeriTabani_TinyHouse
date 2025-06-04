using System;

namespace TinyHouse.Models
{
    public class Yorum

    {
        public YorumCevap Cevap { get; set; }
        public int EvID { get; set; }
        public int YorumID { get; set; }
        public int RezervasyonID { get; set; }
        public int KiraciID { get; set; }
        public int Puan { get; set; }
        public string YorumMetni { get; set; } =string.Empty;
        public DateTime YorumTarihi { get; set; }
        public string Baslik { get; set; } = string.Empty;
    }
}
