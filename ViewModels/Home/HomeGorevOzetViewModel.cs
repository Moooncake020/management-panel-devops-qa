using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Home
{
    public class HomeGorevOzetViewModel
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Durum { get; set; } = "Açık";
        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;
        public string OncelikMetni { get; set; } = "Normal";
        public string AtananKisi { get; set; } = "Atanmamış";
        public DateTime OlusturulmaTarihi { get; set; }
        public DateTime? SonTarih { get; set; }
        public bool GeciktiMi { get; set; }
        public bool BugunBitiyorMu { get; set; }
        public string KalanGunMetni { get; set; } = "Son tarih yok";
        public string? ResimYolu { get; set; }
    }
}
