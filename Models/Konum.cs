namespace TinyHouse.Models
{
    public class Konum
    {
        public int KonumID { get; set; }

        // Nullable yapabilir veya varsayılan boş değer atayabilirsiniz
        public string Sehir { get; set; } = string.Empty;

        public string Ilce { get; set; } = string.Empty;

        public string Ulke { get; set; } = string.Empty;

        // Alternatif olarak şöyle de olabilir:
        // public string? Sehir { get; set; }
        // public string? Ilce { get; set; }
        // public string? Ulke { get; set; }
    }
}
