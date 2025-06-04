using System;

namespace TinyHouse.Models { 
public class AdminDashboardViewModel
{
    public int ToplamKullanici { get; set; }
    public int AktifKullanici { get; set; }
    public int PasifKullanici { get; set; }

    public decimal ToplamGelir { get; set; }
    public int ToplamOdemeSayisi { get; set; }

    public int ToplamRezervasyon { get; set; }
    public int BekleyenRezervasyon { get; set; }
    public int OnaylananRezervasyon { get; set; }
    public int IptalEdilenRezervasyon { get; set; }

    // Daha fazla alan eklenebilir
}
}
