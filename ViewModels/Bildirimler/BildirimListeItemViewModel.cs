namespace YonetimPaneli.ViewModels.Bildirimler
{
    public class BildirimListeItemViewModel
    {
        public int Id { get; set; }
        public string Tur { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool OkunduMu { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
        public string GecenSureMetni { get; set; } = string.Empty;
        public string IkonKodu { get; set; } = "bilgi";
        public string RenkKodu { get; set; } = "slate";
        public string KategoriKodu { get; set; } = "diger";
        public string KategoriMetni { get; set; } = "Diğer";
        public string TarihGrubu { get; set; } = "Daha önce";
    }
}
