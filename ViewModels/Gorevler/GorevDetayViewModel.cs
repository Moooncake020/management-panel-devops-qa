using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Gorevler
{
    public class GorevDetayViewModel
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string Durum { get; set; } = "Açık";
        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;
        public string OncelikMetni { get; set; } = "Normal";
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? SonTarih { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        public bool GeciktiMi { get; set; }
        public bool BugunBitiyorMu { get; set; }
        public string KalanGunMetni { get; set; } = "Son tarih yok";
        public int? AtananKullaniciId { get; set; }
        public string AtananKisi { get; set; } = "Atanmamış";
        public int? OlusturanKullaniciId { get; set; }
        public string GoreviVeren { get; set; } = "Eski Kayıt";
        public DateTime OlusturulmaTarihi { get; set; }
        public string? ResimYolu { get; set; }

        public bool DuzenleyebilirMi { get; set; }
        public bool SilebilirMi { get; set; }
        public bool YorumYazabilirMi { get; set; }

        public GorevYorumEkleViewModel YeniYorum { get; set; }
            = new GorevYorumEkleViewModel();

        public List<GorevYorumListeItemViewModel> Yorumlar { get; set; }
            = new List<GorevYorumListeItemViewModel>();

        public List<GorevAktiviteListeItemViewModel> Aktiviteler { get; set; }
            = new List<GorevAktiviteListeItemViewModel>();
    }
}
