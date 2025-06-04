public class YorumCevap
{
    public int CevapID { get; set; }
    public int YorumID { get; set; }
    public int EvSahibiID { get; set; }    // Bu satırı ekleyin
    public string CevapMetni { get; set; }
    public DateTime CevapTarihi { get; set; }
}
