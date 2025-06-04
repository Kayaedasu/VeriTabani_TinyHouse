namespace TinyHouse.Models
{
    public class Profil
    {
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string Telefon { get; set; }=string.Empty;
        public string Rol { get; set; } =string.Empty;
        public string AdSoyad => $"{Ad} {Soyad}";
    }
}
