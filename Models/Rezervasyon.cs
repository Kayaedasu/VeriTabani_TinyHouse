using System;

namespace TinyHouse.Models
{
    public class Rezervasyon
    {
        public int RezervasyonID { get; set; }
        public int IlanID { get; set; }       // EvID ile ilişkili (TinyHouse tablosu)
        public int KiraciID { get; set; }

        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }

        public int DurumID { get; set; }      // Onay durumu için (Beklemede, Onaylandi, Iptal vb.)
        public string Baslik { get; set; } = string.Empty;
        public string OdemeDurumu { get; set; } = string.Empty;
        public decimal ToplamOdeme { get; set; }
        public int EvSahibiID { get; set; }
        public DateTime OlusturmaTarihi { get; set; }  
        public string Aciklama { get; set; } = string.Empty; 
    }
}
