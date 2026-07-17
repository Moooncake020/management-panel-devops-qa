using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.ViewModels.Organizasyon;

namespace YonetimPaneli.Controllers
{
    /// <summary>
    /// Organizasyon ağacını gösterir; admin kullanıcılar için departman ve
    /// unvan yönetim işlemlerini yürütür.
    /// </summary>
    [Authorize]
    public class OrganizasyonController : Controller
    {
        private readonly AppDbContext _context;

        public OrganizasyonController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // ORGANİZASYON ŞEMASI
        // =====================================================

        [HttpGet]
        public IActionResult Index()
        {
            var kullanicilar = _context.Kullanicilar
                .AsNoTracking()
                .Include(u => u.Departman)
                .Include(u => u.Unvan)
                .Where(u => u.AktifMi)
                .OrderBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .ToList();

            var kullaniciSozlugu = kullanicilar
                .ToDictionary(u => u.Id);

            var astGruplari = kullanicilar
                .Where(u => u.YoneticiId.HasValue)
                .GroupBy(u => u.YoneticiId!.Value)
                .ToDictionary(
                    grup => grup.Key,
                    grup => grup
                        .OrderBy(u => OrganizasyonRolSirasi(u.OrganizasyonRolu))
                        .ThenBy(u => u.Ad)
                        .ThenBy(u => u.Soyad)
                        .ToList()
                );

            // Yöneticisi bulunmayan veya yöneticisi artık aktif olmayan kişiler
            // şemanın kök seviyesinde gösterilir.
            var kokKullanicilar = kullanicilar
                .Where(u =>
                    !u.YoneticiId.HasValue ||
                    !kullaniciSozlugu.ContainsKey(u.YoneticiId.Value))
                .OrderBy(u => OrganizasyonRolSirasi(u.OrganizasyonRolu))
                .ThenBy(u => u.Ad)
                .ThenBy(u => u.Soyad)
                .ToList();

            var islenenKullaniciIdleri = new HashSet<int>();
            var kokDugumler = new List<OrganizasyonDugumViewModel>();

            foreach (var kullanici in kokKullanicilar)
            {
                kokDugumler.Add(
                    DugumOlustur(
                        kullanici,
                        astGruplari,
                        islenenKullaniciIdleri,
                        new HashSet<int>()
                    )
                );
            }

            // Veride beklenmeyen bir döngü veya kopuk ilişki varsa kullanıcıyı
            // görünmez bırakmak yerine ayrı bir kök kart olarak gösteririz.
            foreach (var kullanici in kullanicilar.Where(u =>
                         !islenenKullaniciIdleri.Contains(u.Id)))
            {
                kokDugumler.Add(
                    DugumOlustur(
                        kullanici,
                        astGruplari,
                        islenenKullaniciIdleri,
                        new HashSet<int>()
                    )
                );
            }

            var model = new OrganizasyonIndexViewModel
            {
                KokDugumler = kokDugumler,
                Departmanlar = kullanicilar
                    .Where(u => u.Departman != null)
                    .Select(u => u.Departman!.Ad)
                    .Distinct()
                    .OrderBy(ad => ad)
                    .ToList(),
                AktifKullaniciSayisi = kullanicilar.Count,
                YoneticiSayisi = kullanicilar.Count(u =>
                    astGruplari.ContainsKey(u.Id)),
                DepartmanSayisi = kullanicilar
                    .Where(u => u.DepartmanId.HasValue)
                    .Select(u => u.DepartmanId)
                    .Distinct()
                    .Count(),
                EnDerinSeviye = kokDugumler.Count == 0
                    ? 0
                    : kokDugumler.Max(DugumDerinligi),
                AdminMi = User.IsInRole("Admin")
            };

            return View(model);
        }

