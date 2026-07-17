using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Profil
{
    public class ProfilGorevOzetViewModel
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Durum { get; set; } = "Açık";
        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;
        public DateTime? SonTarih { get; set; }
        public bool GeciktiMi { get; set; }
        public bool BugunBitiyorMu { get; set; }
        public string KalanGunMetni { get; set; } = "Son tarih yok";
        public string GoreviVeren { get; set; } = "Eski Kayıt";
    }
}
