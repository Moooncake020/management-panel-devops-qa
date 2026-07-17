namespace YonetimPaneli.ViewModels.Organizasyon
{
    public class DepartmanYonetimItemViewModel
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public int? UstDepartmanId { get; set; }
        public string UstDepartmanAdi { get; set; } = "Ana departman";
        public int KullaniciSayisi { get; set; }
        public int AltDepartmanSayisi { get; set; }
        public int Seviye { get; set; }
    }
}
