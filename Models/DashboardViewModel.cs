namespace TinyHouse.Models
{
    public class DashboardViewModel
    {
        public string KullaniciAdi { get; set; } = string.Empty;
        public int ToplamRezervasyon { get; set; }
        public decimal ToplamOdeme { get; set; }
        public DateTime? SonRezervasyon { get; set; }
        public List<Ev> PopulerEvler { get; set; } = new List<Ev>();


    }
}
