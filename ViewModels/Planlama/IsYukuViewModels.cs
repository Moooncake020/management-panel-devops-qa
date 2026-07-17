namespace YonetimPaneli.ViewModels.Planlama
{
    public class IsYukuKullaniciViewModel
    {
        public int KullaniciId { get; set; }
        public string TamAd { get; set; } = string.Empty;
        public string BasHarfler { get; set; } = "?";
        public string Departman { get; set; } = "Departman yok";
        public string Unvan { get; set; } = "Unvan yok";
        public int AcikGorev { get; set; }
        public int DevamEden { get; set; }
        public int QaBekleyen { get; set; }
        public int BugHata { get; set; }
        public int Kritik { get; set; }
        public int Geciken { get; set; }
        public int YediGunIcinde { get; set; }
        public int SonOtuzGundeTamamlanan { get; set; }
        public int YukPuani { get; set; }
        public int YukYuzdesi { get; set; }
        public string YukSeviyesi { get; set; } = "Dengeli";
        public string YukKodu { get; set; } = "balanced";
    }

    public class IsYukuDepartmanSecenegiViewModel
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
    }

    public class IsYukuIndexViewModel
    {
        public List<IsYukuKullaniciViewModel> Kullanicilar { get; set; } = new();
        public List<IsYukuDepartmanSecenegiViewModel> Departmanlar { get; set; } = new();
        public int? DepartmanId { get; set; }
        public int ToplamAktifGorev { get; set; }
        public int ToplamGeciken { get; set; }
        public int AtanmamisGorev { get; set; }
        public int YuksekYukluKisi { get; set; }
        public int EnYuksekPuan { get; set; }
    }
}
