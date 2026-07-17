using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Profil;

namespace YonetimPaneli.Controllers
{
    [Authorize]
    public class ProfilController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GorevYetkiServisi _gorevYetkiServisi;
        private readonly GorevZamanServisi _gorevZamanServisi;

        public ProfilController(
            AppDbContext context,
            GorevYetkiServisi gorevYetkiServisi,
            GorevZamanServisi gorevZamanServisi)
        {
            _context = context;
            _gorevYetkiServisi = gorevYetkiServisi;
            _gorevZamanServisi = gorevZamanServisi;
        }

        /// <summary>
        /// id verilmezse oturumdaki kullanıcının profilini, id verilirse ilgili
        /// kullanıcının kurumsal profilini gösterir. Temel organizasyon bilgileri
        /// tüm giriş yapmış kullanıcılara açıktır; iş yükü yalnızca yetkili kişilere
        /// gösterilir.
        /// </summary>
        [HttpGet]
        public IActionResult Index(int? id)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var hedefKullaniciId = id ?? aktifKullaniciId.Value;

            var aktifKullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == aktifKullaniciId.Value && u.AktifMi);

            var hedefKullanici = _context.Kullanicilar
                .AsNoTracking()
                .Include(u => u.Departman)
                .Include(u => u.Unvan)
                .Include(u => u.Yonetici)
                    .ThenInclude(y => y!.Departman)
                .Include(u => u.Yonetici)
                    .ThenInclude(y => y!.Unvan)
                .Include(u => u.Astlar)
                    .ThenInclude(a => a.Departman)
                .Include(u => u.Astlar)
                    .ThenInclude(a => a.Unvan)
                .FirstOrDefault(u => u.Id == hedefKullaniciId);

            if (aktifKullanici == null || hedefKullanici == null)
            {
                return NotFound();
            }

            var kendiProfiliMi = aktifKullanici.Id == hedefKullanici.Id;
            var isYukuGorebilirMi = IsYukuGorebilirMi(
                aktifKullanici,
                hedefKullanici
            );

            var model = new ProfilIndexViewModel
            {
                KullaniciId = hedefKullanici.Id,
                TamAd = hedefKullanici.TamAd,
                Email = hedefKullanici.Email ?? "E-posta yok",
                BasHarfler = BasHarfler(hedefKullanici.Ad, hedefKullanici.Soyad),
                DepartmanAdi = hedefKullanici.Departman?.Ad ?? "Atanmamış",
                UnvanAdi = hedefKullanici.Unvan?.Ad ?? "Atanmamış",
                KidemAdi = KidemMetni(hedefKullanici.KidemSeviyesi),
                OrganizasyonRoluAdi = OrganizasyonRoluMetni(
                    hedefKullanici.OrganizasyonRolu),
                SistemRolu = hedefKullanici.Role == "Admin" ? "Admin" : "Kullanıcı",
                AktifMi = hedefKullanici.AktifMi,
                KendiProfiliMi = kendiProfiliMi,
                IsYukuGorebilirMi = isYukuGorebilirMi,
                DuzenleyebilirMi = aktifKullanici.Role == "Admin",
                Yonetici = hedefKullanici.Yonetici == null
                    ? null
                    : KullaniciOzetle(hedefKullanici.Yonetici),
                DogrudanAstlar = hedefKullanici.Astlar
                    .OrderByDescending(a => a.AktifMi)
                    .ThenBy(a => a.Ad)
                    .ThenBy(a => a.Soyad)
                    .Select(KullaniciOzetle)
                    .ToList()
            };

            if (!isYukuGorebilirMi)
            {
                return View(model);
            }

            var hedefIdMetni = hedefKullanici.Id.ToString();
            var atanmisGorevler = _context.Gorevler
                .AsNoTracking()
                .Include(g => g.OlusturanKullanici)
                .Where(g =>
                    g.AtananKullaniciId == hedefKullanici.Id ||
                    g.AtananUserId == hedefIdMetni)
                .OrderByDescending(g => g.OlusturulmaTarihi)
                .ToList();

            var simdi = DateTime.Now;
            var bugun = simdi.Date;
            var haftaninIlkGunu = bugun.AddDays(-(((int)bugun.DayOfWeek + 6) % 7));
            var sonrakiHafta = haftaninIlkGunu.AddDays(7);

            model.ToplamAtananGorev = atanmisGorevler.Count;
            model.AktifGorev = atanmisGorevler.Count(g => g.Durum != "Tamamlandı");
            model.GecikenGorev = atanmisGorevler.Count(g =>
                _gorevZamanServisi.GeciktiMi(g.SonTarih, g.Durum, simdi));
            model.BugunBitenGorev = atanmisGorevler.Count(g =>
                _gorevZamanServisi.BugunBitiyorMu(g.SonTarih, g.Durum, simdi));
            model.KritikAktifGorev = atanmisGorevler.Count(g =>
                g.Oncelik == GorevOnceligi.Kritik &&
                g.Durum != "Tamamlandı");
            model.TamamlananGorev = atanmisGorevler.Count(g => g.Durum == "Tamamlandı");
            model.BuHaftaTamamlanan = atanmisGorevler.Count(g =>
                g.TamamlanmaTarihi.HasValue &&
                g.TamamlanmaTarihi.Value >= haftaninIlkGunu &&
                g.TamamlanmaTarihi.Value < sonrakiHafta);
            model.OlusturduguGorev = _context.Gorevler
                .AsNoTracking()
                .Count(g => g.OlusturanKullaniciId == hedefKullanici.Id);
            model.TamamlanmaOrani = model.ToplamAtananGorev == 0
                ? 0
                : (int)Math.Round(
                    (double)model.TamamlananGorev / model.ToplamAtananGorev * 100);

            model.AcikGorev = atanmisGorevler.Count(g => g.Durum == "Açık");
            model.DevamEdenGorev = atanmisGorevler.Count(g => g.Durum == "Devam Ediyor");
            model.QaGorevi = atanmisGorevler.Count(g => g.Durum == "QA / Test Bekleyen");
            model.BugHataGorevi = atanmisGorevler.Count(g => g.Durum == "Bug / Hata");

            model.SonGorevler = atanmisGorevler
                .OrderBy(g => g.Durum == "Tamamlandı")
                .ThenByDescending(g =>
                    _gorevZamanServisi.GeciktiMi(g.SonTarih, g.Durum, simdi))
                .ThenBy(g => g.SonTarih ?? DateTime.MaxValue)
                .ThenByDescending(g => g.OlusturulmaTarihi)
                .Take(8)
                .Select(g => new ProfilGorevOzetViewModel
                {
                    Id = g.Id,
                    Baslik = g.Baslik ?? "Başlıksız görev",
                    Durum = g.Durum,
                    Oncelik = g.Oncelik,
                    SonTarih = g.SonTarih,
                    GeciktiMi = _gorevZamanServisi.GeciktiMi(
                        g.SonTarih, g.Durum, simdi),
                    BugunBitiyorMu = _gorevZamanServisi.BugunBitiyorMu(
                        g.SonTarih, g.Durum, simdi),
                    KalanGunMetni = _gorevZamanServisi.KalanGunMetni(
                        g.SonTarih, g.Durum, simdi),
                    GoreviVeren = g.OlusturanKullanici?.TamAd ?? "Eski Kayıt"
                })
                .ToList();

            var atanmisGorevIdleri = atanmisGorevler
                .Select(g => g.Id)
                .ToList();

            model.SonAktiviteler = _context.GorevAktiviteleri
                .AsNoTracking()
                .Include(a => a.Gorev)
                .Include(a => a.Kullanici)
                .Where(a => atanmisGorevIdleri.Contains(a.GorevId))
                .OrderByDescending(a => a.OlusturulmaTarihi)
                .Take(10)
                .ToList()
                .Select(a => new ProfilAktiviteOzetViewModel
                {
                    GorevId = a.GorevId,
                    GorevBasligi = a.Gorev.Baslik ?? "Başlıksız görev",
                    IslemTuru = a.IslemTuru,
                    Aciklama = a.Aciklama,
                    IslemiYapan = a.Kullanici?.TamAd ?? "Sistem",
                    OlusturulmaTarihi = a.OlusturulmaTarihi
                })
                .ToList();

            return View(model);
        }

        private bool IsYukuGorebilirMi(AppUser aktifKullanici, AppUser hedefKullanici)
        {
            if (aktifKullanici.Id == hedefKullanici.Id ||
                aktifKullanici.Role == "Admin" ||
                aktifKullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur)
            {
                return true;
            }

            return _gorevYetkiServisi.GorevVerebilirMi(
                aktifKullanici.Id,
                hedefKullanici.Id
            );
        }

        private static ProfilKullaniciOzetViewModel KullaniciOzetle(AppUser kullanici)
        {
            return new ProfilKullaniciOzetViewModel
            {
                Id = kullanici.Id,
                TamAd = kullanici.TamAd,
                Email = kullanici.Email ?? "E-posta yok",
                UnvanAdi = kullanici.Unvan?.Ad ?? "Atanmamış",
                DepartmanAdi = kullanici.Departman?.Ad ?? "Atanmamış",
                BasHarfler = BasHarfler(kullanici.Ad, kullanici.Soyad),
                AktifMi = kullanici.AktifMi
            };
        }

        private static string BasHarfler(string? ad, string? soyad)
        {
            var ilk = string.IsNullOrWhiteSpace(ad) ? string.Empty : ad.Trim()[0].ToString();
            var ikinci = string.IsNullOrWhiteSpace(soyad) ? string.Empty : soyad.Trim()[0].ToString();
            var sonuc = (ilk + ikinci).ToUpperInvariant();
            return string.IsNullOrWhiteSpace(sonuc) ? "?" : sonuc;
        }

        private static string KidemMetni(KidemSeviyesi kidem)
        {
            return kidem == KidemSeviyesi.MidLevel
                ? "Mid-Level"
                : kidem.ToString();
        }

        private static string OrganizasyonRoluMetni(OrganizasyonRolu rol)
        {
            return rol switch
            {
                OrganizasyonRolu.Calisan => "Çalışan",
                OrganizasyonRolu.TakimLideri => "Takım Lideri",
                OrganizasyonRolu.DepartmanMuduru => "Departman Müdürü",
                OrganizasyonRolu.Direktor => "Direktör",
                OrganizasyonRolu.GenelMudur => "Genel Müdür",
                _ => rol.ToString()
            };
        }

        private int? AktifKullaniciIdGetir()
        {
            var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimDegeri, out var kullaniciId)
                ? kullaniciId
                : null;
        }
    }
}
