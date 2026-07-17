using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Kullanicilar;
using YonetimPaneli.ViewModels.Ortak;

namespace YonetimPaneli.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KullanicilarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly OrganizasyonServisi _organizasyonServisi;
        private readonly GorevAktiviteServisi _gorevAktiviteServisi;
        private readonly BildirimServisi _bildirimServisi;
        private readonly SifreServisi _sifreServisi;

        public KullanicilarController(
            AppDbContext context,
            OrganizasyonServisi organizasyonServisi,
            GorevAktiviteServisi gorevAktiviteServisi,
            BildirimServisi bildirimServisi,
            SifreServisi sifreServisi)
        {
            _context = context;
            _organizasyonServisi = organizasyonServisi;
            _gorevAktiviteServisi = gorevAktiviteServisi;
            _bildirimServisi = bildirimServisi;
            _sifreServisi = sifreServisi;
        }

        // =====================================================
        // KULLANICI LİSTESİ
        // =====================================================

        [HttpGet]
        public IActionResult Index(
            string? arama,
            string durum = "all",
            string rol = "all",
            int? departmanId = null,
            [FromQuery(Name = "Siralama")] string sirala = "ad-asc",
            int sayfa = 1,
            int sayfaBoyutu = 20)
        {
            return View(KullaniciIndexModeliOlustur(
                arama: arama,
                durum: durum,
                rol: rol,
                departmanId: departmanId,
                sirala: sirala,
                sayfa: sayfa,
                sayfaBoyutu: sayfaBoyutu));
        }

        // =====================================================
        // KULLANICI DÜZENLEME SAYFASI
        // =====================================================

        [HttpGet]
        public IActionResult Duzenle(int id)
        {
            var kullanici = _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == id);

            if (kullanici == null)
            {
                return NotFound();
            }

            var model = new KullaniciDuzenleViewModel
            {
                Id = kullanici.Id,
                Ad = kullanici.Ad ?? string.Empty,
                Soyad = kullanici.Soyad ?? string.Empty,
                Email = kullanici.Email ?? string.Empty,
                DepartmanId = kullanici.DepartmanId,
                UnvanId = kullanici.UnvanId,
                KidemSeviyesi = kullanici.KidemSeviyesi,
                OrganizasyonRolu = kullanici.OrganizasyonRolu,
                YoneticiId = kullanici.YoneticiId,
                Role = kullanici.Role ?? "Kullanici",
                AktifMi = kullanici.AktifMi,
                KendiHesabiMi = AktifKullaniciIdGetir() == kullanici.Id,
                HesapKilitliMi = kullanici.KilitBitisTarihi.HasValue &&
                    kullanici.KilitBitisTarihi.Value > DateTime.UtcNow,
                KilitBitisTarihi = kullanici.KilitBitisTarihi,
                BasarisizGirisSayisi = kullanici.BasarisizGirisSayisi,
                Secenekler = FormSecenekleriOlustur(kullanici.Id)
            };

            return View(model);
        }

        // =====================================================
        // KULLANICI GÜNCELLEME
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(KullaniciDuzenleViewModel model)
        {
            var kullanici = _context.Kullanicilar
                .FirstOrDefault(u => u.Id == model.Id);

            if (kullanici == null)
            {
                return NotFound();
            }

            FormMetinleriniTemizle(model);
            DuzenlemeIsKurallariniDogrula(model, kullanici);

            if (!ModelState.IsValid)
            {
                DuzenlemeSayfaBilgileriniDoldur(model);
                return View(model);
            }

            var normalizeEmail = EmailNormalizeEt(model.Email);
            var yeniSifreGirildiMi =
                !string.IsNullOrWhiteSpace(model.YeniPassword);

            // E-posta, rol, aktiflik veya şifre değişirse mevcut JWT'lerin
            // geçersiz olması için güvenlik damgası yenilenir.
            var guvenlikBilgisiDegistiMi =
                !string.Equals(
                    (kullanici.Ad ?? string.Empty).Trim(),
                    model.Ad,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    (kullanici.Soyad ?? string.Empty).Trim(),
                    model.Soyad,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    EmailNormalizeEt(kullanici.Email ?? string.Empty),
                    normalizeEmail,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    kullanici.Role,
                    model.Role,
                    StringComparison.Ordinal) ||
                kullanici.AktifMi != model.AktifMi ||
                yeniSifreGirildiMi;

            kullanici.Ad = model.Ad;
            kullanici.Soyad = model.Soyad;
            kullanici.Email = normalizeEmail;

            // Yeni şifre boş bırakılırsa mevcut hash korunur.
            if (yeniSifreGirildiMi)
            {
                kullanici.Password = _sifreServisi.Hashle(
                    kullanici,
                    model.YeniPassword!);
                kullanici.SifreDegistirmeTarihi = DateTime.UtcNow;
                kullanici.BasarisizGirisSayisi = 0;
                kullanici.KilitBitisTarihi = null;
            }

            kullanici.DepartmanId = model.DepartmanId;
            kullanici.UnvanId = model.UnvanId;
            kullanici.KidemSeviyesi = model.KidemSeviyesi;
            kullanici.OrganizasyonRolu = model.OrganizasyonRolu;
            kullanici.YoneticiId = model.YoneticiId;
            kullanici.Role = model.Role;
            kullanici.AktifMi = model.AktifMi;

            if (guvenlikBilgisiDegistiMi)
            {
                _sifreServisi.GuvenlikDamgasiniYenile(kullanici);
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı güncellenirken veritabanı hatası oluştu. Bilgileri kontrol edip tekrar deneyiniz.");

                DuzenlemeSayfaBilgileriniDoldur(model);
                return View(model);
            }

            TempData["BasariMesaji"] =
                $"{kullanici.TamAd} kullanıcısının bilgileri güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // YENİ KULLANICI OLUŞTURMA
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(KullaniciEkleViewModel model)
        {
            FormMetinleriniTemizle(model);
            EklemeIsKurallariniDogrula(model);

            if (!ModelState.IsValid)
            {
                return View(
                    nameof(Index),
                    KullaniciIndexModeliOlustur(
                        model,
                        drawerAcik: true
                    )
                );
            }

            var kullanici = new AppUser
            {
                Ad = model.Ad,
                Soyad = model.Soyad,
                Email = EmailNormalizeEt(model.Email),
                Password = string.Empty,
                DepartmanId = model.DepartmanId,
                UnvanId = model.UnvanId,
                KidemSeviyesi = model.KidemSeviyesi,
                OrganizasyonRolu = model.OrganizasyonRolu,
                YoneticiId = model.YoneticiId,
                Role = model.Role,
                // Yeni kullanıcılar bilinçli olarak aktif oluşturulur.
                AktifMi = true
            };

            kullanici.Password = _sifreServisi.Hashle(
                kullanici,
                model.Password);
            kullanici.SifreDegistirmeTarihi = DateTime.UtcNow;
            _sifreServisi.GuvenlikDamgasiniYenile(kullanici);

            _context.Kullanicilar.Add(kullanici);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı kaydedilirken veritabanı hatası oluştu. Bilgileri kontrol edip tekrar deneyiniz.");

                return View(
                    nameof(Index),
                    KullaniciIndexModeliOlustur(
                        model,
                        drawerAcik: true
                    )
                );
            }

            TempData["BasariMesaji"] =
                $"{kullanici.TamAd} kullanıcısı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // KULLANICI PASİFLEŞTİRME
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PasifYap(int id)
        {
            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (!aktifKullaniciId.HasValue)
            {
                return Forbid();
            }

            if (aktifKullaniciId.Value == id)
            {
                TempData["HataMesaji"] =
                    "Kendi hesabınızı pasif yapamazsınız.";

                return RedirectToAction(nameof(Index));
            }

            var kullanici = _context.Kullanicilar
                .FirstOrDefault(u => u.Id == id);

            if (kullanici == null)
            {
                return NotFound();
            }

            if (!kullanici.AktifMi)
            {
                TempData["HataMesaji"] =
                    "Bu kullanıcı zaten pasif.";

                return RedirectToAction(nameof(Index));
            }

            var pasiflestirmeEngeli =
                PasiflestirmeEngeliGetir(kullanici);

            if (pasiflestirmeEngeli != null)
            {
                TempData["HataMesaji"] = pasiflestirmeEngeli;
                return RedirectToAction(nameof(Index));
            }

            kullanici.AktifMi = false;
            _sifreServisi.GuvenlikDamgasiniYenile(kullanici);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{kullanici.TamAd} pasif yapıldı.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // KULLANICIYI TEKRAR AKTİFLEŞTİRME
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AktifYap(int id)
        {
            var kullanici = _context.Kullanicilar
                .FirstOrDefault(u => u.Id == id);

            if (kullanici == null)
            {
                return NotFound();
            }

            if (kullanici.AktifMi)
            {
                TempData["HataMesaji"] =
                    "Bu kullanıcı zaten aktif.";

                return RedirectToAction(nameof(Index));
            }

            if (kullanici.YoneticiId.HasValue)
            {
                var yoneticiAktifMi = _context.Kullanicilar
                    .Any(u =>
                        u.Id == kullanici.YoneticiId.Value &&
                        u.AktifMi);

                if (!yoneticiAktifMi)
                {
                    TempData["HataMesaji"] =
                        "Bu kullanıcının yöneticisi pasif. Önce yöneticisini aktif yapın veya düzenleme ekranından değiştirin.";

                    return RedirectToAction(nameof(Index));
                }
            }

            kullanici.AktifMi = true;
            kullanici.BasarisizGirisSayisi = 0;
            kullanici.KilitBitisTarihi = null;
            _sifreServisi.GuvenlikDamgasiniYenile(kullanici);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{kullanici.TamAd} tekrar aktif yapıldı.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // HESAP KİLİDİNİ AÇMA
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KilidiAc(int id, string? returnUrl = null)
        {
            var kullanici = _context.Kullanicilar
                .FirstOrDefault(u => u.Id == id);

            if (kullanici == null)
            {
                return NotFound();
            }

            kullanici.BasarisizGirisSayisi = 0;
            kullanici.KilitBitisTarihi = null;
            _sifreServisi.GuvenlikDamgasiniYenile(kullanici);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{kullanici.TamAd} kullanıcısının giriş kilidi kaldırıldı.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Duzenle), new { id });
        }

        // =====================================================
        // GÖREV DEVRETME SAYFASI
        // =====================================================

        [HttpGet]
        public IActionResult GorevleriDevret(int id)
        {
            var kaynakKullanici = _context.Kullanicilar
                .Include(u => u.Departman)
                .Include(u => u.Unvan)
                .FirstOrDefault(u => u.Id == id);

            if (kaynakKullanici == null)
            {
                return NotFound();
            }

            var kaynakKullaniciIdString =
                kaynakKullanici.Id.ToString();

            var devredilecekGorevler = _context.Gorevler
                .Include(g => g.AtananKullanici)
                .Include(g => g.OlusturanKullanici)
                .Where(g =>
                    (
                        g.AtananKullaniciId == kaynakKullanici.Id ||
                        g.AtananUserId == kaynakKullaniciIdString
                    ) &&
                    g.Durum != "Tamamlandı")
                .OrderByDescending(g => g.OlusturulmaTarihi)
                .ToList();

            var hedefKullanicilar = _context.Kullanicilar
                .Where(u =>
                    u.AktifMi &&
                    u.Id != kaynakKullanici.Id)
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .ToList();

            ViewBag.KaynakKullanici = kaynakKullanici;
            ViewBag.HedefKullanicilar = hedefKullanicilar;

            return View(devredilecekGorevler);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GorevleriDevret(
            int kaynakKullaniciId,
            int hedefKullaniciId)
        {
            if (kaynakKullaniciId == hedefKullaniciId)
            {
                TempData["HataMesaji"] =
                    "Görevler aynı kullanıcıya devredilemez.";

                return RedirectToAction(
                    nameof(GorevleriDevret),
                    new { id = kaynakKullaniciId }
                );
            }

            var kaynakKullanici = _context.Kullanicilar
                .FirstOrDefault(u => u.Id == kaynakKullaniciId);

            if (kaynakKullanici == null)
            {
                return NotFound();
            }

            var hedefKullanici = _context.Kullanicilar
                .FirstOrDefault(u =>
                    u.Id == hedefKullaniciId &&
                    u.AktifMi);

            if (hedefKullanici == null)
            {
                TempData["HataMesaji"] =
                    "Görevlerin devredileceği aktif kullanıcı bulunamadı.";

                return RedirectToAction(
                    nameof(GorevleriDevret),
                    new { id = kaynakKullaniciId }
                );
            }

            var kaynakKullaniciIdString =
                kaynakKullanici.Id.ToString();

            var devredilecekGorevler = _context.Gorevler
                .Where(g =>
                    (
                        g.AtananKullaniciId == kaynakKullanici.Id ||
                        g.AtananUserId == kaynakKullaniciIdString
                    ) &&
                    g.Durum != "Tamamlandı")
                .ToList();

            if (!devredilecekGorevler.Any())
            {
                TempData["HataMesaji"] =
                    "Bu kullanıcının devredilecek tamamlanmamış görevi yok.";

                return RedirectToAction(nameof(Index));
            }

            var islemiYapanKullaniciId = AktifKullaniciIdGetir();

            if (!islemiYapanKullaniciId.HasValue)
            {
                return Forbid();
            }

            foreach (var gorev in devredilecekGorevler)
            {
                _gorevAktiviteServisi.Ekle(
                    gorev.Id,
                    islemiYapanKullaniciId,
                    GorevAktiviteTurleri.GorevDevredildi,
                    $"Görev {kaynakKullanici.TamAd} kullanıcısından {hedefKullanici.TamAd} kullanıcısına toplu işlemle devredildi.",
                    kaynakKullanici.TamAd,
                    hedefKullanici.TamAd
                );

                gorev.AtananKullaniciId = hedefKullanici.Id;
                gorev.AtananUserId = hedefKullanici.Id.ToString();
                gorev.AtananKullaniciAdi = hedefKullanici.TamAd;

                _bildirimServisi.GorevKatilimcilarinaOlustur(
                    gorev,
                    islemiYapanKullaniciId.Value,
                    BildirimTurleri.GorevDevredildi,
                    "Görev devredildi",
                    $"'{gorev.Baslik}' görevi {kaynakKullanici.TamAd} kullanıcısından {hedefKullanici.TamAd} kullanıcısına devredildi."
                );
            }

            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{kaynakKullanici.TamAd} üzerindeki " +
                $"{devredilecekGorevler.Count} görev " +
                $"{hedefKullanici.TamAd} kullanıcısına devredildi.";

            return RedirectToAction(nameof(Index));
        }

        // Eski formlarla uyumluluk için korunur; fiziksel silme yapmaz.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            return PasifYap(id);
        }

        // =====================================================
        // SAYFA MODELİ VE SEÇENEKLER
        // =====================================================

        private KullaniciIndexViewModel KullaniciIndexModeliOlustur(
            KullaniciEkleViewModel? yeniKullanici = null,
            bool drawerAcik = false,
            string? arama = null,
            string durum = "all",
            string rol = "all",
            int? departmanId = null,
            string sirala = "ad-asc",
            int sayfa = 1,
            int sayfaBoyutu = 20)
        {
            var tumKullanicilarSorgusu = _context.Kullanicilar
                .AsNoTracking();

            var simdi = DateTime.UtcNow;
            var sayilar = tumKullanicilarSorgusu
                .GroupBy(_ => 1)
                .Select(grup => new
                {
                    Aktif = grup.Count(u => u.AktifMi),
                    Pasif = grup.Count(u => !u.AktifMi),
                    Admin = grup.Count(u => u.Role == "Admin"),
                    Kilitli = grup.Count(u =>
                        u.KilitBitisTarihi.HasValue &&
                        u.KilitBitisTarihi.Value > simdi)
                })
                .FirstOrDefault();

            var filtreliSorgu = tumKullanicilarSorgusu;
            arama = arama?.Trim();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                filtreliSorgu = filtreliSorgu.Where(u =>
                    (u.Ad != null && u.Ad.Contains(arama)) ||
                    (u.Soyad != null && u.Soyad.Contains(arama)) ||
                    (u.Email != null && u.Email.Contains(arama)) ||
                    (u.Departman != null &&
                     u.Departman.Ad != null &&
                     u.Departman.Ad.Contains(arama)) ||
                    (u.Unvan != null &&
                     u.Unvan.Ad != null &&
                     u.Unvan.Ad.Contains(arama)) ||
                    (u.Yonetici != null &&
                     (((u.Yonetici.Ad ?? string.Empty) + " " +
                       (u.Yonetici.Soyad ?? string.Empty)).Contains(arama))));
            }

            filtreliSorgu = durum switch
            {
                "aktif" => filtreliSorgu.Where(u => u.AktifMi),
                "pasif" => filtreliSorgu.Where(u => !u.AktifMi),
                "kilitli" => filtreliSorgu.Where(u =>
                    u.KilitBitisTarihi.HasValue &&
                    u.KilitBitisTarihi.Value > simdi),
                _ => filtreliSorgu
            };

            filtreliSorgu = rol switch
            {
                "admin" => filtreliSorgu.Where(u => u.Role == "Admin"),
                "kullanici" => filtreliSorgu.Where(u => u.Role != "Admin"),
                _ => filtreliSorgu
            };

            if (departmanId.HasValue)
            {
                filtreliSorgu = filtreliSorgu.Where(u =>
                    u.DepartmanId == departmanId.Value);
            }

            sayfaBoyutu = sayfaBoyutu is 10 or 20 or 50
                ? sayfaBoyutu
                : 20;

            var filtrelenmisToplam = filtreliSorgu.Count();
            var toplamSayfa = Math.Max(
                1,
                (int)Math.Ceiling(filtrelenmisToplam / (double)sayfaBoyutu));
            sayfa = Math.Clamp(sayfa, 1, toplamSayfa);

            var siraliSorgu = sirala switch
            {
                "ad-desc" => filtreliSorgu
                    .OrderByDescending(u => u.Ad)
                    .ThenByDescending(u => u.Soyad),
                "aktif-once" => filtreliSorgu
                    .OrderByDescending(u => u.AktifMi)
                    .ThenBy(u => u.Ad)
                    .ThenBy(u => u.Soyad),
                "rol" => filtreliSorgu
                    .OrderByDescending(u => u.Role == "Admin")
                    .ThenBy(u => u.Ad)
                    .ThenBy(u => u.Soyad),
                _ => filtreliSorgu
                    .OrderBy(u => u.Ad)
                    .ThenBy(u => u.Soyad)
            };

            var kullanicilar = siraliSorgu
                .Include(u => u.Departman)
                .Include(u => u.Unvan)
                .Include(u => u.Yonetici)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .ToList();

            yeniKullanici ??= new KullaniciEkleViewModel();
            yeniKullanici.Secenekler = FormSecenekleriOlustur();

            return new KullaniciIndexViewModel
            {
                Liste = new KullaniciListeViewModel
                {
                    Kullanicilar = kullanicilar,
                    AktifKullaniciId = AktifKullaniciIdGetir() ?? 0
                },
                YeniKullanici = yeniKullanici,
                YeniKullaniciDrawerAcik = drawerAcik,
                Arama = arama ?? string.Empty,
                Durum = durum,
                Rol = rol,
                DepartmanId = departmanId,
                Siralama = sirala,
                SayfaBoyutu = sayfaBoyutu,
                FiltrelenmisToplamSayisi = filtrelenmisToplam,
                AktifSayisi = sayilar?.Aktif ?? 0,
                PasifSayisi = sayilar?.Pasif ?? 0,
                AdminSayisi = sayilar?.Admin ?? 0,
                KilitliSayisi = sayilar?.Kilitli ?? 0,
                Sayfalama = new SayfalamaViewModel
                {
                    Sayfa = sayfa,
                    SayfaBoyutu = sayfaBoyutu,
                    ToplamKayit = filtrelenmisToplam,
                    Controller = "Kullanicilar",
                    Action = nameof(Index)
                }
            };
        }

        private KullaniciFormSecenekleriViewModel FormSecenekleriOlustur(
            int? haricKullaniciId = null)
        {
            return new KullaniciFormSecenekleriViewModel
            {
                Departmanlar = _context.Departmanlar
                    .AsNoTracking()
                    .OrderBy(d => d.Ad)
                    .ToList(),

                Unvanlar = _context.Unvanlar
                    .AsNoTracking()
                    .OrderBy(u => u.Ad)
                    .ToList(),

                Yoneticiler = _context.Kullanicilar
                    .AsNoTracking()
                    .Where(u =>
                        u.AktifMi &&
                        (!haricKullaniciId.HasValue ||
                         u.Id != haricKullaniciId.Value))
                    .OrderBy(u => u.Ad)
                    .ThenBy(u => u.Soyad)
                    .ToList(),

                KidemSeviyeleri = Enum.GetValues<KidemSeviyesi>(),
                OrganizasyonRolleri = Enum.GetValues<OrganizasyonRolu>()
            };
        }

        private void DuzenlemeSayfaBilgileriniDoldur(
            KullaniciDuzenleViewModel model)
        {
            model.Secenekler = FormSecenekleriOlustur(model.Id);
            model.KendiHesabiMi = AktifKullaniciIdGetir() == model.Id;

            var guvenlikBilgisi = _context.Kullanicilar
                .AsNoTracking()
                .Where(u => u.Id == model.Id)
                .Select(u => new
                {
                    u.KilitBitisTarihi,
                    u.BasarisizGirisSayisi
                })
                .FirstOrDefault();

            model.KilitBitisTarihi = guvenlikBilgisi?.KilitBitisTarihi;
            model.BasarisizGirisSayisi =
                guvenlikBilgisi?.BasarisizGirisSayisi ?? 0;
            model.HesapKilitliMi =
                model.KilitBitisTarihi.HasValue &&
                model.KilitBitisTarihi.Value > DateTime.UtcNow;
        }

        // =====================================================
        // SUNUCU TARAFI İŞ KURALLARI
        // =====================================================

        private void EklemeIsKurallariniDogrula(
            KullaniciEkleViewModel model)
        {
            BoslukAlanlariniDogrula(
                model.Ad,
                model.Soyad,
                model.Email,
                model.Password,
                nameof(model.Password)
            );

            if (EmailKullaniliyorMu(model.Email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor."
                );
            }

            SecimAlanlariniDogrula(
                model.DepartmanId,
                model.UnvanId,
                model.YoneticiId,
                nameof(model.DepartmanId),
                nameof(model.UnvanId),
                nameof(model.YoneticiId)
            );

            EnumVeRolAlanlariniDogrula(
                model.KidemSeviyesi,
                model.OrganizasyonRolu,
                model.Role,
                nameof(model.KidemSeviyesi),
                nameof(model.OrganizasyonRolu),
                nameof(model.Role)
            );
        }

        private void DuzenlemeIsKurallariniDogrula(
            KullaniciDuzenleViewModel model,
            AppUser mevcutKullanici)
        {
            BoslukAlanlariniDogrula(
                model.Ad,
                model.Soyad,
                model.Email,
                model.YeniPassword,
                nameof(model.YeniPassword)
            );

            if (EmailKullaniliyorMu(model.Email, model.Id))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor."
                );
            }

            SecimAlanlariniDogrula(
                model.DepartmanId,
                model.UnvanId,
                model.YoneticiId,
                nameof(model.DepartmanId),
                nameof(model.UnvanId),
                nameof(model.YoneticiId)
            );

            EnumVeRolAlanlariniDogrula(
                model.KidemSeviyesi,
                model.OrganizasyonRolu,
                model.Role,
                nameof(model.KidemSeviyesi),
                nameof(model.OrganizasyonRolu),
                nameof(model.Role)
            );

            if (!_organizasyonServisi.YoneticiAtanabilirMi(
                    model.Id,
                    model.YoneticiId))
            {
                ModelState.AddModelError(
                    nameof(model.YoneticiId),
                    "Bu yönetici ataması organizasyon hiyerarşisinde döngü oluşturuyor."
                );
            }

            var aktifKullaniciId = AktifKullaniciIdGetir();

            if (aktifKullaniciId == model.Id && !model.AktifMi)
            {
                ModelState.AddModelError(
                    nameof(model.AktifMi),
                    "Kendi hesabınızı pasif yapamazsınız."
                );
            }

            var adminliktenCikiyorMu =
                mevcutKullanici.Role == "Admin" &&
                model.Role != "Admin";

            var adminPasifOluyorMu =
                mevcutKullanici.Role == "Admin" &&
                mevcutKullanici.AktifMi &&
                !model.AktifMi;

            if (adminliktenCikiyorMu || adminPasifOluyorMu)
            {
                var aktifAdminSayisi = _context.Kullanicilar
                    .Count(u =>
                        u.AktifMi &&
                        u.Role == "Admin");

                if (aktifAdminSayisi <= 1)
                {
                    ModelState.AddModelError(
                        nameof(model.Role),
                        "Sistemde en az bir aktif admin kalmalıdır."
                    );
                }
            }

            // Düzenleme ekranı kullanılarak pasifleştirme yapılırsa da
            // PasifYap metodundaki güvenlik kuralları uygulanır.
            if (mevcutKullanici.AktifMi && !model.AktifMi)
            {
                var engel = PasiflestirmeEngeliGetir(mevcutKullanici);

                if (engel != null)
                {
                    ModelState.AddModelError(
                        nameof(model.AktifMi),
                        engel
                    );
                }
            }
        }

        private void SecimAlanlariniDogrula(
            int? departmanId,
            int? unvanId,
            int? yoneticiId,
            string departmanAlanAdi,
            string unvanAlanAdi,
            string yoneticiAlanAdi)
        {
            if (departmanId.HasValue &&
                !_context.Departmanlar.Any(d => d.Id == departmanId.Value))
            {
                ModelState.AddModelError(
                    departmanAlanAdi,
                    "Seçilen departman bulunamadı."
                );
            }

            if (unvanId.HasValue &&
                !_context.Unvanlar.Any(u => u.Id == unvanId.Value))
            {
                ModelState.AddModelError(
                    unvanAlanAdi,
                    "Seçilen unvan bulunamadı."
                );
            }

            if (yoneticiId.HasValue &&
                !_context.Kullanicilar.Any(u =>
                    u.Id == yoneticiId.Value &&
                    u.AktifMi))
            {
                ModelState.AddModelError(
                    yoneticiAlanAdi,
                    "Seçilen yönetici bulunamadı veya aktif değil."
                );
            }
        }

        private void EnumVeRolAlanlariniDogrula(
            KidemSeviyesi kidemSeviyesi,
            OrganizasyonRolu organizasyonRolu,
            string role,
            string kidemAlanAdi,
            string organizasyonRoluAlanAdi,
            string roleAlanAdi)
        {
            if (!Enum.IsDefined(typeof(KidemSeviyesi), kidemSeviyesi))
            {
                ModelState.AddModelError(
                    kidemAlanAdi,
                    "Geçerli bir kıdem seviyesi seçiniz."
                );
            }

            if (!Enum.IsDefined(typeof(OrganizasyonRolu), organizasyonRolu))
            {
                ModelState.AddModelError(
                    organizasyonRoluAlanAdi,
                    "Geçerli bir organizasyon rolü seçiniz."
                );
            }

            if (role != "Admin" && role != "Kullanici")
            {
                ModelState.AddModelError(
                    roleAlanAdi,
                    "Geçerli bir sistem rolü seçiniz."
                );
            }
        }

        private void BoslukAlanlariniDogrula(
            string ad,
            string soyad,
            string email,
            string? password,
            string passwordAlanAdi)
        {
            if (string.IsNullOrWhiteSpace(ad))
            {
                ModelState.AddModelError("Ad", "Ad alanı boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(soyad))
            {
                ModelState.AddModelError("Soyad", "Soyad alanı boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("Email", "E-posta alanı boş bırakılamaz.");
            }

            if (password != null &&
                password.Length > 0 &&
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    passwordAlanAdi,
                    "Şifre yalnızca boşluklardan oluşamaz."
                );
            }
        }

        private string? PasiflestirmeEngeliGetir(AppUser kullanici)
        {
            if (kullanici.Role == "Admin")
            {
                var aktifAdminSayisi = _context.Kullanicilar
                    .Count(u =>
                        u.AktifMi &&
                        u.Role == "Admin");

                if (aktifAdminSayisi <= 1)
                {
                    return "Sistemde en az bir aktif admin kalmalıdır.";
                }
            }

            var aktifAstiVarMi = _context.Kullanicilar
                .Any(u =>
                    u.AktifMi &&
                    u.YoneticiId == kullanici.Id);

            if (aktifAstiVarMi)
            {
                return "Bu kullanıcının aktif astları var. Önce astların yöneticisini değiştirin.";
            }

            var kullaniciIdMetni = kullanici.Id.ToString();

            var tamamlanmamisGoreviVarMi = _context.Gorevler
                .Any(g =>
                    (
                        g.AtananKullaniciId == kullanici.Id ||
                        g.AtananUserId == kullaniciIdMetni
                    ) &&
                    g.Durum != "Tamamlandı");

            if (tamamlanmamisGoreviVarMi)
            {
                return "Bu kullanıcıya atanmış tamamlanmamış görevler var. Önce görevleri tamamlayın veya başka kullanıcıya atayın.";
            }

            return null;
        }

        private bool EmailKullaniliyorMu(
            string email,
            int? haricKullaniciId = null)
        {
            var normalizeEmail = EmailNormalizeEt(email);

            if (string.IsNullOrWhiteSpace(normalizeEmail))
            {
                return false;
            }

            return _context.Kullanicilar.Any(u =>
                (!haricKullaniciId.HasValue ||
                 u.Id != haricKullaniciId.Value) &&
                u.Email != null &&
                u.Email.Trim().ToLower() == normalizeEmail);
        }

        private static string EmailNormalizeEt(string email)
        {
            return (email ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }

        private static void FormMetinleriniTemizle(
            KullaniciEkleViewModel model)
        {
            model.Ad = (model.Ad ?? string.Empty).Trim();
            model.Soyad = (model.Soyad ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim();
            model.Role = (model.Role ?? string.Empty).Trim();
        }

        private static void FormMetinleriniTemizle(
            KullaniciDuzenleViewModel model)
        {
            model.Ad = (model.Ad ?? string.Empty).Trim();
            model.Soyad = (model.Soyad ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim();
            model.Role = (model.Role ?? string.Empty).Trim();
        }

        private int? AktifKullaniciIdGetir()
        {
            var claimDegeri =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(claimDegeri, out var kullaniciId))
            {
                return kullaniciId;
            }

            return null;
        }
    }
}
