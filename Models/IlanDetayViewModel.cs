namespace TinyHouse.Models
{
    public class IlanDetayViewModel
    {
        public EvSahibiTinyHouse Ilan { get; set; }
        public List<EvSahibiRezervasyon> Rezervasyonlar { get; set; }
        public List<Yorum> Yorumlar { get; set; }
    }
}
