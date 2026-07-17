using YonetimPaneli.Models;

namespace YonetimPaneli.ViewModels.Organizasyon
{
    /// <summary>
    /// Departman ve unvan yönetim sayfasındaki iki yönetim alanını bir arada taşır.
    /// </summary>
    public class OrganizasyonYonetimViewModel
    {
        public List<DepartmanYonetimItemViewModel> Departmanlar { get; set; } = new();
        public List<UnvanYonetimItemViewModel> Unvanlar { get; set; } = new();
        public List<Departman> UstDepartmanSecenekleri { get; set; } = new();
        public DepartmanFormViewModel YeniDepartman { get; set; } = new();
        public UnvanFormViewModel YeniUnvan { get; set; } = new();
        public int DepartmanaAtanmamisKullaniciSayisi { get; set; }
        public int UnvaniAtanmamisKullaniciSayisi { get; set; }
    }
}
