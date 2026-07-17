using Microsoft.EntityFrameworkCore;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.Services
{
    public class GorevYetkiServisi
    {
        private readonly AppDbContext _context;
        private readonly OrganizasyonServisi _organizasyonServisi;

        public GorevYetkiServisi(AppDbContext context, OrganizasyonServisi organizasyonServisi)
        {
            _context = context;
            _organizasyonServisi = organizasyonServisi;
        }

        public bool GorevVerebilirMi(int gorevVerenKullaniciId, int hedefKullaniciId)
        {
            var gorevVeren = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == gorevVerenKullaniciId && u.AktifMi);

            var hedefKullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == hedefKullaniciId && u.AktifMi);

            if (gorevVeren == null || hedefKullanici == null)
            {
                return false;
            }

            if (gorevVeren.Id == hedefKullanici.Id)
            {
                return true;
            }

            if (gorevVeren.Role == "Admin")
            {
                return true;
            }

            switch (gorevVeren.OrganizasyonRolu)
            {
                case OrganizasyonRolu.Calisan:
                    return false;

                case OrganizasyonRolu.TakimLideri:
                    return _organizasyonServisi.DogrudanAstiMi(gorevVeren.Id, hedefKullanici.Id);

                case OrganizasyonRolu.DepartmanMuduru:
                case OrganizasyonRolu.Direktor:
                    return _organizasyonServisi.AltindaCalisiyorMu(gorevVeren.Id, hedefKullanici.Id);

                case OrganizasyonRolu.GenelMudur:
                    return true;

                default:
                    return false;
            }
        }

        public List<AppUser> GorevVerilebilecekKullanicilariGetir(int kullaniciId)
        {
            var kullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == kullaniciId && u.AktifMi);

            if (kullanici == null)
            {
                return new List<AppUser>();
            }

            if (kullanici.Role == "Admin" || kullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur)
            {
                return _context.Kullanicilar
                    .AsNoTracking()
                    .Where(u => u.AktifMi)
                    .OrderBy(u => u.Ad)
                    .ThenBy(u => u.Soyad)
                    .ToList();
            }

            var izinVerilenIdler = new HashSet<int> { kullanici.Id };

            if (kullanici.OrganizasyonRolu == OrganizasyonRolu.TakimLideri)
            {
                var dogrudanAstIdleri = _context.Kullanicilar
                    .AsNoTracking()
                    .Where(u => u.AktifMi && u.YoneticiId == kullanici.Id)
                    .Select(u => u.Id)
                    .ToList();

                foreach (var astId in dogrudanAstIdleri)
                {
                    izinVerilenIdler.Add(astId);
                }
            }

            if (kullanici.OrganizasyonRolu == OrganizasyonRolu.DepartmanMuduru ||
                kullanici.OrganizasyonRolu == OrganizasyonRolu.Direktor)
            {
                var tumAltKullaniciIdleri = _organizasyonServisi.TumAltKullaniciIdleriniGetir(kullanici.Id);

                foreach (var astId in tumAltKullaniciIdleri)
                {
                    izinVerilenIdler.Add(astId);
                }
            }

            return _context.Kullanicilar
                .AsNoTracking()
                .Where(u => u.AktifMi && izinVerilenIdler.Contains(u.Id))
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .ToList();
        }
    }
}
