namespace YonetimPaneli.ViewModels.Arama
{
    /// <summary>
    /// Global arama penceresinde gösterilen tek bir navigasyon sonucudur.
    /// İstemci yalnızca ekranda gerekli alanları alır; veritabanı modelleri
    /// doğrudan JSON olarak dışarı verilmez.
    /// </summary>
    public class GlobalAramaSonucViewModel
    {
        public string Tur { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Ikon { get; set; } = "arama";
        public string? Rozet { get; set; }
    }
}
