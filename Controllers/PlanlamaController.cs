using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Gorevler;
using YonetimPaneli.ViewModels.Planlama;

namespace YonetimPaneli.Controllers
{
    [Authorize]
    public class PlanlamaController : Controller
    {
        private static readonly string[] DurumSirasi =
        {
            "Açık",
            "Devam Ediyor",
            "QA / Test Bekleyen",
            "Bug / Hata",
            "Tamamlandı"
        };

        private readonly AppDbContext _context;
        private readonly GorevYetkiServisi _gorevYetkiServisi;
        private readonly GorevDurumServisi _gorevDurumServisi;
        private readonly GorevAktiviteServisi _gorevAktiviteServisi;
        private readonly GorevZamanServisi _gorevZamanServisi;
        private readonly BildirimServisi _bildirimServisi;

        public PlanlamaController(
            AppDbContext context,
            GorevYetkiServisi gorevYetkiServisi,
            GorevDurumServisi gorevDurumServisi,
            GorevAktiviteServisi gorevAktiviteServisi,
            GorevZamanServisi gorevZamanServisi,
            BildirimServisi bildirimServisi)
        {
            _context = context;
            _gorevYetkiServisi = gorevYetkiServisi;
            _gorevDurumServisi = gorevDurumServisi;
            _gorevAktiviteServisi = gorevAktiviteServisi;
            _gorevZamanServisi = gorevZamanServisi;
            _bildirimServisi = bildirimServisi;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Kanban));
        }

        [HttpGet]
        public async Task<IActionResult> Kanban(
            string? arama,
            string kapsam = "all",
            string atanan = "all",
            bool tamamlanan = false,
            CancellationToken cancellationToken = default)
        {
            var baglam = await KullaniciBaglaminiGetir(cancellationToken);

            if (baglam == null)
            {
                return Forbid();
            }

            var gorevLimiti = 300;
            var sorgu = GorulebilirGorevSorgusu(baglam);

            if (!tamamlanan)
            {
                sorgu = sorgu.Where(g => g.Durum != "Tamamlandı");
            }

            arama = arama?.Trim();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                sorgu = sorgu.Where(g =>
                    (g.Baslik != null && g.Baslik.Contains(arama)) ||
                    (g.Aciklama != null && g.Aciklama.Contains(arama)) ||
                    (g.AtananKullanici != null &&
                     (((g.AtananKullanici.Ad ?? string.Empty) + " " +
                       (g.AtananKullanici.Soyad ?? string.Empty)).Contains(arama) ||
                      (g.AtananKullanici.Email ?? string.Empty).Contains(arama))) ||
                    (g.AtananKullaniciAdi != null && g.AtananKullaniciAdi.Contains(arama)));
            }

            kapsam = kapsam switch
            {
                "atanan" => "bana-atanan",
                "olusturan" => "benim-olusturduklarim",
                _ => kapsam
            };

            var aktifKullaniciIdMetni = baglam.Kullanici.Id.ToString();

            sorgu = kapsam switch
            {
                "bana-atanan" => sorgu.Where(g =>
                    g.AtananKullaniciId == baglam.Kullanici.Id ||
                    g.AtananUserId == aktifKullaniciIdMetni),
                "benim-olusturduklarim" => sorgu.Where(g =>
                    g.OlusturanKullaniciId == baglam.Kullanici.Id),
                "ekip" => sorgu.Where(g =>
                    g.AtananKullaniciId != baglam.Kullanici.Id &&
                    g.AtananUserId != aktifKullaniciIdMetni &&
                    g.OlusturanKullaniciId != baglam.Kullanici.Id),
                _ => sorgu
            };

            if (int.TryParse(atanan, out var atananKullaniciId))
            {
                var atananKullaniciIdMetni = atananKullaniciId.ToString();
                sorgu = sorgu.Where(g =>
                    g.AtananKullaniciId == atananKullaniciId ||
                    g.AtananUserId == atananKullaniciIdMetni);
            }
            else if (string.Equals(atanan, "atanmamis", StringComparison.Ordinal))
            {
                sorgu = sorgu.Where(g =>
                    !g.AtananKullaniciId.HasValue &&
                    string.IsNullOrWhiteSpace(g.AtananUserId));
            }

            var gorevler = await sorgu
                .Include(g => g.AtananKullanici)
                .Include(g => g.Yorumlar)
                .OrderByDescending(g => g.Oncelik)
                .ThenBy(g => g.SonTarih ?? DateTime.MaxValue)
                .ThenByDescending(g => g.OlusturulmaTarihi)
                .Take(gorevLimiti + 1)
                .ToListAsync(cancellationToken);

            var limitAsildiMi = gorevler.Count > gorevLimiti;

            if (limitAsildiMi)
            {
                gorevler = gorevler.Take(gorevLimiti).ToList();
            }

            var simdi = DateTime.Now;
            var kartlar = gorevler
                .Select(g => GorevItemiOlustur(g, baglam, simdi))
                .ToList();

            var kolonAciklamalari = new Dictionary<string, string>
            {
                ["Açık"] = "Henüz başlanmamış ve sıraya alınmış işler.",
                ["Devam Ediyor"] = "Aktif olarak üzerinde çalışılan görevler.",
                ["QA / Test Bekleyen"] = "Kontrol, onay veya test aşamasındaki işler.",
                ["Bug / Hata"] = "Düzeltme gerektiren veya geri dönen görevler.",
                ["Tamamlandı"] = "İş akışı başarıyla sonuçlandırılan görevler."
            };

            var kolonlar = DurumSirasi
                .Where(d => tamamlanan || d != "Tamamlandı")
                .Select(d => new KanbanKolonViewModel
                {
                    Durum = d,
                    Kod = DurumKoduOlustur(d),
                    Aciklama = kolonAciklamalari[d],
                    Gorevler = kartlar.Where(g => g.Durum == d).ToList()
                })
                .ToList();

            var model = new KanbanIndexViewModel
            {
                Kolonlar = kolonlar,
                AtananKisiSecenekleri = AtananSecenekleriniOlustur(baglam),
                Arama = arama ?? string.Empty,
                Kapsam = kapsam,
                Atanan = atanan,
                TamamlananlariGoster = tamamlanan,
                ToplamGorev = kartlar.Count,
                GecikenGorev = kartlar.Count(g => g.GeciktiMi),
                KritikGorev = kartlar.Count(g => g.Oncelik == GorevOnceligi.Kritik),
                GorevLimiti = gorevLimiti,
                LimitAsildiMi = limitAsildiMi
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumGuncelle(
            int id,
            string yeniDurum,
            CancellationToken cancellationToken = default)
        {
            var baglam = await KullaniciBaglaminiGetir(cancellationToken);

            if (baglam == null)
            {
                return Unauthorized(new { mesaj = "Oturum bilgisi doğrulanamadı." });
            }

            var gorev = await _context.Gorevler
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

            if (gorev == null)
            {
                return NotFound(new { mesaj = "Görev bulunamadı." });
            }

            if (!GoreviGorebilirMi(gorev, baglam))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { mesaj = "Bu görevi görüntüleme veya taşıma yetkiniz bulunmuyor." });
            }

            var tamDuzenlemeYetkisi = TamDuzenlemeYetkisiVar(gorev, baglam);
            var atananKullaniciMi = AtananKullaniciId(gorev) == baglam.Kullanici.Id;

            if (!tamDuzenlemeYetkisi && !atananKullaniciMi)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { mesaj = "Bu görevin durumunu değiştirme yetkiniz bulunmuyor." });
            }

            yeniDurum = yeniDurum?.Trim() ?? string.Empty;
            var eskiDurum = gorev.Durum ?? "Açık";

            if (string.Equals(eskiDurum, yeniDurum, StringComparison.Ordinal))
            {
                return Json(new
                {
                    basarili = true,
                    mesaj = "Görev zaten seçilen durumda.",
                    durum = eskiDurum,
                    durumKodu = DurumKoduOlustur(eskiDurum)
                });
            }

            if (!_gorevDurumServisi.DurumGecisiGecerliMi(
                    eskiDurum,
                    yeniDurum,
                    tamDuzenlemeYetkisi))
            {
                var izinliDurumlar = _gorevDurumServisi
                    .IzinliDurumlariGetir(eskiDurum, tamDuzenlemeYetkisi)
                    .Where(d => d != eskiDurum)
                    .ToList();

                return BadRequest(new
                {
                    mesaj = izinliDurumlar.Count == 0
                        ? $"'{eskiDurum}' durumundan başka bir duruma geçiş yapılamıyor."
                        : $"'{eskiDurum}' durumundan yalnızca {string.Join(", ", izinliDurumlar)} durumuna geçilebilir.",
                    izinliDurumlar
                });
            }

            gorev.Durum = yeniDurum;

            if (string.Equals(yeniDurum, "Tamamlandı", StringComparison.Ordinal))
            {
                gorev.TamamlanmaTarihi = DateTime.Now;
            }
            else if (string.Equals(eskiDurum, "Tamamlandı", StringComparison.Ordinal))
            {
                gorev.TamamlanmaTarihi = null;
            }

            _gorevAktiviteServisi.Ekle(
                gorev.Id,
                baglam.Kullanici.Id,
                GorevAktiviteTurleri.DurumDegisti,
                $"Görev durumu '{eskiDurum}' değerinden '{yeniDurum}' değerine değiştirildi.",
                eskiDurum,
                yeniDurum);

            _bildirimServisi.GorevKatilimcilarinaOlustur(
                gorev,
                baglam.Kullanici.Id,
                BildirimTurleri.DurumDegisti,
                "Görev durumu değiştirildi",
                $"'{gorev.Baslik}' görevi '{yeniDurum}' durumuna taşındı.");

            await _context.SaveChangesAsync(cancellationToken);

            return Json(new
            {
                basarili = true,
                mesaj = $"Görev '{yeniDurum}' durumuna taşındı.",
                durum = yeniDurum,
                durumKodu = DurumKoduOlustur(yeniDurum),
                tamamlanmaTarihi = gorev.TamamlanmaTarihi?.ToString("dd.MM.yyyy HH:mm"),
                izinliDurumlar = _gorevDurumServisi.IzinliDurumlariGetir(yeniDurum, tamDuzenlemeYetkisi)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Takvim(
            int? yil,
            int? ay,
            string atanan = "all",
            CancellationToken cancellationToken = default)
        {
            var baglam = await KullaniciBaglaminiGetir(cancellationToken);

            if (baglam == null)
            {
                return Forbid();
            }

            var bugun = DateTime.Today;
            var seciliYil = Math.Clamp(yil ?? bugun.Year, 2000, 2100);
            var seciliAy = Math.Clamp(ay ?? bugun.Month, 1, 12);
            var ayBaslangici = new DateTime(seciliYil, seciliAy, 1);
            var aySonrasi = ayBaslangici.AddMonths(1);
            var takvimBaslangici = ayBaslangici.AddDays(-PazartesiBazliGunIndeksi(ayBaslangici.DayOfWeek));
            var takvimBitisi = takvimBaslangici.AddDays(42);

            var sorgu = GorulebilirGorevSorgusu(baglam);

            if (int.TryParse(atanan, out var atananKullaniciId))
            {
                var idMetni = atananKullaniciId.ToString();
                sorgu = sorgu.Where(g =>
                    g.AtananKullaniciId == atananKullaniciId ||
                    g.AtananUserId == idMetni);
            }
            else if (string.Equals(atanan, "atanmamis", StringComparison.Ordinal))
            {
                sorgu = sorgu.Where(g =>
                    !g.AtananKullaniciId.HasValue &&
                    string.IsNullOrWhiteSpace(g.AtananUserId));
            }

            var gecikenSayisi = await sorgu.CountAsync(g =>
                g.Durum != "Tamamlandı" &&
                g.SonTarih.HasValue &&
                g.SonTarih.Value < bugun,
                cancellationToken);

            var gorevler = await sorgu
                .Include(g => g.AtananKullanici)
                .Where(g =>
                    (g.BaslangicTarihi.HasValue &&
                     g.BaslangicTarihi.Value >= takvimBaslangici &&
                     g.BaslangicTarihi.Value < takvimBitisi) ||
                    (g.SonTarih.HasValue &&
                     g.SonTarih.Value >= takvimBaslangici &&
                     g.SonTarih.Value < takvimBitisi) ||
                    (g.TamamlanmaTarihi.HasValue &&
                     g.TamamlanmaTarihi.Value >= takvimBaslangici &&
                     g.TamamlanmaTarihi.Value < takvimBitisi))
                .OrderBy(g => g.SonTarih ?? g.BaslangicTarihi ?? DateTime.MaxValue)
                .ToListAsync(cancellationToken);

            var gunler = new List<TakvimGunViewModel>();

            for (var tarih = takvimBaslangici; tarih < takvimBitisi; tarih = tarih.AddDays(1))
            {
                var gun = new TakvimGunViewModel
                {
                    Tarih = tarih,
                    BuAyaAitMi = tarih.Month == seciliAy,
                    BugunMu = tarih == bugun,
                    HaftaSonuMu = tarih.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                };

                foreach (var gorev in gorevler)
                {
                    var atananKisi = gorev.AtananKullanici?.TamAd
                        ?? gorev.AtananKullaniciAdi
                        ?? "Atanmamış";

                    if (gorev.BaslangicTarihi?.Date == tarih)
                    {
                        gun.Etkinlikler.Add(TakvimEtkinligiOlustur(gorev, "baslangic", "Başlangıç", atananKisi, bugun));
                    }

                    if (gorev.SonTarih?.Date == tarih)
                    {
                        gun.Etkinlikler.Add(TakvimEtkinligiOlustur(gorev, "bitis", "Son tarih", atananKisi, bugun));
                    }

                    if (gorev.TamamlanmaTarihi?.Date == tarih &&
                        gorev.TamamlanmaTarihi?.Date != gorev.SonTarih?.Date)
                    {
                        gun.Etkinlikler.Add(TakvimEtkinligiOlustur(gorev, "tamamlandi", "Tamamlandı", atananKisi, bugun));
                    }
                }

                gun.Etkinlikler = gun.Etkinlikler
                    .OrderByDescending(e => e.GeciktiMi)
                    .ThenBy(e => e.Tur == "bitis" ? 0 : e.Tur == "baslangic" ? 1 : 2)
                    .ThenBy(e => e.Baslik)
                    .ToList();

                gunler.Add(gun);
            }

            var model = new TakvimIndexViewModel
            {
                Yil = seciliYil,
                Ay = seciliAy,
                AyBasligi = ayBaslangici.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                OncekiAy = ayBaslangici.AddMonths(-1),
                SonrakiAy = ayBaslangici.AddMonths(1),
                Gunler = gunler,
                AtananKisiSecenekleri = AtananSecenekleriniOlustur(baglam),
                Atanan = atanan,
                BuAyBaslayan = gorevler.Count(g =>
                    g.BaslangicTarihi >= ayBaslangici && g.BaslangicTarihi < aySonrasi),
                BuAyBiten = gorevler.Count(g =>
                    g.SonTarih >= ayBaslangici && g.SonTarih < aySonrasi),
                BuAyTamamlanan = gorevler.Count(g =>
                    g.TamamlanmaTarihi >= ayBaslangici && g.TamamlanmaTarihi < aySonrasi),
                Geciken = gecikenSayisi
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> IsYuku(
            int? departmanId,
            CancellationToken cancellationToken = default)
        {
            var baglam = await KullaniciBaglaminiGetir(cancellationToken);

            if (baglam == null)
            {
                return Forbid();
            }

            var izinliKullaniciIdleri = baglam.GorevKapsamindakiKullaniciIdleri;
            var kullaniciSorgu = _context.Kullanicilar
                .AsNoTracking()
                .Include(u => u.Departman)
                .Include(u => u.Unvan)
                .Where(u => u.AktifMi && izinliKullaniciIdleri.Contains(u.Id));

            if (departmanId.HasValue)
            {
                kullaniciSorgu = kullaniciSorgu.Where(u => u.DepartmanId == departmanId.Value);
            }

            var kullanicilar = await kullaniciSorgu
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .ToListAsync(cancellationToken);

            var kullaniciIdleri = kullanicilar.Select(u => u.Id).ToHashSet();
            var kullaniciIdMetinleri = kullaniciIdleri.Select(id => id.ToString()).ToHashSet();
            var bugun = DateTime.Today;
            var yediGunSonra = bugun.AddDays(8);
            var otuzGunOnce = bugun.AddDays(-30);

            var gorevler = await GorulebilirGorevSorgusu(baglam)
                .Where(g =>
                    (g.AtananKullaniciId.HasValue && kullaniciIdleri.Contains(g.AtananKullaniciId.Value)) ||
                    (!g.AtananKullaniciId.HasValue &&
                     g.AtananUserId != null &&
                     kullaniciIdMetinleri.Contains(g.AtananUserId)))
                .Select(g => new
                {
                    g.AtananKullaniciId,
                    g.AtananUserId,
                    g.Durum,
                    g.Oncelik,
                    g.SonTarih,
                    g.TamamlanmaTarihi
                })
                .ToListAsync(cancellationToken);

            var satirlar = new List<IsYukuKullaniciViewModel>();

            foreach (var kullanici in kullanicilar)
            {
                var kullaniciIdMetni = kullanici.Id.ToString();
                var kisiGorevleri = gorevler.Where(g =>
                    g.AtananKullaniciId == kullanici.Id ||
                    (!g.AtananKullaniciId.HasValue && g.AtananUserId == kullaniciIdMetni)).ToList();
                var aktifGorevler = kisiGorevleri.Where(g => g.Durum != "Tamamlandı").ToList();
                var geciken = aktifGorevler.Count(g => g.SonTarih.HasValue && g.SonTarih.Value.Date < bugun);
                var kritik = aktifGorevler.Count(g => g.Oncelik == GorevOnceligi.Kritik);
                var yediGunIcinde = aktifGorevler.Count(g =>
                    g.SonTarih.HasValue &&
                    g.SonTarih.Value.Date >= bugun &&
                    g.SonTarih.Value.Date < yediGunSonra);
                var devamEden = aktifGorevler.Count(g => g.Durum == "Devam Ediyor");
                var qa = aktifGorevler.Count(g => g.Durum == "QA / Test Bekleyen");
                var bugHata = aktifGorevler.Count(g => g.Durum == "Bug / Hata");

                var yukPuani =
                    aktifGorevler.Count * 2 +
                    devamEden +
                    qa +
                    bugHata * 2 +
                    kritik * 3 +
                    geciken * 3 +
                    yediGunIcinde;

                var (yukSeviyesi, yukKodu) = YukSeviyesiniGetir(yukPuani);

                satirlar.Add(new IsYukuKullaniciViewModel
                {
                    KullaniciId = kullanici.Id,
                    TamAd = kullanici.TamAd,
                    BasHarfler = BasHarfleriGetir(kullanici.TamAd),
                    Departman = kullanici.Departman?.Ad ?? "Departman yok",
                    Unvan = kullanici.Unvan?.Ad ?? "Unvan yok",
                    AcikGorev = aktifGorevler.Count,
                    DevamEden = devamEden,
                    QaBekleyen = qa,
                    BugHata = bugHata,
                    Kritik = kritik,
                    Geciken = geciken,
                    YediGunIcinde = yediGunIcinde,
                    SonOtuzGundeTamamlanan = kisiGorevleri.Count(g =>
                        g.TamamlanmaTarihi.HasValue &&
                        g.TamamlanmaTarihi.Value >= otuzGunOnce),
                    YukPuani = yukPuani,
                    YukSeviyesi = yukSeviyesi,
                    YukKodu = yukKodu
                });
            }

            var enYuksekPuan = Math.Max(1, satirlar.Select(s => s.YukPuani).DefaultIfEmpty(1).Max());

            foreach (var satir in satirlar)
            {
                satir.YukYuzdesi = Math.Clamp((int)Math.Round(satir.YukPuani * 100d / enYuksekPuan), 4, 100);
            }

            satirlar = satirlar
                .OrderByDescending(s => s.YukPuani)
                .ThenByDescending(s => s.Geciken)
                .ThenBy(s => s.TamAd)
                .ToList();

            var departmanlar = await _context.Departmanlar
                .AsNoTracking()
                .Where(d => _context.Kullanicilar.Any(u =>
                    u.AktifMi &&
                    u.DepartmanId == d.Id &&
                    izinliKullaniciIdleri.Contains(u.Id)))
                .OrderBy(d => d.Ad)
                .Select(d => new IsYukuDepartmanSecenegiViewModel
                {
                    Id = d.Id,
                    Ad = d.Ad
                })
                .ToListAsync(cancellationToken);

            var atanmamisGorev = await GorulebilirGorevSorgusu(baglam)
                .CountAsync(g =>
                    g.Durum != "Tamamlandı" &&
                    !g.AtananKullaniciId.HasValue &&
                    string.IsNullOrWhiteSpace(g.AtananUserId),
                    cancellationToken);

            var model = new IsYukuIndexViewModel
            {
                Kullanicilar = satirlar,
                Departmanlar = departmanlar,
                DepartmanId = departmanId,
                ToplamAktifGorev = satirlar.Sum(s => s.AcikGorev),
                ToplamGeciken = satirlar.Sum(s => s.Geciken),
                AtanmamisGorev = atanmamisGorev,
                YuksekYukluKisi = satirlar.Count(s => s.YukKodu is "high" or "critical"),
                EnYuksekPuan = enYuksekPuan
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Raporlar(
            int donem = 30,
            CancellationToken cancellationToken = default)
        {
            var baglam = await KullaniciBaglaminiGetir(cancellationToken);

            if (baglam == null)
            {
                return Forbid();
            }

            var izinliDonemler = new[] { 7, 30, 90, 180 };
            donem = izinliDonemler.Contains(donem) ? donem : 30;
            var bugun = DateTime.Today;
            var donemBaslangici = bugun.AddDays(-(donem - 1));
            var yarin = bugun.AddDays(1);

            var gorevler = await GorulebilirGorevSorgusu(baglam)
                .Include(g => g.AtananKullanici)
                    .ThenInclude(u => u!.Departman)
                .ToListAsync(cancellationToken);

            var aktifGorevler = gorevler.Where(g => g.Durum != "Tamamlandı").ToList();
            var donemdeOlusturulanlar = gorevler
                .Where(g => g.OlusturulmaTarihi >= donemBaslangici && g.OlusturulmaTarihi < yarin)
                .ToList();
            var donemdeTamamlananlar = gorevler
                .Where(g => g.TamamlanmaTarihi >= donemBaslangici && g.TamamlanmaTarihi < yarin)
                .ToList();
            var gecikenler = aktifGorevler
                .Where(g => g.SonTarih.HasValue && g.SonTarih.Value.Date < bugun)
                .ToList();

            var durumDagilimi = DurumSirasi
                .Select(durum => new RaporDagilimItemViewModel
                {
                    Etiket = durum,
                    Kod = DurumKoduOlustur(durum),
                    Deger = gorevler.Count(g => g.Durum == durum)
                })
                .ToList();
            YuzdeleriHesapla(durumDagilimi);

            var oncelikDagilimi = Enum.GetValues<GorevOnceligi>()
                .Select(oncelik => new RaporDagilimItemViewModel
                {
                    Etiket = _gorevZamanServisi.OncelikMetni(oncelik),
                    Kod = _gorevZamanServisi.OncelikKodu(oncelik),
                    Deger = aktifGorevler.Count(g => g.Oncelik == oncelik)
                })
                .ToList();
            YuzdeleriHesapla(oncelikDagilimi);

            var trendBaslangici = HaftaninPazartesisi(donemBaslangici);
            var trend = new List<RaporTrendItemViewModel>();

            for (var hafta = trendBaslangici; hafta <= bugun; hafta = hafta.AddDays(7))
            {
                var haftaSonrasi = hafta.AddDays(7);
                trend.Add(new RaporTrendItemViewModel
                {
                    Baslangic = hafta,
                    Etiket = hafta.ToString("dd MMM", CultureInfo.GetCultureInfo("tr-TR")),
                    Olusturulan = gorevler.Count(g =>
                        g.OlusturulmaTarihi >= hafta &&
                        g.OlusturulmaTarihi < haftaSonrasi),
                    Tamamlanan = gorevler.Count(g =>
                        g.TamamlanmaTarihi >= hafta &&
                        g.TamamlanmaTarihi < haftaSonrasi)
                });
            }

            var departmanlar = gorevler
                .GroupBy(g => g.AtananKullanici?.Departman?.Ad ?? "Departman yok")
                .Select(grup => new RaporDepartmanItemViewModel
                {
                    Departman = grup.Key,
                    AktifGorev = grup.Count(g => g.Durum != "Tamamlandı"),
                    Geciken = grup.Count(g =>
                        g.Durum != "Tamamlandı" &&
                        g.SonTarih.HasValue &&
                        g.SonTarih.Value.Date < bugun),
                    DonemdeTamamlanan = grup.Count(g =>
                        g.TamamlanmaTarihi >= donemBaslangici &&
                        g.TamamlanmaTarihi < yarin),
                    Kritik = grup.Count(g =>
                        g.Durum != "Tamamlandı" &&
                        g.Oncelik == GorevOnceligi.Kritik)
                })
                .OrderByDescending(d => d.AktifGorev)
                .ThenByDescending(d => d.Geciken)
                .ThenBy(d => d.Departman)
                .Take(10)
                .ToList();

            var tamamlananKohort = donemdeOlusturulanlar.Count(g => g.Durum == "Tamamlandı");
            var ortalamaTamamlanma = donemdeTamamlananlar
                .Where(g => g.TamamlanmaTarihi.HasValue)
                .Select(g => Math.Max(0, (g.TamamlanmaTarihi!.Value - g.OlusturulmaTarihi).TotalDays))
                .DefaultIfEmpty(0)
                .Average();

            var riskliGorevler = aktifGorevler
                .Where(g =>
                    (g.SonTarih.HasValue && g.SonTarih.Value.Date < bugun) ||
                    g.Oncelik == GorevOnceligi.Kritik)
                .OrderByDescending(g => g.SonTarih.HasValue && g.SonTarih.Value.Date < bugun)
                .ThenByDescending(g => g.Oncelik)
                .ThenBy(g => g.SonTarih ?? DateTime.MaxValue)
                .Take(8)
                .Select(g => GorevItemiOlustur(g, baglam, bugun))
                .ToList();

            var model = new RaporIndexViewModel
            {
                DonemGun = donem,
                DonemBaslangici = donemBaslangici,
                GorulebilirToplam = gorevler.Count,
                DonemdeOlusturulan = donemdeOlusturulanlar.Count,
                DonemdeTamamlanan = donemdeTamamlananlar.Count,
                AktifGorev = aktifGorevler.Count,
                GecikenGorev = gecikenler.Count,
                TamamlanmaOrani = donemdeOlusturulanlar.Count == 0
                    ? 0
                    : Math.Clamp((int)Math.Round(tamamlananKohort * 100d / donemdeOlusturulanlar.Count), 0, 100),
                GecikmeOrani = aktifGorevler.Count == 0
                    ? 0
                    : Math.Clamp((int)Math.Round(gecikenler.Count * 100d / aktifGorevler.Count), 0, 100),
                OrtalamaTamamlanmaGunu = Math.Round(ortalamaTamamlanma, 1),
                DurumDagilimi = durumDagilimi,
                OncelikDagilimi = oncelikDagilimi,
                HaftalikTrend = trend,
                Departmanlar = departmanlar,
                RiskliGorevler = riskliGorevler,
                TrendMaksimum = Math.Max(1, trend.SelectMany(t => new[] { t.Olusturulan, t.Tamamlanan }).DefaultIfEmpty(1).Max())
            };

            return View(model);
        }

        private async Task<KullaniciBaglami?> KullaniciBaglaminiGetir(
            CancellationToken cancellationToken)
        {
            var kullaniciIdMetni = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(kullaniciIdMetni, out var kullaniciId))
            {
                return null;
            }

            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == kullaniciId && u.AktifMi, cancellationToken);

            if (kullanici == null)
            {
                return null;
            }

            var gorevKapsamindakiKullanicilar = _gorevYetkiServisi
                .GorevVerilebilecekKullanicilariGetir(kullanici.Id)
                .Where(u => u.AktifMi)
                .ToList();

            if (gorevKapsamindakiKullanicilar.All(u => u.Id != kullanici.Id))
            {
                gorevKapsamindakiKullanicilar.Add(kullanici);
            }

            return new KullaniciBaglami
            {
                Kullanici = kullanici,
                GorevKapsamindakiKullanicilar = gorevKapsamindakiKullanicilar,
                GorevKapsamindakiKullaniciIdleri = gorevKapsamindakiKullanicilar
                    .Select(u => u.Id)
                    .ToHashSet()
            };
        }

        private IQueryable<Gorev> GorulebilirGorevSorgusu(KullaniciBaglami baglam)
        {
            IQueryable<Gorev> sorgu = _context.Gorevler.AsNoTracking();

            if (baglam.Kullanici.Role == "Admin" ||
                baglam.Kullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur)
            {
                return sorgu;
            }

            var kullaniciId = baglam.Kullanici.Id;
            var kullaniciIdMetni = kullaniciId.ToString();
            var izinliIdler = baglam.GorevKapsamindakiKullaniciIdleri;

            return sorgu.Where(g =>
                g.AtananKullaniciId == kullaniciId ||
                g.OlusturanKullaniciId == kullaniciId ||
                g.AtananUserId == kullaniciIdMetni ||
                (g.AtananKullaniciId.HasValue && izinliIdler.Contains(g.AtananKullaniciId.Value)));
        }

        private PlanlamaGorevItemViewModel GorevItemiOlustur(
            Gorev gorev,
            KullaniciBaglami baglam,
            DateTime referansTarihi)
        {
            var atananId = AtananKullaniciId(gorev);
            var atananKisi = gorev.AtananKullanici?.TamAd
                ?? gorev.AtananKullaniciAdi
                ?? "Atanmamış";
            var tamDuzenlemeYetkisi = TamDuzenlemeYetkisiVar(gorev, baglam);
            var atananKullaniciMi = atananId == baglam.Kullanici.Id;
            var durumDegistirebilirMi = tamDuzenlemeYetkisi || atananKullaniciMi;

            return new PlanlamaGorevItemViewModel
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik ?? "Başlıksız görev",
                Durum = gorev.Durum ?? "Açık",
                DurumKodu = DurumKoduOlustur(gorev.Durum),
                Oncelik = gorev.Oncelik,
                OncelikMetni = _gorevZamanServisi.OncelikMetni(gorev.Oncelik),
                OncelikKodu = _gorevZamanServisi.OncelikKodu(gorev.Oncelik),
                AtananKullaniciId = atananId,
                AtananKisi = atananKisi,
                AtananBasHarfleri = BasHarfleriGetir(atananKisi),
                BaslangicTarihi = gorev.BaslangicTarihi,
                SonTarih = gorev.SonTarih,
                TamamlanmaTarihi = gorev.TamamlanmaTarihi,
                KalanGunMetni = _gorevZamanServisi.KalanGunMetni(gorev.SonTarih, gorev.Durum, referansTarihi),
                GeciktiMi = _gorevZamanServisi.GeciktiMi(gorev.SonTarih, gorev.Durum, referansTarihi),
                BugunBitiyorMu = _gorevZamanServisi.BugunBitiyorMu(gorev.SonTarih, gorev.Durum, referansTarihi),
                DurumDegistirebilirMi = durumDegistirebilirMi,
                IzinliDurumlar = durumDegistirebilirMi
                    ? _gorevDurumServisi.IzinliDurumlariGetir(gorev.Durum, tamDuzenlemeYetkisi)
                    : new List<string> { gorev.Durum },
                YorumSayisi = gorev.Yorumlar?.Count(y => !y.SilindiMi) ?? 0
            };
        }

        private static TakvimEtkinlikViewModel TakvimEtkinligiOlustur(
            Gorev gorev,
            string tur,
            string turMetni,
            string atananKisi,
            DateTime bugun)
        {
            return new TakvimEtkinlikViewModel
            {
                GorevId = gorev.Id,
                Baslik = gorev.Baslik ?? "Başlıksız görev",
                Tur = tur,
                TurMetni = turMetni,
                Durum = gorev.Durum ?? "Açık",
                DurumKodu = DurumKoduOlustur(gorev.Durum),
                OncelikKodu = gorev.Oncelik switch
                {
                    GorevOnceligi.Dusuk => "dusuk",
                    GorevOnceligi.Yuksek => "yuksek",
                    GorevOnceligi.Kritik => "kritik",
                    _ => "normal"
                },
                AtananKisi = atananKisi,
                GeciktiMi = tur == "bitis" &&
                             gorev.Durum != "Tamamlandı" &&
                             gorev.SonTarih.HasValue &&
                             gorev.SonTarih.Value.Date < bugun.Date
            };
        }

        private List<GorevFiltreSecenegiViewModel> AtananSecenekleriniOlustur(
            KullaniciBaglami baglam)
        {
            return baglam.GorevKapsamindakiKullanicilar
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .Select(u => new GorevFiltreSecenegiViewModel
                {
                    Deger = u.Id.ToString(),
                    Metin = u.TamAd
                })
                .ToList();
        }

        private bool GoreviGorebilirMi(Gorev gorev, KullaniciBaglami baglam)
        {
            if (baglam.Kullanici.Role == "Admin" ||
                baglam.Kullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur)
            {
                return true;
            }

            var atananId = AtananKullaniciId(gorev);

            return atananId == baglam.Kullanici.Id ||
                   gorev.OlusturanKullaniciId == baglam.Kullanici.Id ||
                   (atananId.HasValue && baglam.GorevKapsamindakiKullaniciIdleri.Contains(atananId.Value));
        }

        private bool TamDuzenlemeYetkisiVar(Gorev gorev, KullaniciBaglami baglam)
        {
            if (baglam.Kullanici.Role == "Admin" ||
                baglam.Kullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur ||
                gorev.OlusturanKullaniciId == baglam.Kullanici.Id)
            {
                return true;
            }

            var atananId = AtananKullaniciId(gorev);

            return atananId.HasValue &&
                   atananId.Value != baglam.Kullanici.Id &&
                   baglam.GorevKapsamindakiKullaniciIdleri.Contains(atananId.Value);
        }

        private static int? AtananKullaniciId(Gorev gorev)
        {
            if (gorev.AtananKullaniciId.HasValue)
            {
                return gorev.AtananKullaniciId;
            }

            return int.TryParse(gorev.AtananUserId, out var eskiId)
                ? eskiId
                : null;
        }

        private static int PazartesiBazliGunIndeksi(DayOfWeek gun)
        {
            return gun == DayOfWeek.Sunday ? 6 : (int)gun - 1;
        }

        private static DateTime HaftaninPazartesisi(DateTime tarih)
        {
            return tarih.Date.AddDays(-PazartesiBazliGunIndeksi(tarih.DayOfWeek));
        }

        private static string DurumKoduOlustur(string? durum)
        {
            return durum switch
            {
                "Açık" => "acik",
                "Devam Ediyor" => "devam",
                "QA / Test Bekleyen" => "qa",
                "Bug / Hata" => "bug",
                "Tamamlandı" => "tamamlandi",
                _ => "diger"
            };
        }

        private static string BasHarfleriGetir(string? tamAd)
        {
            if (string.IsNullOrWhiteSpace(tamAd) || tamAd == "Atanmamış")
            {
                return "?";
            }

            var parcalar = tamAd
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .ToList();

            return string.Concat(parcalar.Select(p => p[..1].ToUpperInvariant()));
        }

        private static (string Seviye, string Kod) YukSeviyesiniGetir(int puan)
        {
            return puan switch
            {
                <= 7 => ("Hafif", "light"),
                <= 18 => ("Dengeli", "balanced"),
                <= 30 => ("Yüksek", "high"),
                _ => ("Kritik", "critical")
            };
        }

        private static void YuzdeleriHesapla(List<RaporDagilimItemViewModel> satirlar)
        {
            var toplam = satirlar.Sum(s => s.Deger);

            foreach (var satir in satirlar)
            {
                satir.Yuzde = toplam == 0
                    ? 0
                    : Math.Clamp((int)Math.Round(satir.Deger * 100d / toplam), 0, 100);
            }
        }

        private sealed class KullaniciBaglami
        {
            public AppUser Kullanici { get; set; } = null!;
            public List<AppUser> GorevKapsamindakiKullanicilar { get; set; } = new();
            public HashSet<int> GorevKapsamindakiKullaniciIdleri { get; set; } = new();
        }
    }
}
