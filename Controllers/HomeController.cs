using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Diagnostics;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Home;

namespace YonetimPaneli.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GorevYetkiServisi _gorevYetkiServisi;
        private readonly GorevZamanServisi _gorevZamanServisi;

        public HomeController(
            AppDbContext context,
            GorevYetkiServisi gorevYetkiServisi,
            GorevZamanServisi gorevZamanServisi)
        {
            _context = context;
            _gorevYetkiServisi = gorevYetkiServisi;
            _gorevZamanServisi = gorevZamanServisi;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken = default)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var aktifKullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Where(u => u.Id == aktifKullaniciId.Value)
                .Select(u => new
                {
                    u.Id,
                    u.Role,
                    u.OrganizasyonRolu
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (aktifKullanici == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            IQueryable<Gorev> gorulebilirSorgu = _context.Gorevler
                .AsNoTracking();

            if (aktifKullanici.Role != "Admin" &&
                aktifKullanici.OrganizasyonRolu != OrganizasyonRolu.GenelMudur)
            {
                var izinliKullaniciIdleri = _gorevYetkiServisi
                    .GorevVerilebilecekKullanicilariGetir(aktifKullanici.Id)
                    .Select(u => u.Id)
                    .ToList();
                var aktifKullaniciIdString = aktifKullanici.Id.ToString();

                gorulebilirSorgu = gorulebilirSorgu.Where(g =>
                    g.AtananKullaniciId == aktifKullanici.Id ||
                    g.OlusturanKullaniciId == aktifKullanici.Id ||
                    g.AtananUserId == aktifKullaniciIdString ||
                    (g.AtananKullaniciId.HasValue &&
                     izinliKullaniciIdleri.Contains(g.AtananKullaniciId.Value)));
            }

            var simdi = DateTime.Now;
            var bugun = simdi.Date;
            var yarin = bugun.AddDays(1);
            var haftaninIlkGunu = bugun.AddDays(-(((int)bugun.DayOfWeek + 6) % 7));
            var sonrakiHafta = haftaninIlkGunu.AddDays(7);

            var metrikler = await gorulebilirSorgu
                .GroupBy(_ => 1)
                .Select(grup => new
                {
                    Toplam = grup.Count(),
                    Acik = grup.Count(g => g.Durum == "Açık"),
                    Devam = grup.Count(g => g.Durum == "Devam Ediyor"),
                    Qa = grup.Count(g => g.Durum == "QA / Test Bekleyen"),
                    Bug = grup.Count(g => g.Durum == "Bug / Hata"),
                    Tamamlanan = grup.Count(g => g.Durum == "Tamamlandı"),
                    Geciken = grup.Count(g =>
                        g.Durum != "Tamamlandı" &&
                        g.SonTarih.HasValue &&
                        g.SonTarih.Value < bugun),
                    BugunBiten = grup.Count(g =>
                        g.Durum != "Tamamlandı" &&
                        g.SonTarih.HasValue &&
                        g.SonTarih.Value >= bugun &&
                        g.SonTarih.Value < yarin),
                    KritikAktif = grup.Count(g =>
                        g.Durum != "Tamamlandı" &&
                        g.Oncelik == GorevOnceligi.Kritik),
                    BuHaftaTamamlanan = grup.Count(g =>
                        g.TamamlanmaTarihi.HasValue &&
                        g.TamamlanmaTarihi.Value >= haftaninIlkGunu &&
                        g.TamamlanmaTarihi.Value < sonrakiHafta)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var sonGorevler = await gorulebilirSorgu
                .Include(g => g.AtananKullanici)
                .OrderByDescending(g => g.OlusturulmaTarihi)
                .Take(6)
                .ToListAsync(cancellationToken);

            var dikkatGerektirenler = await gorulebilirSorgu
                .Where(g =>
                    g.Durum != "Tamamlandı" &&
                    ((g.SonTarih.HasValue && g.SonTarih.Value < yarin) ||
                     g.Oncelik == GorevOnceligi.Kritik))
                .Include(g => g.AtananKullanici)
                .OrderByDescending(g =>
                    g.SonTarih.HasValue && g.SonTarih.Value < bugun)
                .ThenByDescending(g => g.Oncelik)
                .ThenBy(g => g.SonTarih ?? DateTime.MaxValue)
                .Take(6)
                .ToListAsync(cancellationToken);

            HomeGorevOzetViewModel OzetOlustur(Gorev gorev) => new()
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik ?? "Başlıksız görev",
                Durum = gorev.Durum ?? "Açık",
                Oncelik = gorev.Oncelik,
                OncelikMetni = _gorevZamanServisi.OncelikMetni(gorev.Oncelik),
                AtananKisi = gorev.AtananKullanici?.TamAd
                    ?? gorev.AtananKullaniciAdi
                    ?? "Atanmamış",
                OlusturulmaTarihi = gorev.OlusturulmaTarihi,
                SonTarih = gorev.SonTarih,
                GeciktiMi = _gorevZamanServisi.GeciktiMi(
                    gorev.SonTarih, gorev.Durum, simdi),
                BugunBitiyorMu = _gorevZamanServisi.BugunBitiyorMu(
                    gorev.SonTarih, gorev.Durum, simdi),
                KalanGunMetni = _gorevZamanServisi.KalanGunMetni(
                    gorev.SonTarih, gorev.Durum, simdi),
                ResimYolu = gorev.ResimYolu
            };

            var model = new HomeIndexViewModel
            {
                ToplamGorev = metrikler?.Toplam ?? 0,
                AcikGorev = metrikler?.Acik ?? 0,
                DevamEden = metrikler?.Devam ?? 0,
                QaBekleyen = metrikler?.Qa ?? 0,
                BugHata = metrikler?.Bug ?? 0,
                Tamamlanan = metrikler?.Tamamlanan ?? 0,
                Geciken = metrikler?.Geciken ?? 0,
                BugunBiten = metrikler?.BugunBiten ?? 0,
                KritikAktif = metrikler?.KritikAktif ?? 0,
                BuHaftaTamamlanan = metrikler?.BuHaftaTamamlanan ?? 0,
                SonGorevler = sonGorevler.Select(OzetOlustur).ToList(),
                DikkatGerektirenGorevler = dikkatGerektirenler
                    .Select(OzetOlustur)
                    .ToList()
            };

            return View(model);
        }


        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                StatusCode = 500,
                Baslik = "Beklenmeyen bir hata oluştu",
                Mesaj = "İşleminiz tamamlanamadı. Sorun devam ederse istek kodunu sistem yöneticisine iletin.",
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCode(int code)
        {
            Response.StatusCode = code;

            var (baslik, mesaj) = code switch
            {
                404 => ("Sayfa bulunamadı", "Aradığınız sayfa kaldırılmış, taşınmış veya hiç oluşturulmamış olabilir."),
                403 => ("Bu işlem için yetkiniz yok", "Hesabınız bu sayfayı görüntülemek için gerekli yetkiye sahip değil."),
                429 => ("Çok fazla istek gönderildi", "Kısa bir süre bekledikten sonra tekrar deneyin."),
                _ => ("İstek tamamlanamadı", "Sunucu isteğinizi işlerken bir sorun oluştu.")
            };

            return View("Error", new ErrorViewModel
            {
                StatusCode = code,
                Baslik = baslik,
                Mesaj = mesaj,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
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
