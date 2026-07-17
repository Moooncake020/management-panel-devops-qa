using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Security;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Arama;

namespace YonetimPaneli.Controllers
{
    /// <summary>
    /// Üst çubuktaki global arama penceresine görev ve çalışan sonuçları sağlar.
    /// Görev sorgusu, görev liste ekranıyla aynı yetki kapsamını kullanır.
    /// </summary>
    [Authorize]
    public class AramaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GorevYetkiServisi _gorevYetkiServisi;

        public AramaController(
            AppDbContext context,
            GorevYetkiServisi gorevYetkiServisi)
        {
            _context = context;
            _gorevYetkiServisi = gorevYetkiServisi;
        }

        [HttpGet]
        [EnableRateLimiting(GuvenlikSabitleri.AramaRateLimitPolitikasi)]
        public async Task<IActionResult> Sonuclar(
            string? q,
            CancellationToken cancellationToken = default)
        {
            var sorgu = q?.Trim();

            if (string.IsNullOrWhiteSpace(sorgu) || sorgu.Length < 2)
            {
                return Json(new GlobalAramaCevapViewModel
                {
                    Sorgu = sorgu ?? string.Empty
                });
            }

            sorgu = sorgu[..Math.Min(sorgu.Length, 60)];

            var aktifKullaniciId = AktifKullaniciIdGetir();
            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var aktifKullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Where(u => u.Id == aktifKullaniciId.Value && u.AktifMi)
                .Select(u => new
                {
                    u.Id,
                    u.Role,
                    u.OrganizasyonRolu
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (aktifKullanici == null)
            {
                return Forbid();
            }

            var gorevSorgusu = GorevYetkiKapsaminiUygula(
                aktifKullanici.Id,
                aktifKullanici.Role,
                aktifKullanici.OrganizasyonRolu);

            var gorevIdAramasi = int.TryParse(
                sorgu.TrimStart('#'),
                out var arananGorevId);

            gorevSorgusu = gorevSorgusu.Where(g =>
                (gorevIdAramasi && g.Id == arananGorevId) ||
                (g.Baslik != null && g.Baslik.Contains(sorgu)) ||
                (g.Aciklama != null && g.Aciklama.Contains(sorgu)) ||
                (g.AtananKullaniciAdi != null &&
                 g.AtananKullaniciAdi.Contains(sorgu)) ||
                (g.AtananKullanici != null &&
                 (((g.AtananKullanici.Ad ?? string.Empty) + " " +
                   (g.AtananKullanici.Soyad ?? string.Empty)).Contains(sorgu))));

            var gorevKayitlari = await gorevSorgusu
                .OrderBy(g => g.Durum == "Tamamlandı")
                .ThenByDescending(g => g.OlusturulmaTarihi)
                .Select(g => new
                {
                    g.Id,
                    Baslik = g.Baslik ?? "Başlıksız görev",
                    g.Durum,
                    AtananAd = g.AtananKullanici != null
                        ? g.AtananKullanici.Ad
                        : null,
                    AtananSoyad = g.AtananKullanici != null
                        ? g.AtananKullanici.Soyad
                        : null,
                    EskiAtananAdi = g.AtananKullaniciAdi,
                    g.SonTarih
                })
                .Take(6)
                .ToListAsync(cancellationToken);

            var kullaniciKayitlari = await _context.Kullanicilar
                .AsNoTracking()
                .Where(u =>
                    u.AktifMi &&
                    (((u.Ad ?? string.Empty) + " " +
                      (u.Soyad ?? string.Empty)).Contains(sorgu) ||
                     (u.Email != null && u.Email.Contains(sorgu)) ||
                     (u.Departman != null && u.Departman.Ad.Contains(sorgu)) ||
                     (u.Unvan != null && u.Unvan.Ad.Contains(sorgu))))
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .Select(u => new
                {
                    u.Id,
                    u.Ad,
                    u.Soyad,
                    u.Email,
                    Departman = u.Departman != null ? u.Departman.Ad : null,
                    Unvan = u.Unvan != null ? u.Unvan.Ad : null,
                    u.Role
                })
                .Take(6)
                .ToListAsync(cancellationToken);

            var cevap = new GlobalAramaCevapViewModel
            {
                Sorgu = sorgu,
                Gorevler = gorevKayitlari.Select(g =>
                    new GlobalAramaSonucViewModel
                    {
                        Tur = "gorev",
                        Baslik = g.Baslik,
                        Aciklama = GorevAciklamasi(
                            TamAd(g.AtananAd, g.AtananSoyad, g.EskiAtananAdi),
                            g.SonTarih),
                        Url = Url.Action("Detay", "Gorev", new { id = g.Id })
                            ?? $"/Gorev/Detay/{g.Id}",
                        Ikon = "gorev",
                        Rozet = g.Durum
                    }).ToList(),
                Kullanicilar = kullaniciKayitlari.Select(u =>
                    new GlobalAramaSonucViewModel
                    {
                        Tur = "kullanici",
                        Baslik = TamAd(u.Ad, u.Soyad, u.Email),
                        Aciklama = KullaniciAciklamasi(
                            u.Unvan,
                            u.Departman,
                            u.Email),
                        Url = Url.Action("Index", "Profil", new { id = u.Id })
                            ?? $"/Profil/Index/{u.Id}",
                        Ikon = "kullanici",
                        Rozet = u.Role == "Admin" ? "Admin" : null
                    }).ToList()
            };

            return Json(cevap);
        }

        private IQueryable<Gorev> GorevYetkiKapsaminiUygula(
            int aktifKullaniciId,
            string? sistemRolu,
            OrganizasyonRolu organizasyonRolu)
        {
            var sorgu = _context.Gorevler.AsNoTracking();
            var tumunuGorebilirMi =
                sistemRolu == "Admin" ||
                organizasyonRolu == OrganizasyonRolu.GenelMudur;

            if (tumunuGorebilirMi)
            {
                return sorgu;
            }

            var izinliKullaniciIdleri = _gorevYetkiServisi
                .GorevVerilebilecekKullanicilariGetir(aktifKullaniciId)
                .Select(u => u.Id)
                .ToList();

            var aktifKullaniciIdMetni = aktifKullaniciId.ToString();

            return sorgu.Where(g =>
                g.AtananKullaniciId == aktifKullaniciId ||
                g.OlusturanKullaniciId == aktifKullaniciId ||
                g.AtananUserId == aktifKullaniciIdMetni ||
                (g.AtananKullaniciId.HasValue &&
                 izinliKullaniciIdleri.Contains(g.AtananKullaniciId.Value)));
        }

        private static string TamAd(
            string? ad,
            string? soyad,
            string? yedekDeger = null)
        {
            var tamAd = $"{ad} {soyad}".Trim();

            if (!string.IsNullOrWhiteSpace(tamAd))
            {
                return tamAd;
            }

            return string.IsNullOrWhiteSpace(yedekDeger)
                ? "İsimsiz kullanıcı"
                : yedekDeger.Trim();
        }

        private static string GorevAciklamasi(
            string? atananKisi,
            DateTime? sonTarih)
        {
            var kisi = string.IsNullOrWhiteSpace(atananKisi)
                ? "Atanmamış"
                : atananKisi.Trim();

            return sonTarih.HasValue
                ? $"{kisi} · Son tarih {sonTarih.Value:dd.MM.yyyy}"
                : $"{kisi} · Son tarih yok";
        }

        private static string KullaniciAciklamasi(
            string? unvan,
            string? departman,
            string? email)
        {
            var organizasyon = new[] { unvan, departman }
                .Where(deger => !string.IsNullOrWhiteSpace(deger))
                .Select(deger => deger!.Trim())
                .ToList();

            if (organizasyon.Count > 0)
            {
                return string.Join(" · ", organizasyon);
            }

            return string.IsNullOrWhiteSpace(email)
                ? "Organizasyon bilgisi bulunmuyor"
                : email;
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
