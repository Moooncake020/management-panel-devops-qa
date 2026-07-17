namespace YonetimPaneli.ViewModels.Profil
{
    public class ProfilIndexViewModel
    {
        public int KullaniciId { get; set; }
        public string TamAd { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BasHarfler { get; set; } = "?";
        public string DepartmanAdi { get; set; } = "Atanmamış";
        public string UnvanAdi { get; set; } = "Atanmamış";
        public string KidemAdi { get; set; } = string.Empty;
        public string OrganizasyonRoluAdi { get; set; } = string.Empty;
        public string SistemRolu { get; set; } = "Kullanıcı";
        public bool AktifMi { get; set; }
        public bool KendiProfiliMi { get; set; }
        public bool IsYukuGorebilirMi { get; set; }
        public bool DuzenleyebilirMi { get; set; }

        public ProfilKullaniciOzetViewModel? Yonetici { get; set; }
        public List<ProfilKullaniciOzetViewModel> DogrudanAstlar { get; set; } = new();

        public int ToplamAtananGorev { get; set; }
        public int AktifGorev { get; set; }
        public int GecikenGorev { get; set; }
        public int BugunBitenGorev { get; set; }
        public int KritikAktifGorev { get; set; }
        public int TamamlananGorev { get; set; }
        public int BuHaftaTamamlanan { get; set; }
        public int OlusturduguGorev { get; set; }
        public int TamamlanmaOrani { get; set; }

        public int AcikGorev { get; set; }
        public int DevamEdenGorev { get; set; }
        public int QaGorevi { get; set; }
        public int BugHataGorevi { get; set; }

        public List<ProfilGorevOzetViewModel> SonGorevler { get; set; } = new();
        public List<ProfilAktiviteOzetViewModel> SonAktiviteler { get; set; } = new();
    }
}
