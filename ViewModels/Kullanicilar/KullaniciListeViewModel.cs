using YonetimPaneli.Models;

namespace YonetimPaneli.ViewModels.Kullanicilar
{
    public class KullaniciListeViewModel
    {
        public List<AppUser> Kullanicilar { get; set; } = new();
        public int AktifKullaniciId { get; set; }
    }
}
