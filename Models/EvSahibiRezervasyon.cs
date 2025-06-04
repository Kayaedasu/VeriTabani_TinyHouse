using System;

namespace TinyHouse.Models
{
    public class EvSahibiRezervasyon
    {
        public int RezervasyonID { get; set; }
        public int KiraciID { get; set; }
        public int EvID { get; set; }
        public int DurumID { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public string Aciklama { get; set; }

        // View için eklenen alanlar
        public string DurumAdi { get; set; }    // Rezervasyon durumunun adı (ör: Beklemede, Onaylandı)
        public string KiraciAdi { get; set; }   // Kiracı adı soyadı
        public int EvSahibiID { get; set; }     // Ev sahibinin ID'si (rezervasyonun ait olduğu evin sahibi)
        public string EvFotoUrl { get; set; }   // Ev için birinci fotoğrafın URL'si
        public string OdemeDurumu { get; set; }


    }
}
