using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Bildirimler;
using YonetimPaneli.ViewModels.Ortak;

namespace YonetimPaneli.Controllers
{
    [Authorize]
    public class BildirimController : Controller
    {
        private static readonly string[] GecerliDurumFiltreleri =
            { "tum", "okunmamis", "okunmus" };

        private static readonly string[] GecerliTurFiltreleri =
            { "tum", "gorev", "yorum", "tarih" };

        private readonly AppDbContext _context;
        private readonly BildirimServisi _bildirimServisi;

        public BildirimController(
            AppDbContext context,
            BildirimServisi bildirimServisi)
        {
            _context = context;
            _bildirimServisi = bildirimServisi;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? arama,
            string filtre = "tum",
            string tur = "tum",
            int sayfa = 1,
            int sayfaBoyutu = 20,
            CancellationToken cancellationToken = default)
        {
            var kullaniciId = AktifKullaniciIdGetir();

            if (!kullaniciId.HasValue)
            {
                return Forbid();
            }

            ZamanUyarilariniTamamla(kullaniciId.Value);

            filtre = GecerliDurumFiltreleri.Contains(
                filtre,
                StringComparer.OrdinalIgnoreCase)
                ? filtre.ToLowerInvariant()
                : "tum";

            tur = GecerliTurFiltreleri.Contains(
                tur,
                StringComparer.OrdinalIgnoreCase)
                ? tur.ToLowerInvariant()
                : "tum";

            arama = arama?.Trim();
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama[..Math.Min(arama.Length, 80)];
            }

            sayfaBoyutu = sayfaBoyutu is 10 or 20 or 50
                ? sayfaBoyutu
                : 20;

            var tumSorgu = _context.Bildirimler
                .AsNoTracking()
                .Where(b => b.KullaniciId == kullaniciId.Value);

