using System;

namespace TinyHouse.Models
{
    public class Ilan
    {
        public int IlanID { get; set; }           // EvID ile eşleşir
        public int EvSahibiID { get; set; }
        public int KonumID { get; set; }
        public int DurumID { get; set; }          // Aktif/pasif durumu

        public string Baslik { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
        public DateTime EklenmeTarihi { get; set; }
    }
}