        // =====================================================
        // DEPARTMAN VE UNVAN YÖNETİMİ
        // =====================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Yonetim()
        {
            return View(YonetimModeliOlustur());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DepartmanEkle(DepartmanFormViewModel model)
        {
            FormMetinleriniTemizle(model);
            DepartmanFormunuDogrula(model, yeniKayitMi: true);

            if (!ModelState.IsValid)
            {
                TempData["HataMesaji"] = IlkModelHatasiniGetir();
                TempData["AktifYonetimSekmesi"] = "departman";
                return RedirectToAction(nameof(Yonetim));
            }

            var departman = new Departman
            {
                Ad = model.Ad,
                Aciklama = model.Aciklama,
                UstDepartmanId = model.UstDepartmanId
            };

            _context.Departmanlar.Add(departman);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{departman.Ad} departmanı oluşturuldu.";
            TempData["AktifYonetimSekmesi"] = "departman";

            return RedirectToAction(nameof(Yonetim));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DepartmanDuzenle(DepartmanFormViewModel model)
        {
            var departman = _context.Departmanlar
                .FirstOrDefault(d => d.Id == model.Id);

            if (departman == null)
            {
                return NotFound();
            }

            FormMetinleriniTemizle(model);
            DepartmanFormunuDogrula(model, yeniKayitMi: false);

            if (!ModelState.IsValid)
            {
                TempData["HataMesaji"] = IlkModelHatasiniGetir();
                TempData["AktifYonetimSekmesi"] = "departman";
                return RedirectToAction(nameof(Yonetim));
            }

            departman.Ad = model.Ad;
            departman.Aciklama = model.Aciklama;
            departman.UstDepartmanId = model.UstDepartmanId;

            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{departman.Ad} departmanı güncellendi.";
            TempData["AktifYonetimSekmesi"] = "departman";

            return RedirectToAction(nameof(Yonetim));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DepartmanSil(int id)
        {
            var departman = _context.Departmanlar
                .FirstOrDefault(d => d.Id == id);

            if (departman == null)
            {
                return NotFound();
            }

            var kullaniliyorMu = _context.Kullanicilar
                .Any(u => u.DepartmanId == id);

            if (kullaniliyorMu)
            {
                TempData["HataMesaji"] =
                    "Bu departmana bağlı kullanıcılar bulunduğu için silinemez. " +
                    "Önce kullanıcıları başka bir departmana taşıyın.";
                TempData["AktifYonetimSekmesi"] = "departman";
                return RedirectToAction(nameof(Yonetim));
            }

            var altDepartmaniVarMi = _context.Departmanlar
                .Any(d => d.UstDepartmanId == id);

            if (altDepartmaniVarMi)
            {
                TempData["HataMesaji"] =
                    "Bu departmanın alt departmanları bulunduğu için silinemez. " +
                    "Önce alt departmanları taşıyın veya silin.";
                TempData["AktifYonetimSekmesi"] = "departman";
                return RedirectToAction(nameof(Yonetim));
            }

            var departmanAdi = departman.Ad;
            _context.Departmanlar.Remove(departman);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{departmanAdi} departmanı silindi.";
            TempData["AktifYonetimSekmesi"] = "departman";

            return RedirectToAction(nameof(Yonetim));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnvanEkle(UnvanFormViewModel model)
        {
            FormMetinleriniTemizle(model);
            UnvanFormunuDogrula(model, yeniKayitMi: true);

            if (!ModelState.IsValid)
            {
                TempData["HataMesaji"] = IlkModelHatasiniGetir();
                TempData["AktifYonetimSekmesi"] = "unvan";
                return RedirectToAction(nameof(Yonetim));
            }

            var unvan = new Unvan
            {
                Ad = model.Ad,
                Aciklama = model.Aciklama
            };

            _context.Unvanlar.Add(unvan);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{unvan.Ad} unvanı oluşturuldu.";
            TempData["AktifYonetimSekmesi"] = "unvan";

            return RedirectToAction(nameof(Yonetim));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnvanDuzenle(UnvanFormViewModel model)
        {
            var unvan = _context.Unvanlar
                .FirstOrDefault(u => u.Id == model.Id);

            if (unvan == null)
            {
                return NotFound();
            }

            FormMetinleriniTemizle(model);
            UnvanFormunuDogrula(model, yeniKayitMi: false);

            if (!ModelState.IsValid)
            {
                TempData["HataMesaji"] = IlkModelHatasiniGetir();
                TempData["AktifYonetimSekmesi"] = "unvan";
                return RedirectToAction(nameof(Yonetim));
            }

            unvan.Ad = model.Ad;
            unvan.Aciklama = model.Aciklama;

            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{unvan.Ad} unvanı güncellendi.";
            TempData["AktifYonetimSekmesi"] = "unvan";

            return RedirectToAction(nameof(Yonetim));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnvanSil(int id)
        {
            var unvan = _context.Unvanlar
                .FirstOrDefault(u => u.Id == id);

            if (unvan == null)
            {
                return NotFound();
            }

            var kullaniliyorMu = _context.Kullanicilar
                .Any(u => u.UnvanId == id);

            if (kullaniliyorMu)
            {
                TempData["HataMesaji"] =
                    "Bu unvan kullanıcılara atanmış olduğu için silinemez. " +
                    "Önce kullanıcıların unvanını değiştirin.";
                TempData["AktifYonetimSekmesi"] = "unvan";
                return RedirectToAction(nameof(Yonetim));
            }

            var unvanAdi = unvan.Ad;
            _context.Unvanlar.Remove(unvan);
            _context.SaveChanges();

            TempData["BasariMesaji"] =
                $"{unvanAdi} unvanı silindi.";
            TempData["AktifYonetimSekmesi"] = "unvan";

            return RedirectToAction(nameof(Yonetim));
        }

        // =====================================================
        // ORGANİZASYON AĞACI YARDIMCILARI
        // =====================================================

        private OrganizasyonDugumViewModel DugumOlustur(
            AppUser kullanici,
            IReadOnlyDictionary<int, List<AppUser>> astGruplari,
            ISet<int> islenenKullaniciIdleri,
            ISet<int> mevcutYol)
        {
            var dugum = new OrganizasyonDugumViewModel
            {
                Id = kullanici.Id,
                TamAd = kullanici.TamAd,
                Email = kullanici.Email ?? string.Empty,
                DepartmanAdi = kullanici.Departman?.Ad ?? "Atanmamış",
                UnvanAdi = kullanici.Unvan?.Ad ?? "Atanmamış",
                KidemAdi = KidemAdiGetir(kullanici.KidemSeviyesi),
                OrganizasyonRoluAdi = OrganizasyonRoluAdiGetir(
                    kullanici.OrganizasyonRolu),
                SistemRolu = kullanici.Role == "Admin"
                    ? "Admin"
                    : "Kullanıcı",
                AktifMi = kullanici.AktifMi
            };

            // Aynı kullanıcı zincir içinde tekrar görülürse döngüyü burada keseriz.
            if (mevcutYol.Contains(kullanici.Id))
            {
                return dugum;
            }

            islenenKullaniciIdleri.Add(kullanici.Id);

            var yeniYol = new HashSet<int>(mevcutYol)
            {
                kullanici.Id
            };

            if (astGruplari.TryGetValue(kullanici.Id, out var astlar))
            {
                foreach (var ast in astlar)
                {
                    if (yeniYol.Contains(ast.Id))
                    {
                        continue;
                    }

                    dugum.Astlar.Add(
                        DugumOlustur(
                            ast,
                            astGruplari,
                            islenenKullaniciIdleri,
                            yeniYol
                        )
                    );
                }
            }

            dugum.DogrudanAstSayisi = dugum.Astlar.Count;
            return dugum;
        }

        private static int DugumDerinligi(OrganizasyonDugumViewModel dugum)
        {
            if (dugum.Astlar.Count == 0)
            {
                return 1;
            }

            return 1 + dugum.Astlar.Max(DugumDerinligi);
        }

        // =====================================================
        // YÖNETİM SAYFASI YARDIMCILARI
        // =====================================================

        private OrganizasyonYonetimViewModel YonetimModeliOlustur()
        {
            var departmanlar = _context.Departmanlar
                .AsNoTracking()
                .Include(d => d.UstDepartman)
                .Include(d => d.Kullanicilar)
                .Include(d => d.AltDepartmanlar)
                .OrderBy(d => d.Ad)
                .ToList();

            var departmanSeviyeSozlugu = DepartmanSeviyeleriniHesapla(
                departmanlar);

            var unvanlar = _context.Unvanlar
                .AsNoTracking()
                .Include(u => u.Kullanicilar)
                .OrderBy(u => u.Ad)
                .ToList();

            return new OrganizasyonYonetimViewModel
            {
                Departmanlar = departmanlar
                    .OrderBy(d => departmanSeviyeSozlugu[d.Id])
                    .ThenBy(d => d.Ad)
                    .Select(d => new DepartmanYonetimItemViewModel
                    {
                        Id = d.Id,
                        Ad = d.Ad,
                        Aciklama = d.Aciklama,
                        UstDepartmanId = d.UstDepartmanId,
                        UstDepartmanAdi = d.UstDepartman?.Ad ?? "Ana departman",
                        KullaniciSayisi = d.Kullanicilar.Count,
                        AltDepartmanSayisi = d.AltDepartmanlar.Count,
                        Seviye = departmanSeviyeSozlugu[d.Id]
                    })
                    .ToList(),
                Unvanlar = unvanlar
                    .Select(u => new UnvanYonetimItemViewModel
                    {
                        Id = u.Id,
                        Ad = u.Ad,
                        Aciklama = u.Aciklama,
                        KullaniciSayisi = u.Kullanicilar.Count
                    })
                    .ToList(),
                UstDepartmanSecenekleri = departmanlar
                    .OrderBy(d => d.Ad)
                    .ToList(),
                DepartmanaAtanmamisKullaniciSayisi = _context.Kullanicilar
                    .Count(u => !u.DepartmanId.HasValue),
                UnvaniAtanmamisKullaniciSayisi = _context.Kullanicilar
                    .Count(u => !u.UnvanId.HasValue)
            };
        }

        private void DepartmanFormunuDogrula(
            DepartmanFormViewModel model,
            bool yeniKayitMi)
        {
            if (string.IsNullOrWhiteSpace(model.Ad))
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Departman adı yalnızca boşluklardan oluşamaz."
                );
            }

            var ayniAdVarMi = _context.Departmanlar.Any(d =>
                (yeniKayitMi || d.Id != model.Id) &&
                d.Ad.ToUpper() == model.Ad.ToUpper());

            if (ayniAdVarMi)
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Aynı ada sahip başka bir departman bulunuyor."
                );
            }

            if (model.UstDepartmanId.HasValue)
            {
                var ustDepartmanVarMi = _context.Departmanlar
                    .Any(d => d.Id == model.UstDepartmanId.Value);

                if (!ustDepartmanVarMi)
                {
                    ModelState.AddModelError(
                        nameof(model.UstDepartmanId),
                        "Seçilen üst departman bulunamadı."
                    );
                }
            }

            if (!yeniKayitMi &&
                DepartmanDongusuOlustururMu(
                    model.Id,
                    model.UstDepartmanId))
            {
                ModelState.AddModelError(
                    nameof(model.UstDepartmanId),
                    "Bu üst departman seçimi hiyerarşide döngü oluşturur."
                );
            }
        }

        private void UnvanFormunuDogrula(
            UnvanFormViewModel model,
            bool yeniKayitMi)
        {
            if (string.IsNullOrWhiteSpace(model.Ad))
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Unvan adı yalnızca boşluklardan oluşamaz."
                );
            }

            var ayniAdVarMi = _context.Unvanlar.Any(u =>
                (yeniKayitMi || u.Id != model.Id) &&
                u.Ad.ToUpper() == model.Ad.ToUpper());

            if (ayniAdVarMi)
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Aynı ada sahip başka bir unvan bulunuyor."
                );
            }
        }

        private bool DepartmanDongusuOlustururMu(
            int departmanId,
            int? yeniUstDepartmanId)
        {
            if (!yeniUstDepartmanId.HasValue)
            {
                return false;
            }

            if (departmanId == yeniUstDepartmanId.Value)
            {
                return true;
            }

            var departmanlar = _context.Departmanlar
                .AsNoTracking()
                .Select(d => new
                {
                    d.Id,
                    d.UstDepartmanId
                })
                .ToDictionary(d => d.Id, d => d.UstDepartmanId);

            var ziyaretEdilenler = new HashSet<int>();
            int? incelenenId = yeniUstDepartmanId;

            while (incelenenId.HasValue &&
                   ziyaretEdilenler.Add(incelenenId.Value))
            {
                if (incelenenId.Value == departmanId)
                {
                    return true;
                }

                if (!departmanlar.TryGetValue(
                        incelenenId.Value,
                        out var ustDepartmanId))
                {
                    break;
                }

                incelenenId = ustDepartmanId;
            }

            return false;
        }

        private static Dictionary<int, int> DepartmanSeviyeleriniHesapla(
            IReadOnlyCollection<Departman> departmanlar)
        {
            var ustDepartmanSozlugu = departmanlar
                .ToDictionary(d => d.Id, d => d.UstDepartmanId);

            var sonuc = new Dictionary<int, int>();

            foreach (var departman in departmanlar)
            {
                var seviye = 0;
                var ziyaretEdilenler = new HashSet<int>();
                int? incelenenId = departman.UstDepartmanId;

                while (incelenenId.HasValue &&
                       ziyaretEdilenler.Add(incelenenId.Value) &&
                       ustDepartmanSozlugu.TryGetValue(
                           incelenenId.Value,
                           out var sonrakiUstId))
                {
                    seviye++;
                    incelenenId = sonrakiUstId;
                }

                sonuc[departman.Id] = seviye;
            }

            return sonuc;
        }

        private string IlkModelHatasiniGetir()
        {
            return ModelState.Values
                .SelectMany(deger => deger.Errors)
                .Select(hata => hata.ErrorMessage)
                .FirstOrDefault(mesaj =>
                    !string.IsNullOrWhiteSpace(mesaj))
                ?? "Form bilgileri geçerli değil. Alanları kontrol edip tekrar deneyin.";
        }

        private static void FormMetinleriniTemizle(
            DepartmanFormViewModel model)
        {
            model.Ad = model.Ad?.Trim() ?? string.Empty;
            model.Aciklama = BosMetniNullYap(model.Aciklama);
        }

        private static void FormMetinleriniTemizle(
            UnvanFormViewModel model)
        {
            model.Ad = model.Ad?.Trim() ?? string.Empty;
            model.Aciklama = BosMetniNullYap(model.Aciklama);
        }

        private static string? BosMetniNullYap(string? metin)
        {
            var temizMetin = metin?.Trim();
            return string.IsNullOrWhiteSpace(temizMetin)
                ? null
                : temizMetin;
        }

        private static int OrganizasyonRolSirasi(OrganizasyonRolu rol)
        {
            return rol switch
            {
                OrganizasyonRolu.GenelMudur => 0,
                OrganizasyonRolu.Direktor => 1,
                OrganizasyonRolu.DepartmanMuduru => 2,
                OrganizasyonRolu.TakimLideri => 3,
                _ => 4
            };
        }

        private static string KidemAdiGetir(KidemSeviyesi kidem)
        {
            return kidem == KidemSeviyesi.MidLevel
                ? "Mid-Level"
                : kidem.ToString();
        }

        private static string OrganizasyonRoluAdiGetir(
            OrganizasyonRolu rol)
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
    }
}
