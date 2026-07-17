namespace YonetimPaneli.Security
{
    /// <summary>
    /// Kimlik doğrulama akışında birden fazla dosyada kullanılan sabitler.
    /// Yazım hatalarını ve cookie/claim adlarının birbirinden kopmasını önler.
    /// </summary>
    public static class GuvenlikSabitleri
    {
        public const string TokenCookieAdi = "yonetim_paneli_token";
        public const string GuvenlikDamgasiClaim = "guvenlik_damgasi";
        public const string LoginRateLimitPolitikasi = "login";
        public const string AramaRateLimitPolitikasi = "arama";
    }
}
