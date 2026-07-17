using Microsoft.EntityFrameworkCore;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Services;

namespace YonetimPaneli.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sifreServisi = scope.ServiceProvider.GetRequiredService<SifreServisi>();
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("SeedData");

            await DepartmanlariOlustur(context);
            await UnvanlariOlustur(context);

            if (environment.IsDevelopment())
            {
                await TestKullanicilariniOlustur(context, sifreServisi);
            }
            else
            {
                await IlkAdminiOlustur(
                    context,
                    sifreServisi,
                    configuration);
            }

            var donusturulenSifreSayisi = await EskiSifreleriDonustur(
                context,
                sifreServisi);

            if (donusturulenSifreSayisi > 0)
            {
                logger.LogInformation(
                    "{Sayi} kullanıcının eski düz metin şifresi güvenli hashe dönüştürüldü.",
                    donusturulenSifreSayisi);
            }
        }


        private static async Task DepartmanlariOlustur(AppDbContext context)
        {
            if (await context.Departmanlar.AnyAsync())
            {
                return;
            }

            var yonetim = new Departman
            {
                Ad = "Yönetim",
                Aciklama = "Şirketin üst yönetim birimi"
            };

            var teknoloji = new Departman
            {
                Ad = "Teknoloji",
                Aciklama = "Teknoloji ekiplerinin bağlı olduğu ana departman",
                UstDepartman = yonetim
            };

            var yazilimGelistirme = new Departman
            {
                Ad = "Yazılım Geliştirme",
                Aciklama = "Yazılım geliştirme ekiplerinin ana birimi",
                UstDepartman = teknoloji
            };

            var backend = new Departman
            {
                Ad = "Backend",
                Aciklama = "Sunucu tarafı uygulama geliştirme ekibi",
                UstDepartman = yazilimGelistirme
            };

            var frontend = new Departman
            {
                Ad = "Frontend",
                Aciklama = "Web arayüz geliştirme ekibi",
                UstDepartman = yazilimGelistirme
            };

            var mobil = new Departman
            {
                Ad = "Mobil",
                Aciklama = "Mobil uygulama geliştirme ekibi",
                UstDepartman = yazilimGelistirme
            };

            var qa = new Departman
            {
                Ad = "QA ve Test",
                Aciklama = "Kalite güvence ve test süreçleri",
                UstDepartman = teknoloji
            };

            var devops = new Departman
            {
                Ad = "DevOps ve Platform",
                Aciklama = "CI/CD, altyapı ve platform operasyonları",
                UstDepartman = teknoloji
            };

            var urunVeProje = new Departman
            {
                Ad = "Ürün ve Proje",
                Aciklama = "Ürün ve proje yönetim süreçleri",
                UstDepartman = yonetim
            };

            var urunYonetimi = new Departman
            {
                Ad = "Ürün Yönetimi",
                UstDepartman = urunVeProje
            };

            var projeYonetimi = new Departman
            {
                Ad = "Proje Yönetimi",
                UstDepartman = urunVeProje
            };

            var isAnalizi = new Departman
            {
                Ad = "İş Analizi",
                UstDepartman = urunVeProje
            };

            var uiUx = new Departman
            {
                Ad = "UI/UX Tasarım",
                UstDepartman = urunVeProje
            };

            context.Departmanlar.AddRange(
                yonetim,
                teknoloji,
                yazilimGelistirme,
                backend,
                frontend,
                mobil,
                qa,
                devops,
                urunVeProje,
                urunYonetimi,
                projeYonetimi,
                isAnalizi,
                uiUx
            );

            await context.SaveChangesAsync();
        }


        private static async Task UnvanlariOlustur(AppDbContext context)
        {
            if (await context.Unvanlar.AnyAsync())
            {
                return;
            }

            context.Unvanlar.AddRange(
                new Unvan { Ad = "Genel Müdür" },
                new Unvan { Ad = "Teknoloji Direktörü" },
                new Unvan { Ad = "Yazılım Geliştirme Müdürü" },
                new Unvan { Ad = "Backend Developer" },
                new Unvan { Ad = "Frontend Developer" },
                new Unvan { Ad = "Mobile Developer" },
                new Unvan { Ad = "QA Engineer" },
                new Unvan { Ad = "Test Automation Engineer" },
                new Unvan { Ad = "DevOps Engineer" },
                new Unvan { Ad = "Site Reliability Engineer" },
                new Unvan { Ad = "Product Manager" },
                new Unvan { Ad = "Project Manager" },
                new Unvan { Ad = "Business Analyst" },
                new Unvan { Ad = "UI/UX Designer" }
            );

            await context.SaveChangesAsync();
        }


        private static async Task TestKullanicilariniOlustur(
            AppDbContext context,
            SifreServisi sifreServisi)
        {
            var yonetim = await DepartmanGetir(context, "Yönetim");
            var yazilimGelistirme = await DepartmanGetir(context, "Yazılım Geliştirme");
            var backend = await DepartmanGetir(context, "Backend");
            var frontend = await DepartmanGetir(context, "Frontend");

            var genelMudurUnvani = await UnvanGetir(context, "Genel Müdür");
            var yazilimMuduruUnvani = await UnvanGetir(context, "Yazılım Geliştirme Müdürü");
            var backendDeveloperUnvani = await UnvanGetir(context, "Backend Developer");
            var frontendDeveloperUnvani = await UnvanGetir(context, "Frontend Developer");

            var admin = await KullaniciGetirVeyaOlustur(
                context,
                sifreServisi,
                email: "admin@test.com",
                ad: "Admin",
                soyad: "Test",
                departmanId: yonetim?.Id,
                unvanId: genelMudurUnvani?.Id,
                kidemSeviyesi: KidemSeviyesi.Principal,
                organizasyonRolu: OrganizasyonRolu.GenelMudur,
                yoneticiId: null,
                role: "Admin"
            );

            var ahmet = await KullaniciGetirVeyaOlustur(
                context,
                sifreServisi,
                email: "ahmet@test.com",
                ad: "Ahmet",
                soyad: "Müdür",
                departmanId: yazilimGelistirme?.Id,
                unvanId: yazilimMuduruUnvani?.Id,
                kidemSeviyesi: KidemSeviyesi.Senior,
                organizasyonRolu: OrganizasyonRolu.DepartmanMuduru,
                yoneticiId: null,
                role: "Kullanici"
            );

            var mehmet = await KullaniciGetirVeyaOlustur(
                context,
                sifreServisi,
                email: "mehmet@test.com",
                ad: "Mehmet",
                soyad: "Lead",
                departmanId: backend?.Id,
                unvanId: backendDeveloperUnvani?.Id,
                kidemSeviyesi: KidemSeviyesi.Senior,
                organizasyonRolu: OrganizasyonRolu.TakimLideri,
                yoneticiId: ahmet.Id,
                role: "Kullanici"
            );

            await KullaniciGetirVeyaOlustur(
                context,
                sifreServisi,
                email: "kemal@test.com",
                ad: "Kemal",
                soyad: "Dev",
                departmanId: backend?.Id,
                unvanId: backendDeveloperUnvani?.Id,
                kidemSeviyesi: KidemSeviyesi.Junior,
                organizasyonRolu: OrganizasyonRolu.Calisan,
                yoneticiId: mehmet.Id,
                role: "Kullanici"
            );

            await KullaniciGetirVeyaOlustur(
                context,
                sifreServisi,
                email: "ayse@test.com",
                ad: "Ayşe",
                soyad: "Frontend",
                departmanId: frontend?.Id,
                unvanId: frontendDeveloperUnvani?.Id,
                kidemSeviyesi: KidemSeviyesi.Junior,
                organizasyonRolu: OrganizasyonRolu.Calisan,
                yoneticiId: ahmet.Id,
                role: "Kullanici"
            );
        }


        private static async Task<AppUser> KullaniciGetirVeyaOlustur(
            AppDbContext context,
            SifreServisi sifreServisi,
            string email,
            string ad,
            string soyad,
            int? departmanId,
            int? unvanId,
            KidemSeviyesi kidemSeviyesi,
            OrganizasyonRolu organizasyonRolu,
            int? yoneticiId,
            string role)
        {
            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(u => u.Email == email);

            if (kullanici == null)
            {
                kullanici = new AppUser
                {
                    Email = email,
                    Password = string.Empty,
                    Ad = ad,
                    Soyad = soyad,
                    DepartmanId = departmanId,
                    UnvanId = unvanId,
                    KidemSeviyesi = kidemSeviyesi,
                    OrganizasyonRolu = organizasyonRolu,
                    YoneticiId = yoneticiId,
                    Role = role,
                    AktifMi = true
                };

                kullanici.Password = sifreServisi.Hashle(
                    kullanici,
                    "123456");
                kullanici.SifreDegistirmeTarihi = DateTime.UtcNow;
                sifreServisi.GuvenlikDamgasiniYenile(kullanici);

                context.Kullanicilar.Add(kullanici);
                await context.SaveChangesAsync();

                return kullanici;
            }

            var guvenlikBilgisiDegistiMi =
                kullanici.Role != role ||
                !kullanici.AktifMi;

            kullanici.Ad = ad;
            kullanici.Soyad = soyad;
            kullanici.DepartmanId = departmanId;
            kullanici.UnvanId = unvanId;
            kullanici.KidemSeviyesi = kidemSeviyesi;
            kullanici.OrganizasyonRolu = organizasyonRolu;
            kullanici.YoneticiId = yoneticiId;
            kullanici.Role = role;
            kullanici.AktifMi = true;

            if (string.IsNullOrWhiteSpace(kullanici.Password))
            {
                kullanici.Password = sifreServisi.Hashle(
                    kullanici,
                    "123456");
                kullanici.SifreDegistirmeTarihi = DateTime.UtcNow;
                guvenlikBilgisiDegistiMi = true;
            }

            if (string.IsNullOrWhiteSpace(kullanici.GuvenlikDamgasi) ||
                guvenlikBilgisiDegistiMi)
            {
                sifreServisi.GuvenlikDamgasiniYenile(kullanici);
            }

            await context.SaveChangesAsync();

            return kullanici;
        }


        private static async Task<int> EskiSifreleriDonustur(
            AppDbContext context,
            SifreServisi sifreServisi)
        {
            var kullanicilar = await context.Kullanicilar.ToListAsync();
            var donusturulenSifreSayisi = 0;
            var degisiklikVarMi = false;

            foreach (var kullanici in kullanicilar)
            {
                if (string.IsNullOrWhiteSpace(kullanici.GuvenlikDamgasi))
                {
                    sifreServisi.GuvenlikDamgasiniYenile(kullanici);
                    degisiklikVarMi = true;
                }

                if (string.IsNullOrWhiteSpace(kullanici.Password) ||
                    sifreServisi.HashFormatindaMi(kullanici.Password))
                {
                    continue;
                }

                var eskiDuzMetinSifre = kullanici.Password;
                kullanici.Password = sifreServisi.Hashle(
                    kullanici,
                    eskiDuzMetinSifre);
                kullanici.SifreDegistirmeTarihi ??= DateTime.UtcNow;
                sifreServisi.GuvenlikDamgasiniYenile(kullanici);

                donusturulenSifreSayisi++;
                degisiklikVarMi = true;
            }

            if (degisiklikVarMi)
            {
                await context.SaveChangesAsync();
            }

            return donusturulenSifreSayisi;
        }


        private static async Task IlkAdminiOlustur(
            AppDbContext context,
            SifreServisi sifreServisi,
            IConfiguration configuration)
        {
            if (await context.Kullanicilar.AnyAsync())
            {
                return;
            }

            var email = configuration["BootstrapAdmin:Email"]?.Trim();
            var sifre = configuration["BootstrapAdmin:Password"];
            var ad = configuration["BootstrapAdmin:Ad"]?.Trim() ?? "Sistem";
            var soyad = configuration["BootstrapAdmin:Soyad"]?.Trim() ?? "Yöneticisi";

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(sifre))
            {
                throw new InvalidOperationException(
                    "Üretim veritabanında kullanıcı bulunmuyor. " +
                    "BootstrapAdmin:Email ve BootstrapAdmin:Password " +
                    "değerlerini secret manager üzerinden tanımlayın.");
            }

            if (!GucluSifreMi(sifre))
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin:Password en az 8 karakter olmalı; " +
                    "büyük harf, küçük harf ve rakam içermelidir.");
            }

            var yonetim = await DepartmanGetir(context, "Yönetim");
            var genelMudurUnvani = await UnvanGetir(context, "Genel Müdür");

            var admin = new AppUser
            {
                Email = email.ToLowerInvariant(),
                Password = string.Empty,
                Ad = ad,
                Soyad = soyad,
                DepartmanId = yonetim?.Id,
                UnvanId = genelMudurUnvani?.Id,
                KidemSeviyesi = KidemSeviyesi.Principal,
                OrganizasyonRolu = OrganizasyonRolu.GenelMudur,
                Role = "Admin",
                AktifMi = true,
                SifreDegistirmeTarihi = DateTime.UtcNow
            };

            admin.Password = sifreServisi.Hashle(admin, sifre);
            sifreServisi.GuvenlikDamgasiniYenile(admin);

            context.Kullanicilar.Add(admin);
            await context.SaveChangesAsync();
        }


        private static bool GucluSifreMi(string sifre)
        {
            return sifre.Length >= 8 &&
                   sifre.Length <= 100 &&
                   sifre.Any(char.IsUpper) &&
                   sifre.Any(char.IsLower) &&
                   sifre.Any(char.IsDigit) &&
                   !sifre.Any(char.IsWhiteSpace);
        }


        private static Task<Departman?> DepartmanGetir(
            AppDbContext context,
            string ad)
        {
            return context.Departmanlar
                .FirstOrDefaultAsync(d => d.Ad == ad);
        }


        private static Task<Unvan?> UnvanGetir(
            AppDbContext context,
            string ad)
        {
            return context.Unvanlar
                .FirstOrDefaultAsync(u => u.Ad == ad);
        }
    }
}
