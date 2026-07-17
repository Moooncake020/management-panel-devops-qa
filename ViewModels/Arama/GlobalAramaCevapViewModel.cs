namespace YonetimPaneli.ViewModels.Arama
{
    public class GlobalAramaCevapViewModel
    {
        public string Sorgu { get; set; } = string.Empty;
        public List<GlobalAramaSonucViewModel> Gorevler { get; set; } = new();
        public List<GlobalAramaSonucViewModel> Kullanicilar { get; set; } = new();
        public int ToplamSonuc => Gorevler.Count + Kullanicilar.Count;
    }
}
