using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Gorevler;
using YonetimPaneli.ViewModels.Ortak;

namespace YonetimPaneli.Controllers
{
    [Authorize]
    public class GorevController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GorevYetkiServisi _gorevYetkiServisi;
        private readonly GorevDurumServisi _gorevDurumServisi;
        private readonly GorevResimServisi _gorevResimServisi;
        private readonly GorevAktiviteServisi _gorevAktiviteServisi;
        private readonly GorevZamanServisi _gorevZamanServisi;
        private readonly BildirimServisi _bildirimServisi;

        public GorevController(
            AppDbContext context,
            GorevYetkiServisi gorevYetkiServisi,
            GorevDurumServisi gorevDurumServisi,
            GorevResimServisi gorevResimServisi,
            GorevAktiviteServisi gorevAktiviteServisi,
            GorevZamanServisi gorevZamanServisi,
            BildirimServisi bildirimServisi)
        {
            _context = context;
            _gorevYetkiServisi = gorevYetkiServisi;
            _gorevDurumServisi = gorevDurumServisi;
            _gorevResimServisi = gorevResimServisi;
            _gorevAktiviteServisi = gorevAktiviteServisi;
            _gorevZamanServisi = gorevZamanServisi;
            _bildirimServisi = bildirimServisi;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? arama,
            string durum = "all",
            string oncelik = "all",
            string tarih = "all",
            string kapsam = "all",
            string atanan = "all",
            string sirala = "newest",
            int sayfa = 1,
            int sayfaBoyutu = 20,
            CancellationToken cancellationToken = default)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
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
                return Forbid();
            }

            var adminMi = aktifKullanici.Role == "Admin";
            var genelMudurMu =
                aktifKullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur;

            IQueryable<Gorev> gorulebilirSorgu = _context.Gorevler
                .AsNoTracking();

            if (!adminMi && !genelMudurMu)
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

            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);
            var dortGunSonra = bugun.AddDays(4);

            // Özet kartları filtre uygulanmadan önceki yetki kapsamını gösterir.
            // Tek bir aggregate sorgusu ile sekiz ayrı COUNT sorgusu yerine tüm
            // metrikler aynı veritabanı çağrısında hesaplanır.
            var ozet = await gorulebilirSorgu
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
                    Kritik = grup.Count(g =>
                        g.Durum != "Tamamlandı" &&
                        g.Oncelik == GorevOnceligi.Kritik)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var filtreliSorgu = gorulebilirSorgu;
            arama = arama?.Trim();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                filtreliSorgu = filtreliSorgu.Where(g =>
                    (g.Baslik != null && g.Baslik.Contains(arama)) ||
                    (g.Aciklama != null && g.Aciklama.Contains(arama)) ||
                    (g.AtananKullaniciAdi != null &&
                     g.AtananKullaniciAdi.Contains(arama)) ||
                    (g.AtananKullanici != null &&
                     (((g.AtananKullanici.Ad ?? string.Empty) + " " +
                       (g.AtananKullanici.Soyad ?? string.Empty)).Contains(arama) ||
                      (g.AtananKullanici.Email ?? string.Empty).Contains(arama))) ||
                    (g.OlusturanKullanici != null &&
                     (((g.OlusturanKullanici.Ad ?? string.Empty) + " " +
                       (g.OlusturanKullanici.Soyad ?? string.Empty)).Contains(arama) ||
                      (g.OlusturanKullanici.Email ?? string.Empty).Contains(arama))));
            }

            filtreliSorgu = durum switch
            {
                "aktif" => filtreliSorgu.Where(g => g.Durum != "Tamamlandı"),
                "acik" => filtreliSorgu.Where(g => g.Durum == "Açık"),
                "devam" => filtreliSorgu.Where(g => g.Durum == "Devam Ediyor"),
                "qa" => filtreliSorgu.Where(g => g.Durum == "QA / Test Bekleyen"),
                "bug" => filtreliSorgu.Where(g => g.Durum == "Bug / Hata"),
                "tamamlandi" => filtreliSorgu.Where(g => g.Durum == "Tamamlandı"),
                _ => filtreliSorgu
            };

            filtreliSorgu = oncelik switch
            {
                "dusuk" => filtreliSorgu.Where(g => g.Oncelik == GorevOnceligi.Dusuk),
                "normal" => filtreliSorgu.Where(g => g.Oncelik == GorevOnceligi.Normal),
                "yuksek" => filtreliSorgu.Where(g => g.Oncelik == GorevOnceligi.Yuksek),
                "kritik" => filtreliSorgu.Where(g => g.Oncelik == GorevOnceligi.Kritik),
                _ => filtreliSorgu
            };

            filtreliSorgu = tarih switch
            {
                "geciken" => filtreliSorgu.Where(g =>
                    g.Durum != "Tamamlandı" &&
                    g.SonTarih.HasValue &&
                    g.SonTarih.Value < bugun),
                "bugun" => filtreliSorgu.Where(g =>
                    g.Durum != "Tamamlandı" &&
                    g.SonTarih.HasValue &&
                    g.SonTarih.Value >= bugun &&
                    g.SonTarih.Value < yarin),
                "yaklasan" => filtreliSorgu.Where(g =>
                    g.Durum != "Tamamlandı" &&
                    g.SonTarih.HasValue &&
                    g.SonTarih.Value >= yarin &&
                    g.SonTarih.Value < dortGunSonra),
                "planli" => filtreliSorgu.Where(g =>
                    g.Durum != "Tamamlandı" &&
                    g.SonTarih.HasValue &&
                    g.SonTarih.Value >= dortGunSonra),
                "tarihsiz" => filtreliSorgu.Where(g =>
                    g.Durum != "Tamamlandı" &&
                    !g.SonTarih.HasValue),
                "tamamlandi" => filtreliSorgu.Where(g => g.Durum == "Tamamlandı"),
                _ => filtreliSorgu
            };

            // Önceki aşamalarda kullanılan eski query-string değerlerini de
            // destekliyoruz. Böylece kaydedilmiş bağlantılar bozulmaz.
            kapsam = kapsam switch
            {
                "atanan" => "bana-atanan",
                "olusturan" => "benim-olusturduklarim",
                _ => kapsam
            };

            var aktifKullaniciIdString2 = aktifKullanici.Id.ToString();
            filtreliSorgu = kapsam switch
            {
                "bana-atanan" => filtreliSorgu.Where(g =>
                    g.AtananKullaniciId == aktifKullanici.Id ||
                    g.AtananUserId == aktifKullaniciIdString2),
                "benim-olusturduklarim" => filtreliSorgu.Where(g =>
                    g.OlusturanKullaniciId == aktifKullanici.Id),
                "ekip" => filtreliSorgu.Where(g =>
                    g.AtananKullaniciId != aktifKullanici.Id &&
                    g.AtananUserId != aktifKullaniciIdString2 &&
                    g.OlusturanKullaniciId != aktifKullanici.Id),
                _ => filtreliSorgu
            };

            if (int.TryParse(atanan, out var atananKullaniciId))
            {
                var atananKullaniciIdString = atananKullaniciId.ToString();
                filtreliSorgu = filtreliSorgu.Where(g =>
                    g.AtananKullaniciId == atananKullaniciId ||
                    g.AtananUserId == atananKullaniciIdString);
            }

            sayfaBoyutu = sayfaBoyutu is 10 or 20 or 50
                ? sayfaBoyutu
                : 20;

            var filtrelenmisToplam = await filtreliSorgu
                .CountAsync(cancellationToken);
            var toplamSayfa = Math.Max(
                1,
                (int)Math.Ceiling(filtrelenmisToplam / (double)sayfaBoyutu));
            sayfa = Math.Clamp(sayfa, 1, toplamSayfa);

            var siraliSorgu = sirala switch
            {
                "oldest" => filtreliSorgu
                    .OrderBy(g => g.OlusturulmaTarihi),
                "title-asc" => filtreliSorgu
                    .OrderBy(g => g.Baslik),
                "status" => filtreliSorgu
                    .OrderBy(g => g.Durum == "Açık" ? 1 :
                                  g.Durum == "Devam Ediyor" ? 2 :
                                  g.Durum == "QA / Test Bekleyen" ? 3 :
                                  g.Durum == "Bug / Hata" ? 4 :
                                  g.Durum == "Tamamlandı" ? 5 : 6)
                    .ThenByDescending(g => g.OlusturulmaTarihi),
                "priority" => filtreliSorgu
                    .OrderByDescending(g => g.Oncelik)
                    .ThenBy(g => g.SonTarih ?? DateTime.MaxValue),
                "due-soon" => filtreliSorgu
                    .OrderBy(g => g.SonTarih ?? DateTime.MaxValue)
                    .ThenByDescending(g => g.Oncelik),
                _ => filtreliSorgu
                    .OrderByDescending(g => g.OlusturulmaTarihi)
            };

            var gorulebilirGorevler = await siraliSorgu
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .ToListAsync(cancellationToken);

            var sayfadakiGorevIdleri = gorulebilirGorevler
                .Select(g => g.Id)
                .ToList();

            var yorumSayilari = sayfadakiGorevIdleri.Count == 0
                ? new Dictionary<int, int>()
                : await _context.GorevYorumlari
                    .AsNoTracking()
                    .Where(y =>
                        sayfadakiGorevIdleri.Contains(y.GorevId) &&
                        !y.SilindiMi)
                    .GroupBy(y => y.GorevId)
                    .Select(grup => new
                    {
                        GorevId = grup.Key,
                        Sayisi = grup.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.GorevId,
                        x => x.Sayisi,
                        cancellationToken);

            var simdi = DateTime.Now;
            var gorevListeItemleri = gorulebilirGorevler
                .Select(gorev =>
                {
                    var eskiAtananId = EskiAtananKullaniciIdCoz(gorev.AtananUserId);
                    var atananId = gorev.AtananKullaniciId ?? eskiAtananId;
                    var kullaniciyaAtanmisMi = atananId == aktifKullanici.Id;
                    var kullaniciTarafindanOlusturulmusMu =
                        gorev.OlusturanKullaniciId == aktifKullanici.Id;

                    return new GorevListeItemViewModel
                    {
                        Id = gorev.Id,
                        Baslik = gorev.Baslik ?? "Başlıksız görev",
                        Aciklama = gorev.Aciklama ?? string.Empty,
                        Durum = gorev.Durum ?? "Açık",
                        DurumKodu = DurumKoduOlustur(gorev.Durum),
                        Oncelik = gorev.Oncelik,
                        OncelikMetni = _gorevZamanServisi.OncelikMetni(gorev.Oncelik),
                        OncelikKodu = _gorevZamanServisi.OncelikKodu(gorev.Oncelik),
                        BaslangicTarihi = gorev.BaslangicTarihi,
                        SonTarih = gorev.SonTarih,
                        TamamlanmaTarihi = gorev.TamamlanmaTarihi,
                        GeciktiMi = _gorevZamanServisi.GeciktiMi(gorev.SonTarih, gorev.Durum, simdi),
                        BugunBitiyorMu = _gorevZamanServisi.BugunBitiyorMu(gorev.SonTarih, gorev.Durum, simdi),
                        YaklasanMi = _gorevZamanServisi.YaklasanMi(gorev.SonTarih, gorev.Durum, 3, simdi),
                        TarihDurumKodu = _gorevZamanServisi.TarihDurumKodu(gorev.SonTarih, gorev.Durum, simdi),
                        KalanGunMetni = _gorevZamanServisi.KalanGunMetni(gorev.SonTarih, gorev.Durum, simdi),
                        AtananKullaniciId = atananId,
                        AtananKisi = gorev.AtananKullanici?.TamAd
                            ?? gorev.AtananKullaniciAdi
                            ?? "Atanmamış",
                        OlusturanKullaniciId = gorev.OlusturanKullaniciId,
                        GoreviVeren = gorev.OlusturanKullanici?.TamAd ?? "Eski Kayıt",
                        OlusturulmaTarihi = gorev.OlusturulmaTarihi,
                        ResimYolu = gorev.ResimYolu,
                        SilebilirMi = adminMi ||
                                      genelMudurMu ||
                                      gorev.OlusturanKullaniciId == aktifKullanici.Id,
                        KullaniciyaAtanmisMi = kullaniciyaAtanmisMi,
                        KullaniciTarafindanOlusturulmusMu =
                            kullaniciTarafindanOlusturulmusMu,
                        EkipGoreviMi = !kullaniciyaAtanmisMi &&
                                      !kullaniciTarafindanOlusturulmusMu,
                        YorumSayisi = yorumSayilari.GetValueOrDefault(gorev.Id)
                    };
                })
                .ToList();

            var atananIdleri = await gorulebilirSorgu
                .Where(g => g.AtananKullaniciId.HasValue)
                .Select(g => g.AtananKullaniciId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Id.ToString() gibi sunucu sağlayıcısına göre farklı çevrilebilen
            // işlemleri SQL tarafında yapmak yerine küçük kullanıcı sonucunu
            // belleğe aldıktan sonra ViewModel'e dönüştürüyoruz.
            var atananKullanicilar = await _context.Kullanicilar
                .AsNoTracking()
                .Where(u => atananIdleri.Contains(u.Id))
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .Select(u => new
                {
                    u.Id,
                    u.Ad,
                    u.Soyad
                })
                .ToListAsync(cancellationToken);

            var atananSecenekleri = atananKullanicilar
                .Select(u => new GorevFiltreSecenegiViewModel
                {
                    Deger = u.Id.ToString(),
                    Metin = $"{u.Ad} {u.Soyad}".Trim()
                })
                .ToList();

            var model = new GorevIndexViewModel
            {
                Gorevler = gorevListeItemleri,
                AktifKullaniciId = aktifKullanici.Id,
                AdminMi = adminMi,
                GenelMudurMu = genelMudurMu,
                AtananKisiSecenekleri = atananSecenekleri,
                Arama = arama ?? string.Empty,
                Durum = durum,
                Oncelik = oncelik,
                Tarih = tarih,
                Kapsam = kapsam,
                Atanan = atanan,
                Siralama = sirala,
                SayfaBoyutu = sayfaBoyutu,
                GorulebilirToplamSayisi = ozet?.Toplam ?? 0,
                FiltrelenmisToplamSayisi = filtrelenmisToplam,
                AcikSayisi = ozet?.Acik ?? 0,
                DevamSayisi = ozet?.Devam ?? 0,
                QaSayisi = ozet?.Qa ?? 0,
                BugSayisi = ozet?.Bug ?? 0,
                TamamlananSayisi = ozet?.Tamamlanan ?? 0,
                GecikenSayisi = ozet?.Geciken ?? 0,
                BugunBitenSayisi = ozet?.BugunBiten ?? 0,
                KritikSayisi = ozet?.Kritik ?? 0,
                Sayfalama = new SayfalamaViewModel
                {
                    Sayfa = sayfa,
                    SayfaBoyutu = sayfaBoyutu,
                    ToplamKayit = filtrelenmisToplam,
                    Controller = "Gorev",
                    Action = nameof(Index)
                }
            };

            return View(model);
        }

        // =====================================================
        // GÖREV DETAYI, YORUMLAR VE AKTİVİTE GEÇMİŞİ
        // =====================================================

        [HttpGet]
        public IActionResult Detay(int id)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var gorev = _context.Gorevler
                .AsNoTracking()
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .FirstOrDefault(g => g.Id == id);

            if (gorev == null)
            {
                return NotFound();
            }

            if (!GoreviGorebilirMi(aktifKullaniciId.Value, gorev))
            {
                return Forbid();
            }

            return View(
                GorevDetayModeliniOlustur(
                    gorev,
                    aktifKullaniciId.Value
                )
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumEkle(GorevYorumEkleViewModel model)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var gorev = _context.Gorevler
                .AsNoTracking()
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .FirstOrDefault(g => g.Id == model.GorevId);

            if (gorev == null)
            {
                return NotFound();
            }

            if (!GoreviGorebilirMi(aktifKullaniciId.Value, gorev))
            {
                return Forbid();
            }

            model.Icerik = model.Icerik?.Trim() ?? string.Empty;

            // Model binding, trim işleminden önce doğrulama yaptığı için temizlenmiş
            // metni yeniden doğrularız. Böylece yalnızca boşluk girilemez.
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                return View(
                    "Detay",
                    GorevDetayModeliniOlustur(
                        gorev,
                        aktifKullaniciId.Value,
                        model
                    )
                );
            }

            var aktifKullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u =>
                    u.Id == aktifKullaniciId.Value &&
                    u.AktifMi);

            if (aktifKullanici == null)
            {
                return Forbid();
            }

            var yorum = new GorevYorum
            {
                GorevId = gorev.Id,
                KullaniciId = aktifKullanici.Id,
                Icerik = model.Icerik,
                OlusturulmaTarihi = DateTime.Now
            };

            _context.GorevYorumlari.Add(yorum);

            _gorevAktiviteServisi.Ekle(
                gorev.Id,
                aktifKullanici.Id,
                GorevAktiviteTurleri.YorumEklendi,
                $"{aktifKullanici.TamAd} göreve yorum ekledi."
            );

            _bildirimServisi.GorevKatilimcilarinaOlustur(
                gorev,
                aktifKullanici.Id,
                BildirimTurleri.YorumEklendi,
                "Göreve yeni yorum eklendi",
                $"{aktifKullanici.TamAd}, '{gorev.Baslik}' görevine yorum ekledi."
            );

            _context.SaveChanges();

            TempData["BasariMesaji"] = "Yorumunuz eklendi.";

            return Redirect(
                Url.Action(nameof(Detay), new { id = gorev.Id }) +
                "#yorumlar"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumSil(int id)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var yorum = _context.GorevYorumlari
                .Include(y => y.Gorev)
                .FirstOrDefault(y => y.Id == id);

            if (yorum == null)
            {
                return NotFound();
            }

            if (!GoreviGorebilirMi(aktifKullaniciId.Value, yorum.Gorev))
            {
                return Forbid();
            }

            var yorumSilinebilirMi =
                yorum.KullaniciId == aktifKullaniciId.Value ||
                TamDuzenlemeYetkisiVar(
                    aktifKullaniciId.Value,
                    yorum.Gorev
                );

            if (!yorumSilinebilirMi)
            {
                return Forbid();
            }

            if (!yorum.SilindiMi)
            {
                yorum.SilindiMi = true;
                yorum.SilinmeTarihi = DateTime.Now;

                _gorevAktiviteServisi.Ekle(
                    yorum.GorevId,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.YorumSilindi,
                    "Görevdeki bir yorum kaldırıldı."
                );

                _context.SaveChanges();
            }

            TempData["BasariMesaji"] = "Yorum kaldırıldı.";

            return Redirect(
                Url.Action(nameof(Detay), new { id = yorum.GorevId }) +
                "#yorumlar"
            );
        }

        // =====================================================
        // YENİ GÖREV
        // =====================================================

        [HttpGet]
        public IActionResult Ekle()
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var model = GorevEkleModeliniHazirla(
                aktifKullaniciId.Value,
                new GorevEkleViewModel()
            );

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6_291_456)]
        public async Task<IActionResult> Ekle(
            GorevEkleViewModel model,
            CancellationToken cancellationToken)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            GorevFormMetinleriniTemizle(model);

            // Model binding doğrulamayı trim işleminden önce yapar.
            // Temizlenmiş değerleri yeniden doğrulayarak yalnızca boşluk girilmesini engelleriz.
            ModelState.Clear();
            TryValidateModel(model);

            AppUser? hedefKullanici = null;

            if (model.AtananKullaniciId.HasValue)
            {
                var gorevVerebilirMi = _gorevYetkiServisi.GorevVerebilirMi(
                    aktifKullaniciId.Value,
                    model.AtananKullaniciId.Value
                );

                if (!gorevVerebilirMi)
                {
                    ModelState.AddModelError(
                        nameof(model.AtananKullaniciId),
                        "Bu kullanıcıya görev verme yetkiniz bulunmuyor."
                    );
                }
                else
                {
                    hedefKullanici = _context.Kullanicilar
                        .FirstOrDefault(u =>
                            u.Id == model.AtananKullaniciId.Value &&
                            u.AktifMi);

                    if (hedefKullanici == null)
                    {
                        ModelState.AddModelError(
                            nameof(model.AtananKullaniciId),
                            "Seçilen kullanıcı bulunamadı veya pasif durumda."
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(
                    GorevEkleModeliniHazirla(
                        aktifKullaniciId.Value,
                        model
                    )
                );
            }

            string? yeniResimYolu = null;

            if (model.ResimDosyasi != null)
            {
                var resimSonucu = await _gorevResimServisi.KaydetAsync(
                    model.ResimDosyasi,
                    cancellationToken
                );

                if (!resimSonucu.Basarili)
                {
                    ModelState.AddModelError(
                        nameof(model.ResimDosyasi),
                        resimSonucu.HataMesaji ??
                        "Görev görseli kaydedilemedi."
                    );

                    return View(
                        GorevEkleModeliniHazirla(
                            aktifKullaniciId.Value,
                            model
                        )
                    );
                }

                yeniResimYolu = resimSonucu.WebYolu;
            }

            var yeniGorev = new Gorev
            {
                Baslik = model.Baslik,
                Aciklama = model.Aciklama,
                // Yeni görevler kontrollü iş akışına her zaman Açık durumunda başlar.
                Durum = "Açık",
                Oncelik = model.Oncelik,
                BaslangicTarihi = model.BaslangicTarihi?.Date,
                SonTarih = model.SonTarih?.Date,
                TamamlanmaTarihi = null,
                AtananKullaniciId = hedefKullanici!.Id,
                OlusturanKullaniciId = aktifKullaniciId.Value,
                AtananUserId = hedefKullanici.Id.ToString(),
                AtananKullaniciAdi = hedefKullanici.TamAd,
                OlusturulmaTarihi = DateTime.Now,
                ResimYolu = yeniResimYolu
            };

            _context.Gorevler.Add(yeniGorev);

            _gorevAktiviteServisi.Ekle(
                yeniGorev,
                aktifKullaniciId.Value,
                GorevAktiviteTurleri.Olusturuldu,
                $"Görev oluşturuldu, {_gorevZamanServisi.OncelikMetni(model.Oncelik)} önceliğiyle {hedefKullanici.TamAd} kullanıcısına atandı.",
                null,
                TarihPlaniMetni(model.BaslangicTarihi, model.SonTarih)
            );

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Veritabanı kaydı başarısız olursa yeni yüklenen dosya yetim kalmasın.
                _gorevResimServisi.Sil(yeniResimYolu);

                ModelState.AddModelError(
                    string.Empty,
                    "Görev kaydedilirken veritabanı hatası oluştu. Bilgileri kontrol edip tekrar deneyiniz."
                );

                return View(
                    GorevEkleModeliniHazirla(
                        aktifKullaniciId.Value,
                        model
                    )
                );
            }

            if (hedefKullanici.Id != aktifKullaniciId.Value)
            {
                _bildirimServisi.Olustur(
                    hedefKullanici.Id,
                    BildirimTurleri.GorevAtandi,
                    "Yeni görev atandı",
                    $"'{yeniGorev.Baslik}' görevi size atandı.",
                    $"/Gorev/Detay/{yeniGorev.Id}",
                    yeniGorev.Id
                );

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Bildirim kaydı başarısız olsa bile görev oluşturma işlemi korunur.
                    _context.ChangeTracker.Clear();
                }
            }

            TempData["BasariMesaji"] =
                $"'{yeniGorev.Baslik}' görevi başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Detay), new { id = yeniGorev.Id });
        }

        // =====================================================
        // GÖREV DÜZENLEME
        // =====================================================

        [HttpGet]
        public IActionResult Duzenle(int id)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var gorev = _context.Gorevler
                .AsNoTracking()
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .FirstOrDefault(g => g.Id == id);

            if (gorev == null)
            {
                return NotFound();
            }

            var tamDuzenlemeYetkisi = TamDuzenlemeYetkisiVar(
                aktifKullaniciId.Value,
                gorev
            );

            var atananKullaniciMi = GorevAtananKullaniciMi(
                aktifKullaniciId.Value,
                gorev
            );

            if (!tamDuzenlemeYetkisi && !atananKullaniciMi)
            {
                return Forbid();
            }

            var model = GorevDuzenleModeliniHazirla(
                gorev,
                aktifKullaniciId.Value,
                tamDuzenlemeYetkisi
            );

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6_291_456)]
        public async Task<IActionResult> Duzenle(
            GorevDuzenleViewModel model,
            CancellationToken cancellationToken)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var gorev = _context.Gorevler
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .FirstOrDefault(g => g.Id == model.Id);

            if (gorev == null)
            {
                return NotFound();
            }

            var tamDuzenlemeYetkisi = TamDuzenlemeYetkisiVar(
                aktifKullaniciId.Value,
                gorev
            );

            var atananKullaniciMi = GorevAtananKullaniciMi(
                aktifKullaniciId.Value,
                gorev
            );

            if (!tamDuzenlemeYetkisi && !atananKullaniciMi)
            {
                return Forbid();
            }

            var eskiBaslik = gorev.Baslik ?? string.Empty;
            var eskiAciklama = gorev.Aciklama ?? string.Empty;
            var eskiDurum = gorev.Durum ?? "Açık";
            var eskiOncelik = gorev.Oncelik;
            var eskiBaslangicTarihi = gorev.BaslangicTarihi;
            var eskiSonTarih = gorev.SonTarih;
            var eskiAtananKullaniciId = gorev.AtananKullaniciId
                ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId);
            var eskiAtananKisi = gorev.AtananKullanici?.TamAd
                ?? gorev.AtananKullaniciAdi
                ?? "Atanmamış";
            var eskiResimYolu = gorev.ResimYolu;

            if (tamDuzenlemeYetkisi)
            {
                GorevFormMetinleriniTemizle(model);
            }
            else
            {
                // Atanan çalışan yalnızca durum değiştirebilir.
                // Formdan farklı değer gönderilse bile içerik alanları veritabanından alınır.
                model.Baslik = gorev.Baslik ?? string.Empty;
                model.Aciklama = gorev.Aciklama ?? string.Empty;
                model.AtananKullaniciId = gorev.AtananKullaniciId
                    ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId);
                model.Oncelik = gorev.Oncelik;
                model.BaslangicTarihi = gorev.BaslangicTarihi;
                model.SonTarih = gorev.SonTarih;
                model.ResimDosyasi = null;
                model.MevcutResmiKaldir = false;

                ModelState.Remove(nameof(model.Baslik));
                ModelState.Remove(nameof(model.Aciklama));
                ModelState.Remove(nameof(model.AtananKullaniciId));
                ModelState.Remove(nameof(model.Oncelik));
                ModelState.Remove(nameof(model.BaslangicTarihi));
                ModelState.Remove(nameof(model.SonTarih));
                ModelState.Remove(nameof(model.ResimDosyasi));
                ModelState.Remove(nameof(model.MevcutResmiKaldir));
            }

            // Trim ve yetki bazlı alan düzeltmelerinden sonra formu yeniden doğrula.
            ModelState.Clear();
            TryValidateModel(model);

            var durumGecisiGecerliMi = _gorevDurumServisi.DurumGecisiGecerliMi(
                gorev.Durum,
                model.Durum,
                tamDuzenlemeYetkisi
            );

            if (!durumGecisiGecerliMi)
            {
                ModelState.AddModelError(
                    nameof(model.Durum),
                    $"'{gorev.Durum}' durumundan '{model.Durum}' durumuna geçiş yapılamaz."
                );
            }

            AppUser? yeniHedefKullanici = null;

            if (tamDuzenlemeYetkisi && model.AtananKullaniciId.HasValue)
            {
                var gorevVerebilirMi = _gorevYetkiServisi.GorevVerebilirMi(
                    aktifKullaniciId.Value,
                    model.AtananKullaniciId.Value
                );

                if (!gorevVerebilirMi)
                {
                    ModelState.AddModelError(
                        nameof(model.AtananKullaniciId),
                        "Bu kullanıcıya görev verme yetkiniz bulunmuyor."
                    );
                }
                else
                {
                    yeniHedefKullanici = _context.Kullanicilar
                        .FirstOrDefault(u =>
                            u.Id == model.AtananKullaniciId.Value &&
                            u.AktifMi);

                    if (yeniHedefKullanici == null)
                    {
                        ModelState.AddModelError(
                            nameof(model.AtananKullaniciId),
                            "Seçilen kullanıcı bulunamadı veya pasif durumda."
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(
                    GorevDuzenleModeliniHazirla(
                        gorev,
                        aktifKullaniciId.Value,
                        tamDuzenlemeYetkisi,
                        model
                    )
                );
            }

            string? yeniKaydedilenResimYolu = null;

            if (tamDuzenlemeYetkisi && model.ResimDosyasi != null)
            {
                var resimSonucu = await _gorevResimServisi.KaydetAsync(
                    model.ResimDosyasi,
                    cancellationToken
                );

                if (!resimSonucu.Basarili)
                {
                    ModelState.AddModelError(
                        nameof(model.ResimDosyasi),
                        resimSonucu.HataMesaji ??
                        "Görev görseli kaydedilemedi."
                    );

                    return View(
                        GorevDuzenleModeliniHazirla(
                            gorev,
                            aktifKullaniciId.Value,
                            tamDuzenlemeYetkisi,
                            model
                        )
                    );
                }

                yeniKaydedilenResimYolu = resimSonucu.WebYolu;
            }

            if (tamDuzenlemeYetkisi)
            {
                gorev.Baslik = model.Baslik;
                gorev.Aciklama = model.Aciklama;
                gorev.AtananKullaniciId = yeniHedefKullanici!.Id;
                gorev.AtananUserId = yeniHedefKullanici.Id.ToString();
                gorev.AtananKullaniciAdi = yeniHedefKullanici.TamAd;
                gorev.Oncelik = model.Oncelik;
                gorev.BaslangicTarihi = model.BaslangicTarihi?.Date;
                gorev.SonTarih = model.SonTarih?.Date;

                if (yeniKaydedilenResimYolu != null)
                {
                    gorev.ResimYolu = yeniKaydedilenResimYolu;
                }
                else if (model.MevcutResmiKaldir)
                {
                    gorev.ResimYolu = null;
                }
            }

            gorev.Durum = model.Durum;

            if (!string.Equals(eskiDurum, gorev.Durum, StringComparison.Ordinal))
            {
                if (string.Equals(gorev.Durum, "Tamamlandı", StringComparison.Ordinal))
                {
                    gorev.TamamlanmaTarihi = DateTime.Now;
                }
                else if (string.Equals(eskiDurum, "Tamamlandı", StringComparison.Ordinal))
                {
                    gorev.TamamlanmaTarihi = null;
                }
            }

            var baslikDegisti =
                !string.Equals(eskiBaslik, gorev.Baslik, StringComparison.Ordinal);

            var aciklamaDegisti =
                !string.Equals(eskiAciklama, gorev.Aciklama, StringComparison.Ordinal);

            if (baslikDegisti || aciklamaDegisti)
            {
                var icerikAciklamasi = baslikDegisti && aciklamaDegisti
                    ? "Görev başlığı ve açıklaması güncellendi."
                    : baslikDegisti
                        ? "Görev başlığı güncellendi."
                        : "Görev açıklaması güncellendi.";

                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.IcerikGuncellendi,
                    icerikAciklamasi,
                    baslikDegisti ? eskiBaslik : null,
                    baslikDegisti ? gorev.Baslik : null
                );
            }

            if (!string.Equals(eskiDurum, gorev.Durum, StringComparison.Ordinal))
            {
                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.DurumDegisti,
                    $"Görev durumu '{eskiDurum}' değerinden '{gorev.Durum}' değerine değiştirildi.",
                    eskiDurum,
                    gorev.Durum
                );
            }

            var yeniAtananKullaniciId = gorev.AtananKullaniciId
                ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId);

            if (eskiAtananKullaniciId != yeniAtananKullaniciId)
            {
                var yeniAtananKisi = gorev.AtananKullaniciAdi ?? "Atanmamış";

                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.AtananDegisti,
                    $"Görev {eskiAtananKisi} kullanıcısından {yeniAtananKisi} kullanıcısına devredildi.",
                    eskiAtananKisi,
                    yeniAtananKisi
                );
            }

            if (eskiOncelik != gorev.Oncelik)
            {
                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.OncelikDegisti,
                    "Görev önceliği değiştirildi.",
                    _gorevZamanServisi.OncelikMetni(eskiOncelik),
                    _gorevZamanServisi.OncelikMetni(gorev.Oncelik)
                );
            }

            if (eskiBaslangicTarihi?.Date != gorev.BaslangicTarihi?.Date ||
                eskiSonTarih?.Date != gorev.SonTarih?.Date)
            {
                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.TarihPlaniDegisti,
                    "Görev başlangıç ve son tarih planı güncellendi.",
                    TarihPlaniMetni(eskiBaslangicTarihi, eskiSonTarih),
                    TarihPlaniMetni(gorev.BaslangicTarihi, gorev.SonTarih)
                );
            }

            if (!string.Equals(
                    eskiResimYolu,
                    gorev.ResimYolu,
                    StringComparison.OrdinalIgnoreCase))
            {
                var gorselAciklamasi = string.IsNullOrWhiteSpace(eskiResimYolu)
                    ? "Göreve görsel eklendi."
                    : string.IsNullOrWhiteSpace(gorev.ResimYolu)
                        ? "Görev görseli kaldırıldı."
                        : "Görev görseli değiştirildi.";

                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    aktifKullaniciId.Value,
                    GorevAktiviteTurleri.GorselGuncellendi,
                    gorselAciklamasi
                );
            }

            if (eskiAtananKullaniciId != yeniAtananKullaniciId &&
                yeniAtananKullaniciId.HasValue &&
                yeniAtananKullaniciId.Value != aktifKullaniciId.Value)
            {
                _bildirimServisi.Olustur(
                    yeniAtananKullaniciId.Value,
                    BildirimTurleri.GorevDevredildi,
                    "Görev size devredildi",
                    $"'{gorev.Baslik}' görevi {eskiAtananKisi} kullanıcısından size devredildi.",
                    $"/Gorev/Detay/{gorev.Id}",
                    gorev.Id
                );
            }

            if (!string.Equals(eskiDurum, gorev.Durum, StringComparison.Ordinal))
            {
                _bildirimServisi.GorevKatilimcilarinaOlustur(
                    gorev,
                    aktifKullaniciId.Value,
                    BildirimTurleri.DurumDegisti,
                    "Görev durumu değişti",
                    $"'{gorev.Baslik}' görevi '{gorev.Durum}' durumuna geçti."
                );
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _gorevResimServisi.Sil(yeniKaydedilenResimYolu);
                gorev.ResimYolu = eskiResimYolu;

                ModelState.AddModelError(
                    string.Empty,
                    "Görev güncellenirken veritabanı hatası oluştu. Bilgileri kontrol edip tekrar deneyiniz."
                );

                return View(
                    GorevDuzenleModeliniHazirla(
                        gorev,
                        aktifKullaniciId.Value,
                        tamDuzenlemeYetkisi,
                        model
                    )
                );
            }

            var eskiResimSilinecekMi =
                !string.IsNullOrWhiteSpace(eskiResimYolu) &&
                !string.Equals(
                    eskiResimYolu,
                    gorev.ResimYolu,
                    StringComparison.OrdinalIgnoreCase
                );

            if (eskiResimSilinecekMi)
            {
                _gorevResimServisi.Sil(eskiResimYolu);
            }

            TempData["BasariMesaji"] =
                $"'{gorev.Baslik}' görevi başarıyla güncellendi.";

            return RedirectToAction(nameof(Detay), new { id = gorev.Id });
        }

        // =====================================================
        // GÖREV SİLME
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id);

            if (gorev == null)
            {
                return NotFound();
            }

            var aktifKullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == aktifKullaniciId.Value);

            if (aktifKullanici == null)
            {
                return Forbid();
            }

            var silebilirMi =
                aktifKullanici.Role == "Admin" ||
                aktifKullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur ||
                gorev.OlusturanKullaniciId == aktifKullanici.Id;

            if (!silebilirMi)
            {
                return Forbid();
            }

            var silinecekResimYolu = gorev.ResimYolu;
            var silinenGorevBasligi = gorev.Baslik ?? "Görev";

            _context.Gorevler.Remove(gorev);
            _context.SaveChanges();

            _gorevResimServisi.Sil(silinecekResimYolu);

            TempData["BasariMesaji"] =
                $"'{silinenGorevBasligi}' görevi silindi.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // FORM MODELLERİNİ HAZIRLAMA
        // =====================================================

        private GorevDetayViewModel GorevDetayModeliniOlustur(
            Gorev gorev,
            int aktifKullaniciId,
            GorevYorumEkleViewModel? yeniYorum = null)
        {
            var aktifKullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == aktifKullaniciId);

            var tamDuzenlemeYetkisi = TamDuzenlemeYetkisiVar(
                aktifKullaniciId,
                gorev
            );

            var atananKullaniciMi = GorevAtananKullaniciMi(
                aktifKullaniciId,
                gorev
            );

            var yorumlar = _context.GorevYorumlari
                .AsNoTracking()
                .Include(y => y.Kullanici)
                .Where(y =>
                    y.GorevId == gorev.Id &&
                    !y.SilindiMi)
                .OrderBy(y => y.OlusturulmaTarihi)
                .ToList()
                .Select(y => new GorevYorumListeItemViewModel
                {
                    Id = y.Id,
                    KullaniciId = y.KullaniciId,
                    KullaniciAdi = y.Kullanici?.TamAd ?? "Bilinmeyen kullanıcı",
                    KullaniciBasHarfi = string.IsNullOrWhiteSpace(y.Kullanici?.Ad)
                        ? "?"
                        : y.Kullanici.Ad[..1].ToUpperInvariant(),
                    Icerik = y.Icerik,
                    OlusturulmaTarihi = y.OlusturulmaTarihi,
                    DuzenlenmeTarihi = y.DuzenlenmeTarihi,
                    SilinebilirMi =
                        y.KullaniciId == aktifKullaniciId ||
                        tamDuzenlemeYetkisi
                })
                .ToList();

            var aktiviteler = _context.GorevAktiviteleri
                .AsNoTracking()
                .Include(a => a.Kullanici)
                .Where(a => a.GorevId == gorev.Id)
                .OrderByDescending(a => a.OlusturulmaTarihi)
                .ThenByDescending(a => a.Id)
                .Select(a => new GorevAktiviteListeItemViewModel
                {
                    Id = a.Id,
                    IslemTuru = a.IslemTuru,
                    Aciklama = a.Aciklama,
                    KullaniciAdi = a.Kullanici != null
                        ? (a.Kullanici.Ad + " " + a.Kullanici.Soyad)
                        : "Sistem",
                    EskiDeger = a.EskiDeger,
                    YeniDeger = a.YeniDeger,
                    OlusturulmaTarihi = a.OlusturulmaTarihi
                })
                .ToList();

            var adminMi = aktifKullanici?.Role == "Admin";
            var genelMudurMu = aktifKullanici?.OrganizasyonRolu ==
                OrganizasyonRolu.GenelMudur;

            return new GorevDetayViewModel
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik ?? "Başlıksız görev",
                Aciklama = gorev.Aciklama ?? string.Empty,
                Durum = gorev.Durum ?? "Açık",
                Oncelik = gorev.Oncelik,
                OncelikMetni = _gorevZamanServisi.OncelikMetni(gorev.Oncelik),
                BaslangicTarihi = gorev.BaslangicTarihi,
                SonTarih = gorev.SonTarih,
                TamamlanmaTarihi = gorev.TamamlanmaTarihi,
                GeciktiMi = _gorevZamanServisi.GeciktiMi(gorev.SonTarih, gorev.Durum),
                BugunBitiyorMu = _gorevZamanServisi.BugunBitiyorMu(gorev.SonTarih, gorev.Durum),
                KalanGunMetni = _gorevZamanServisi.KalanGunMetni(gorev.SonTarih, gorev.Durum),
                AtananKullaniciId = gorev.AtananKullaniciId
                    ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId),
                AtananKisi = gorev.AtananKullanici?.TamAd
                    ?? gorev.AtananKullaniciAdi
                    ?? "Atanmamış",
                OlusturanKullaniciId = gorev.OlusturanKullaniciId,
                GoreviVeren = gorev.OlusturanKullanici?.TamAd
                    ?? "Eski Kayıt",
                OlusturulmaTarihi = gorev.OlusturulmaTarihi,
                ResimYolu = gorev.ResimYolu,
                DuzenleyebilirMi = tamDuzenlemeYetkisi || atananKullaniciMi,
                SilebilirMi = adminMi ||
                              genelMudurMu ||
                              gorev.OlusturanKullaniciId == aktifKullaniciId,
                YorumYazabilirMi = true,
                YeniYorum = yeniYorum ?? new GorevYorumEkleViewModel
                {
                    GorevId = gorev.Id
                },
                Yorumlar = yorumlar,
                Aktiviteler = aktiviteler
            };
        }

        private GorevEkleViewModel GorevEkleModeliniHazirla(
            int aktifKullaniciId,
            GorevEkleViewModel model)
        {
            model.Kullanicilar = GorevVerilebilecekKullaniciSecenekleriniGetir(
                aktifKullaniciId
            );

            return model;
        }

        private GorevDuzenleViewModel GorevDuzenleModeliniHazirla(
            Gorev gorev,
            int aktifKullaniciId,
            bool tamDuzenlemeYetkisi,
            GorevDuzenleViewModel? model = null)
        {
            var atananKullaniciId = gorev.AtananKullaniciId
                ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId);

            model ??= new GorevDuzenleViewModel
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik ?? string.Empty,
                Aciklama = gorev.Aciklama ?? string.Empty,
                AtananKullaniciId = atananKullaniciId,
                Durum = gorev.Durum ?? "Açık",
                Oncelik = gorev.Oncelik,
                BaslangicTarihi = gorev.BaslangicTarihi,
                SonTarih = gorev.SonTarih
            };

            model.TamDuzenlemeYetkisi = tamDuzenlemeYetkisi;
            model.AtananKisi = gorev.AtananKullanici?.TamAd
                ?? gorev.AtananKullaniciAdi
                ?? "Atanmamış";
            model.GoreviVeren = gorev.OlusturanKullanici?.TamAd
                ?? "Eski Kayıt";
            model.MevcutResimYolu = gorev.ResimYolu;
            model.IzinliDurumlar = _gorevDurumServisi.IzinliDurumlariGetir(
                gorev.Durum,
                tamDuzenlemeYetkisi
            );

            model.Kullanicilar = GorevVerilebilecekKullaniciSecenekleriniGetir(
                aktifKullaniciId
            );

            // Eski kayıtlarda atanmış kişi artık normal seçenekler arasında
            // bulunmasa bile mevcut seçim ekranda kaybolmasın.
            if (atananKullaniciId.HasValue &&
                model.Kullanicilar.All(k => k.Id != atananKullaniciId.Value))
            {
                var mevcutAtanan = _context.Kullanicilar
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == atananKullaniciId.Value);

                if (mevcutAtanan != null)
                {
                    model.Kullanicilar.Add(
                        new GorevKullaniciSecenegiViewModel
                        {
                            Id = mevcutAtanan.Id,
                            TamAd = $"{mevcutAtanan.TamAd} (mevcut atama)"
                        }
                    );
                }
            }

            model.Kullanicilar = model.Kullanicilar
                .OrderBy(k => k.TamAd)
                .ToList();

            if (model.AtananKullaniciId.HasValue)
            {
                var seciliKullanici = model.Kullanicilar
                    .FirstOrDefault(k => k.Id == model.AtananKullaniciId.Value);

                if (seciliKullanici != null)
                {
                    model.AtananKisi = seciliKullanici.TamAd
                        .Replace(" (mevcut atama)", string.Empty);
                }
            }

            return model;
        }

        private List<GorevKullaniciSecenegiViewModel>
            GorevVerilebilecekKullaniciSecenekleriniGetir(int kullaniciId)
        {
            return _gorevYetkiServisi
                .GorevVerilebilecekKullanicilariGetir(kullaniciId)
                .Where(u => u.AktifMi)
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .Select(u => new GorevKullaniciSecenegiViewModel
                {
                    Id = u.Id,
                    TamAd = u.TamAd
                })
                .ToList();
        }

        private static void GorevFormMetinleriniTemizle(
            GorevEkleViewModel model)
        {
            model.Baslik = model.Baslik?.Trim() ?? string.Empty;
            model.Aciklama = model.Aciklama?.Trim() ?? string.Empty;
        }

        private static void GorevFormMetinleriniTemizle(
            GorevDuzenleViewModel model)
        {
            model.Baslik = model.Baslik?.Trim() ?? string.Empty;
            model.Aciklama = model.Aciklama?.Trim() ?? string.Empty;
        }

        // =====================================================
        // YETKİ VE YARDIMCI METOTLAR
        // =====================================================

        private bool GoreviGorebilirMi(int kullaniciId, Gorev gorev)
        {
            var kullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u =>
                    u.Id == kullaniciId &&
                    u.AktifMi);

            if (kullanici == null)
            {
                return false;
            }

            if (kullanici.Role == "Admin" ||
                kullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur)
            {
                return true;
            }

            var atananKullaniciId = gorev.AtananKullaniciId
                ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId);

            if (atananKullaniciId == kullaniciId ||
                gorev.OlusturanKullaniciId == kullaniciId)
            {
                return true;
            }

            return atananKullaniciId.HasValue &&
                   _gorevYetkiServisi.GorevVerebilirMi(
                       kullaniciId,
                       atananKullaniciId.Value
                   );
        }

        private bool TamDuzenlemeYetkisiVar(int kullaniciId, Gorev gorev)
        {
            var kullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == kullaniciId);

            if (kullanici == null)
            {
                return false;
            }

            if (kullanici.Role == "Admin")
            {
                return true;
            }

            if (kullanici.OrganizasyonRolu == OrganizasyonRolu.GenelMudur)
            {
                return true;
            }

            if (gorev.OlusturanKullaniciId == kullaniciId)
            {
                return true;
            }

            var atananKullaniciId = gorev.AtananKullaniciId
                ?? EskiAtananKullaniciIdCoz(gorev.AtananUserId);

            if (atananKullaniciId == kullaniciId)
            {
                return false;
            }

            if (atananKullaniciId.HasValue)
            {
                return _gorevYetkiServisi.GorevVerebilirMi(
                    kullaniciId,
                    atananKullaniciId.Value
                );
            }

            return false;
        }

        private static bool GorevAtananKullaniciMi(
            int kullaniciId,
            Gorev gorev)
        {
            if (gorev.AtananKullaniciId == kullaniciId)
            {
                return true;
            }

            return gorev.AtananUserId == kullaniciId.ToString();
        }

        private static int? EskiAtananKullaniciIdCoz(string? atananUserId)
        {
            return int.TryParse(atananUserId, out var kullaniciId)
                ? kullaniciId
                : null;
        }

        private static string TarihPlaniMetni(
            DateTime? baslangicTarihi,
            DateTime? sonTarih)
        {
            var baslangic = baslangicTarihi.HasValue
                ? baslangicTarihi.Value.ToString("dd.MM.yyyy")
                : "Başlangıç yok";

            var bitis = sonTarih.HasValue
                ? sonTarih.Value.ToString("dd.MM.yyyy")
                : "Son tarih yok";

            return $"{baslangic} → {bitis}";
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

        private int? AktifKullaniciIdGetir()
        {
            var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(claimDegeri, out var kullaniciId))
            {
                return kullaniciId;
            }

            return null;
        }
    }
}
