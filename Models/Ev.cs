namespace TinyHouse.Models
{
    public class Ev
    {
        public int EvID { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
        public string Konum { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;

      

        // Çoklu resim desteği
        public List<string> Resimler { get; set; } = new List<string>();

        // Ortalama puan (0 ile 5 arası)
        public decimal OrtalamaPuan { get; set; }

        // Ev ile ilişkili yorumlar
        public List<Yorum> Yorumlar { get; set; } = new List<Yorum>();
    }
}
