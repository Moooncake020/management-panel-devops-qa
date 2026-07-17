using YonetimPaneli.ViewModels.Gorevler;

namespace YonetimPaneli.ViewModels.Planlama
{
    public class KanbanKolonViewModel
    {
        public string Durum { get; set; } = string.Empty;
        public string Kod { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public List<PlanlamaGorevItemViewModel> Gorevler { get; set; } = new();
        public int Toplam => Gorevler.Count;
    }

    public class KanbanIndexViewModel
    {
        public List<KanbanKolonViewModel> Kolonlar { get; set; } = new();
        public List<GorevFiltreSecenegiViewModel> AtananKisiSecenekleri { get; set; } = new();
        public string Arama { get; set; } = string.Empty;
        public string Kapsam { get; set; } = "all";
        public string Atanan { get; set; } = "all";
        public bool TamamlananlariGoster { get; set; }
        public int ToplamGorev { get; set; }
        public int GecikenGorev { get; set; }
        public int KritikGorev { get; set; }
        public int GorevLimiti { get; set; } = 300;
        public bool LimitAsildiMi { get; set; }
    }
}
