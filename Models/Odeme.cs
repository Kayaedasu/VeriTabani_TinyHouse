using System;

namespace TinyHouse.Models
{
    public class Odeme
    {
        public int OdemeID { get; set; }
        public int RezervasyonID { get; set; }
        public string OdemeYontemi { get; set; } =string.Empty;
        public string KartNumarasi { get; set; } = string.Empty;
        public string KartNumarasiMaskeli { get; set; } = string.Empty;
        public DateTime OdemeTarihi { get; set; }
        public decimal Tutar { get; set; }
    }
}
