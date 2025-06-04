using System;

namespace TinyHouse.Models
{
    public class OdemeViewModel
    {
        public int OdemeID { get; set; }
        public int RezervasyonID { get; set; }
        public decimal Tutar { get; set; }
        public DateTime OdemeTarihi { get; set; }
        public string OdemeYontemi { get; set; } = string.Empty;
    }
}
