namespace YonetimPaneli.Models.Enums
{
    /// <summary>
    /// Veritabanında saklanan aktivite türlerinin tek merkezden yönetilmesini
    /// sağlar. String sabit kullanılması eski kayıtlarla okunabilirliği korur.
    /// </summary>
    public static class GorevAktiviteTurleri
    {
        public const string SistemKaydi = "SistemKaydi";
        public const string Olusturuldu = "Olusturuldu";
        public const string DurumDegisti = "DurumDegisti";
        public const string AtananDegisti = "AtananDegisti";
        public const string IcerikGuncellendi = "IcerikGuncellendi";
        public const string GorselGuncellendi = "GorselGuncellendi";
        public const string OncelikDegisti = "OncelikDegisti";
        public const string TarihPlaniDegisti = "TarihPlaniDegisti";
        public const string YorumEklendi = "YorumEklendi";
        public const string YorumSilindi = "YorumSilindi";
        public const string GorevDevredildi = "GorevDevredildi";
    }
}
