namespace YonetimPaneli.ViewModels.Profil
{
    public class ProfilKullaniciOzetViewModel
    {
        public int Id { get; set; }
        public string TamAd { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UnvanAdi { get; set; } = "Atanmamış";
        public string DepartmanAdi { get; set; } = "Atanmamış";
        public string BasHarfler { get; set; } = "?";
        public bool AktifMi { get; set; }
    }
}
