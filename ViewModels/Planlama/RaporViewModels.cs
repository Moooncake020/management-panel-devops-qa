namespace YonetimPaneli.ViewModels.Planlama
{
    public class RaporDagilimItemViewModel
    {
        public string Etiket { get; set; } = string.Empty;
        public string Kod { get; set; } = string.Empty;
        public int Deger { get; set; }
        public int Yuzde { get; set; }
    }

    public class RaporTrendItemViewModel
    {
        public string Etiket { get; set; } = string.Empty;
        public DateTime Baslangic { get; set; }
        public int Olusturulan { get; set; }
        public int Tamamlanan { get; set; }
    }

    public class RaporDepartmanItemViewModel
    {
        public string Departman { get; set; } = "Departman yok";
        public int AktifGorev { get; set; }
        public int Geciken { get; set; }
        public int DonemdeTamamlanan { get; set; }
        public int Kritik { get; set; }
    }

    public class RaporIndexViewModel
    {
        public int DonemGun { get; set; } = 30;
        public DateTime DonemBaslangici { get; set; }
        public int GorulebilirToplam { get; set; }
        public int DonemdeOlusturulan { get; set; }
        public int DonemdeTamamlanan { get; set; }
        public int AktifGorev { get; set; }
        public int GecikenGorev { get; set; }
        public int TamamlanmaOrani { get; set; }
        public int GecikmeOrani { get; set; }
        public double OrtalamaTamamlanmaGunu { get; set; }
        public List<RaporDagilimItemViewModel> DurumDagilimi { get; set; } = new();
        public List<RaporDagilimItemViewModel> OncelikDagilimi { get; set; } = new();
        public List<RaporTrendItemViewModel> HaftalikTrend { get; set; } = new();
        public List<RaporDepartmanItemViewModel> Departmanlar { get; set; } = new();
        public List<PlanlamaGorevItemViewModel> RiskliGorevler { get; set; } = new();
        public int TrendMaksimum { get; set; } = 1;
    }
}
