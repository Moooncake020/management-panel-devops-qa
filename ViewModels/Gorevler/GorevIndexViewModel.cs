using YonetimPaneli.ViewModels.Ortak;

namespace YonetimPaneli.ViewModels.Gorevler
{
    /// <summary>
    /// Görev listesi, özet metrikleri, aktif filtreleri ve sayfalama bilgisini
    /// tek modelde taşır. Kayıtlar artık tarayıcıda değil veritabanında filtrelenir.
    /// </summary>
    public class GorevIndexViewModel
    {
        public List<GorevListeItemViewModel> Gorevler { get; set; } = new();
        public List<GorevFiltreSecenegiViewModel> AtananKisiSecenekleri { get; set; } = new();
        public SayfalamaViewModel Sayfalama { get; set; } = new();

        public int AktifKullaniciId { get; set; }
        public bool AdminMi { get; set; }
        public bool GenelMudurMu { get; set; }
        public bool TumGorevleriGoruyor => AdminMi || GenelMudurMu;

        public string Arama { get; set; } = string.Empty;
        public string Durum { get; set; } = "all";
        public string Oncelik { get; set; } = "all";
        public string Tarih { get; set; } = "all";
        public string Kapsam { get; set; } = "all";
        public string Atanan { get; set; } = "all";
        public string Siralama { get; set; } = "newest";
        public int SayfaBoyutu { get; set; } = 20;

        public int GorulebilirToplamSayisi { get; set; }
        public int FiltrelenmisToplamSayisi { get; set; }
        public int AcikSayisi { get; set; }
        public int DevamSayisi { get; set; }
        public int QaSayisi { get; set; }
        public int BugSayisi { get; set; }
        public int TamamlananSayisi { get; set; }
        public int GecikenSayisi { get; set; }
        public int BugunBitenSayisi { get; set; }
        public int KritikSayisi { get; set; }
    }
}
