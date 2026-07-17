namespace YonetimPaneli.Models.Enums
{
    /// <summary>
    /// Bildirim türlerinin controller ve view dosyalarında farklı yazımlarla
    /// çoğalmasını engelleyen merkezi sabitlerdir.
    /// </summary>
    public static class BildirimTurleri
    {
        public const string GorevAtandi = "GorevAtandi";
        public const string GorevDevredildi = "GorevDevredildi";
        public const string DurumDegisti = "DurumDegisti";
        public const string YorumEklendi = "YorumEklendi";
        public const string SonTarihYaklasiyor = "SonTarihYaklasiyor";
        public const string GorevBugunBitiyor = "GorevBugunBitiyor";
        public const string GorevGecikti = "GorevGecikti";
    }
}
