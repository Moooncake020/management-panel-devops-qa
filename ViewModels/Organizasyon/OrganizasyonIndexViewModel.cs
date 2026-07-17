namespace YonetimPaneli.ViewModels.Organizasyon
{
    /// <summary>
    /// Organizasyon şeması sayfasının tamamını taşır.
    /// </summary>
    public class OrganizasyonIndexViewModel
    {
        public List<OrganizasyonDugumViewModel> KokDugumler { get; set; } = new();
        public List<string> Departmanlar { get; set; } = new();
        public int AktifKullaniciSayisi { get; set; }
        public int YoneticiSayisi { get; set; }
        public int DepartmanSayisi { get; set; }
        public int EnDerinSeviye { get; set; }
        public bool AdminMi { get; set; }
    }
}
