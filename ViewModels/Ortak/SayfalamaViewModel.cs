namespace YonetimPaneli.ViewModels.Ortak
{
    /// <summary>
    /// Liste ekranlarında ortak kullanılan sunucu tarafı sayfalama bilgisidir.
    /// Controller yalnızca mevcut sayfaya ait kayıtları getirir; bu model
    /// görünümün toplam sayfa, başlangıç ve bitiş kaydı gibi bilgileri üretir.
    /// </summary>
    public class SayfalamaViewModel
    {
        public int Sayfa { get; set; } = 1;
        public int SayfaBoyutu { get; set; } = 20;
        public int ToplamKayit { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";

        public int ToplamSayfa => Math.Max(
            1,
            (int)Math.Ceiling(ToplamKayit / (double)Math.Max(1, SayfaBoyutu))
        );

        public bool OncekiVar => Sayfa > 1;
        public bool SonrakiVar => Sayfa < ToplamSayfa;

        public int BaslangicKaydi => ToplamKayit == 0
            ? 0
            : ((Sayfa - 1) * SayfaBoyutu) + 1;

        public int BitisKaydi => Math.Min(
            Sayfa * SayfaBoyutu,
            ToplamKayit
        );
    }
}
