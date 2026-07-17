using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Kullanicilar
{
    /// <summary>
    /// Kullanıcı ekleme ve düzenleme formlarındaki ortak seçim listelerini taşır.
    /// ViewBag yerine güçlü tip kullanıldığı için alan adları derleme sırasında kontrol edilir.
    /// </summary>
    public class KullaniciFormSecenekleriViewModel
    {
        public List<Departman> Departmanlar { get; set; } = new();
        public List<Unvan> Unvanlar { get; set; } = new();
        public List<AppUser> Yoneticiler { get; set; } = new();

        public KidemSeviyesi[] KidemSeviyeleri { get; set; }
            = Array.Empty<KidemSeviyesi>();

        public OrganizasyonRolu[] OrganizasyonRolleri { get; set; }
            = Array.Empty<OrganizasyonRolu>();
    }
}