            var sayilar = await tumSorgu
                .GroupBy(_ => 1)
                .Select(grup => new
                {
                    Toplam = grup.Count(),
                    Okunmamis = grup.Count(b => !b.OkunduMu),
                    Okunmus = grup.Count(b => b.OkunduMu)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var filtreliSorgu = tumSorgu;

            filtreliSorgu = filtre switch
            {
                "okunmamis" => filtreliSorgu.Where(b => !b.OkunduMu),
                "okunmus" => filtreliSorgu.Where(b => b.OkunduMu),
                _ => filtreliSorgu
            };

            if (tur != "tum")
            {
                var turler = BildirimServisi
                    .KategoriTurleri(tur)
                    .ToArray();

                filtreliSorgu = filtreliSorgu.Where(b => turler.Contains(b.Tur));
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                filtreliSorgu = filtreliSorgu.Where(b =>
                    b.Baslik.Contains(arama) ||
                    b.Mesaj.Contains(arama));
            }

            var filtrelenmisToplam = await filtreliSorgu
                .CountAsync(cancellationToken);
            var toplamSayfa = Math.Max(
                1,
                (int)Math.Ceiling(filtrelenmisToplam / (double)sayfaBoyutu));
            sayfa = Math.Clamp(sayfa, 1, toplamSayfa);

            var bildirimler = await filtreliSorgu
                .OrderByDescending(b => b.OlusturulmaTarihi)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .ToListAsync(cancellationToken);

            var simdi = DateTime.Now;
            var model = new BildirimIndexViewModel
            {
                Bildirimler = bildirimler
                    .Select(b => _bildirimServisi.ViewModelOlustur(b, simdi))
                    .ToList(),
                ToplamSayisi = sayilar?.Toplam ?? 0,
                OkunmamisSayisi = sayilar?.Okunmamis ?? 0,
                OkunmusSayisi = sayilar?.Okunmus ?? 0,
                AktifFiltre = filtre,
                AktifTur = tur,
                Arama = arama,
                SayfaBoyutu = sayfaBoyutu,
                AktifFiltreSayisi =
                    (filtre == "tum" ? 0 : 1) +
                    (tur == "tum" ? 0 : 1) +
                    (string.IsNullOrWhiteSpace(arama) ? 0 : 1),
                Sayfalama = new SayfalamaViewModel
                {
                    Sayfa = sayfa,
                    SayfaBoyutu = sayfaBoyutu,
                    ToplamKayit = filtrelenmisToplam,
                    Controller = "Bildirim",
                    Action = nameof(Index)
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Oku(int id, string? hedef = null)
        {
            var kullaniciId = AktifKullaniciIdGetir();

            if (!kullaniciId.HasValue)
            {
                return Forbid();
            }

            var bildirim = _context.Bildirimler
                .FirstOrDefault(b =>
                    b.Id == id &&
                    b.KullaniciId == kullaniciId.Value);

            if (bildirim == null)
            {
                return NotFound();
            }

            if (!bildirim.OkunduMu)
            {
                bildirim.OkunduMu = true;
                bildirim.OkunmaTarihi = DateTime.Now;
                _context.SaveChanges();
            }

            var gidilecekAdres = !string.IsNullOrWhiteSpace(hedef)
                ? hedef
                : bildirim.Link;

            if (!string.IsNullOrWhiteSpace(gidilecekAdres) &&
                Url.IsLocalUrl(gidilecekAdres))
            {
                return LocalRedirect(gidilecekAdres);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DurumDegistir(
            int id,
            bool okunduMu,
            string? geriDonus = null)
        {
            var kullaniciId = AktifKullaniciIdGetir();

            if (!kullaniciId.HasValue)
            {
                return Forbid();
            }

            var bildirim = _context.Bildirimler.FirstOrDefault(b =>
                b.Id == id && b.KullaniciId == kullaniciId.Value);

            if (bildirim == null)
            {
                return NotFound();
            }

            bildirim.OkunduMu = okunduMu;
            bildirim.OkunmaTarihi = okunduMu ? DateTime.Now : null;
            _context.SaveChanges();

            TempData["BasariMesaji"] = okunduMu
                ? "Bildirim okundu olarak işaretlendi."
                : "Bildirim yeniden okunmamış olarak işaretlendi.";

            return GuvenliGeriDonus(geriDonus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id, string? geriDonus = null)
        {
            var kullaniciId = AktifKullaniciIdGetir();

            if (!kullaniciId.HasValue)
            {
                return Forbid();
            }

            var bildirim = _context.Bildirimler.FirstOrDefault(b =>
                b.Id == id && b.KullaniciId == kullaniciId.Value);

            if (bildirim == null)
            {
                return NotFound();
            }

            _context.Bildirimler.Remove(bildirim);
            _context.SaveChanges();

            TempData["BasariMesaji"] = "Bildirim silindi.";
            return GuvenliGeriDonus(geriDonus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OkunmuslariSil(string? geriDonus = null)
        {
            var kullaniciId = AktifKullaniciIdGetir();

            if (!kullaniciId.HasValue)
            {
                return Forbid();
            }

            var okunmusBildirimler = _context.Bildirimler
                .Where(b =>
                    b.KullaniciId == kullaniciId.Value &&
                    b.OkunduMu)
                .ToList();

            if (okunmusBildirimler.Count > 0)
            {
                _context.Bildirimler.RemoveRange(okunmusBildirimler);
                _context.SaveChanges();
            }

            TempData["BasariMesaji"] = okunmusBildirimler.Count == 0
                ? "Silinecek okunmuş bildirim bulunmuyor."
                : $"{okunmusBildirimler.Count} okunmuş bildirim silindi.";

            return GuvenliGeriDonus(geriDonus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TumunuOkunduYap(string? geriDonus = null)
        {
            var kullaniciId = AktifKullaniciIdGetir();

            if (!kullaniciId.HasValue)
            {
                return Forbid();
            }

            var okunmamisBildirimler = _context.Bildirimler
                .Where(b =>
                    b.KullaniciId == kullaniciId.Value &&
                    !b.OkunduMu)
                .ToList();

            var simdi = DateTime.Now;

            foreach (var bildirim in okunmamisBildirimler)
            {
                bildirim.OkunduMu = true;
                bildirim.OkunmaTarihi = simdi;
            }

            if (okunmamisBildirimler.Count > 0)
            {
                _context.SaveChanges();
            }

            TempData["BasariMesaji"] = okunmamisBildirimler.Count == 0
                ? "Okunmamış bildirim bulunmuyor."
                : $"{okunmamisBildirimler.Count} bildirim okundu olarak işaretlendi.";

            return GuvenliGeriDonus(geriDonus);
        }

        private IActionResult GuvenliGeriDonus(string? geriDonus)
        {
            if (!string.IsNullOrWhiteSpace(geriDonus) &&
                Url.IsLocalUrl(geriDonus))
            {
                return LocalRedirect(geriDonus);
            }

            return RedirectToAction(nameof(Index));
        }

        private void ZamanUyarilariniTamamla(int kullaniciId)
        {
            var eklenen = _bildirimServisi.ZamanUyarilariniOlustur(kullaniciId);

            if (eklenen > 0)
            {
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    // Aynı kullanıcı iki sekmede eş zamanlı açtıysa benzersiz anahtar
                    // çakışabilir. Menü yine mevcut bildirimleri göstermeye devam eder.
                    _context.ChangeTracker.Clear();
                }
            }
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
