using YonetimPaneli.ViewModels.Ortak;

namespace YonetimPaneli.ViewModels.Kullanicilar
{
    /// <summary>
    /// Kullanıcı yönetimi sayfasının liste, filtre, sayfalama ve yeni kullanıcı
    /// drawer bilgilerini taşır.
    /// </summary>
    public class KullaniciIndexViewModel
    {
        public KullaniciListeViewModel Liste { get; set; } = new();
        public KullaniciEkleViewModel YeniKullanici { get; set; } = new();
        public SayfalamaViewModel Sayfalama { get; set; } = new();
        public bool YeniKullaniciDrawerAcik { get; set; }

        public string Arama { get; set; } = string.Empty;
        public string Durum { get; set; } = "all";
        public string Rol { get; set; } = "all";
        public int? DepartmanId { get; set; }
        public string Siralama { get; set; } = "ad-asc";
        public int SayfaBoyutu { get; set; } = 20;

        public int FiltrelenmisToplamSayisi { get; set; }
        public int AktifSayisi { get; set; }
        public int PasifSayisi { get; set; }
        public int AdminSayisi { get; set; }
        public int KilitliSayisi { get; set; }
    }
}
