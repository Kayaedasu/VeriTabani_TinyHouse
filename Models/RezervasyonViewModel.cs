using System;

namespace TinyHouse.Models
{
    public class RezervasyonViewModel
    {
        public int RezervasyonID { get; set; }
        public int IlanID { get; set; }       // Veritabanındaki Rezervasyon tablosundaki IlanID (EvID)
        public int KiraciID { get; set; }

        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }

        public int DurumID { get; set; }

        public string DurumAdi { get; set; } = string.Empty;

        public int EvID { get; set; }
        public string EvBasligi { get; set; } = string.Empty;

        public string KiraciAdi { get; set; } = string.Empty;
        public string KiraciSoyadi { get; set; } = string.Empty;

        public bool OdemeYapildiMi { get; set; }
    }
}
