using YonetimPaneli.ViewModels.Ortak;

namespace YonetimPaneli.ViewModels.Bildirimler
{
    public class BildirimIndexViewModel
    {
        public List<BildirimListeItemViewModel> Bildirimler { get; set; } = new();
        public SayfalamaViewModel Sayfalama { get; set; } = new();
        public int ToplamSayisi { get; set; }
        public int OkunmamisSayisi { get; set; }
        public int OkunmusSayisi { get; set; }
        public string AktifFiltre { get; set; } = "tum";
        public string AktifTur { get; set; } = "tum";
        public string? Arama { get; set; }
        public int SayfaBoyutu { get; set; } = 20;
        public int AktifFiltreSayisi { get; set; }
    }
}
