namespace YonetimPaneli.ViewModels.Organizasyon
{
    public class UnvanYonetimItemViewModel
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public int KullaniciSayisi { get; set; }
    }
}
