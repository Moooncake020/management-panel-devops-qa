using YonetimPaneli.ViewModels.Gorevler;

namespace YonetimPaneli.ViewModels.Planlama
{
    public class TakvimEtkinlikViewModel
    {
        public int GorevId { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Tur { get; set; } = "bitis";
        public string TurMetni { get; set; } = "Bitiş";
        public string Durum { get; set; } = "Açık";
        public string DurumKodu { get; set; } = "acik";
        public string OncelikKodu { get; set; } = "normal";
        public string AtananKisi { get; set; } = "Atanmamış";
        public bool GeciktiMi { get; set; }
    }

    public class TakvimGunViewModel
    {
        public DateTime Tarih { get; set; }
        public bool BuAyaAitMi { get; set; }
        public bool BugunMu { get; set; }
        public bool HaftaSonuMu { get; set; }
        public List<TakvimEtkinlikViewModel> Etkinlikler { get; set; } = new();
    }

    public class TakvimIndexViewModel
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public string AyBasligi { get; set; } = string.Empty;
        public DateTime OncekiAy { get; set; }
        public DateTime SonrakiAy { get; set; }
        public List<TakvimGunViewModel> Gunler { get; set; } = new();
        public List<GorevFiltreSecenegiViewModel> AtananKisiSecenekleri { get; set; } = new();
        public string Atanan { get; set; } = "all";
        public int BuAyBaslayan { get; set; }
        public int BuAyBiten { get; set; }
        public int BuAyTamamlanan { get; set; }
        public int Geciken { get; set; }
    }
}
