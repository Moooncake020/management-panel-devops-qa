using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using YonetimPaneli.Models;
using YonetimPaneli.Security;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Auth;

namespace YonetimPaneli.Controllers
{
    public class AuthController : Controller
    {
        private const int MaksimumBasarisizGiris = 5;
        private static readonly TimeSpan HesapKilitSuresi =
            TimeSpan.FromMinutes(15);

        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly SifreServisi _sifreServisi;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration config,
            SifreServisi sifreServisi,
            IWebHostEnvironment environment,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _sifreServisi = sifreServisi;
            _environment = environment;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel
            {
                ReturnUrl = YerelDonusAdresiGetir(returnUrl)
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [EnableRateLimiting(GuvenlikSabitleri.LoginRateLimitPolitikasi)]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.Email = EmailNormalizeEt(model.Email);
            model.ReturnUrl = YerelDonusAdresiGetir(model.ReturnUrl);

            // Model binding doğrulaması trim işleminden önce çalışır. Normalize
            // edilmiş e-posta üzerinden yeniden doğrulayarak baştaki/sondaki
            // boşlukların geçerli hesabı engellemesini önleriz.
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == model.Email);

            // Hesabın sistemde bulunup bulunmadığı dışarıya açıklanmaz.
            if (kullanici == null || !kullanici.AktifMi)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-posta veya şifre hatalı.");

                return View(model);
            }

            var simdi = DateTime.UtcNow;

            if (kullanici.KilitBitisTarihi.HasValue &&
                kullanici.KilitBitisTarihi.Value > simdi)
            {
                var kalanDakika = Math.Max(
                    1,
                    (int)Math.Ceiling(
                        (kullanici.KilitBitisTarihi.Value - simdi)
                        .TotalMinutes));

                ModelState.AddModelError(
                    string.Empty,
                    $"Hesap geçici olarak kilitli. Yaklaşık {kalanDakika} dakika sonra tekrar deneyin.");

                return View(model);
            }

            // Önceki kilit süresi dolmuşsa sayaç temizlenir.
            if (kullanici.KilitBitisTarihi.HasValue)
            {
                kullanici.KilitBitisTarihi = null;
                kullanici.BasarisizGirisSayisi = 0;
            }

            var dogrulamaSonucu = _sifreServisi.Dogrula(
                kullanici,
                model.Password);

            if (dogrulamaSonucu == PasswordVerificationResult.Failed)
            {
                kullanici.BasarisizGirisSayisi++;

                if (kullanici.BasarisizGirisSayisi >=
                    MaksimumBasarisizGiris)
                {
                    kullanici.KilitBitisTarihi =
                        simdi.Add(HesapKilitSuresi);
                    kullanici.BasarisizGirisSayisi = 0;

                    ModelState.AddModelError(
                        string.Empty,
                        "Çok sayıda hatalı deneme nedeniyle hesap 15 dakika kilitlendi.");
                }
                else
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "E-posta veya şifre hatalı.");
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Başarısız giriş denemesi. KullanıcıId: {KullaniciId}, IP: {IpAdresi}",
                    kullanici.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return View(model);
            }

            // Eski düz metin veya eski parametreli hash ilk başarılı girişte
            // güncel PasswordHasher formatına dönüştürülür.
            if (dogrulamaSonucu ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                kullanici.Password = _sifreServisi.Hashle(
                    kullanici,
                    model.Password);
                kullanici.SifreDegistirmeTarihi ??= simdi;
            }

            if (string.IsNullOrWhiteSpace(kullanici.GuvenlikDamgasi))
            {
                _sifreServisi.GuvenlikDamgasiniYenile(kullanici);
            }

            kullanici.BasarisizGirisSayisi = 0;
            kullanici.KilitBitisTarihi = null;
            kullanici.SonGirisTarihi = simdi;

            await _context.SaveChangesAsync();

            var tokenSuresi = TokenSuresiGetir(model.BeniHatirla);
            var token = TokenOlustur(kullanici, tokenSuresi);

            Response.Cookies.Append(
                GuvenlikSabitleri.TokenCookieAdi,
                token,
                TokenCookieAyarlari(
                    tokenSuresi,
                    model.BeniHatirla));

            _logger.LogInformation(
                "Kullanıcı giriş yaptı. KullanıcıId: {KullaniciId}",
                kullanici.Id);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public IActionResult SifreDegistir()
        {
            return View(new SifreDegistirViewModel());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SifreDegistir(
            SifreDegistirViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var kullaniciIdMetni =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(kullaniciIdMetni, out var kullaniciId))
            {
                return Forbid();
            }

            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(u =>
                    u.Id == kullaniciId &&
                    u.AktifMi);

            if (kullanici == null)
            {
                return NotFound();
            }

            var mevcutSifreSonucu = _sifreServisi.Dogrula(
                kullanici,
                model.MevcutSifre);

            if (mevcutSifreSonucu == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    nameof(model.MevcutSifre),
                    "Mevcut şifre doğru değil.");

                return View(model);
            }

            var yeniSifreMevcutlaAyni = _sifreServisi.Dogrula(
                kullanici,
                model.YeniSifre) != PasswordVerificationResult.Failed;

            if (yeniSifreMevcutlaAyni)
            {
                ModelState.AddModelError(
                    nameof(model.YeniSifre),
                    "Yeni şifre mevcut şifreyle aynı olamaz.");

                return View(model);
            }

            kullanici.Password = _sifreServisi.Hashle(
                kullanici,
                model.YeniSifre);
            kullanici.SifreDegistirmeTarihi = DateTime.UtcNow;
            kullanici.BasarisizGirisSayisi = 0;
            kullanici.KilitBitisTarihi = null;
            _sifreServisi.GuvenlikDamgasiniYenile(kullanici);

            await _context.SaveChangesAsync();

            TokenCookieSil();

            TempData["BasariMesaji"] =
                "Şifreniz değiştirildi. Yeni şifrenizle tekrar giriş yapın.";

            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Logout()
        {
            TokenCookieSil();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Yetkisiz()
        {
            return View();
        }

        private string TokenOlustur(
            AppUser kullanici,
            TimeSpan tokenSuresi)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new(ClaimTypes.Name, kullanici.TamAd),
                new(ClaimTypes.Role, kullanici.Role ?? "Kullanici"),
                new(ClaimTypes.Email, kullanici.Email ?? string.Empty),
                new(
                    GuvenlikSabitleri.GuvenlikDamgasiClaim,
                    kullanici.GuvenlikDamgasi),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            var secretKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key yapılandırması bulunamadı.");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(tokenSuresi),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private TimeSpan TokenSuresiGetir(bool beniHatirla)
        {
            if (beniHatirla)
            {
                var gun = Math.Clamp(
                    _config.GetValue<int?>("Jwt:RememberMeDays") ?? 7,
                    1,
                    30);

                return TimeSpan.FromDays(gun);
            }

            var dakika = Math.Clamp(
                _config.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 120,
                15,
                1_440);

            return TimeSpan.FromMinutes(dakika);
        }

        private CookieOptions TokenCookieAyarlari(
            TimeSpan tokenSuresi,
            bool beniHatirla)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment() || Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/"
            };

            // "Beni hatırla" seçilmediyse tarayıcı kapanınca cookie silinen
            // oturum cookie'si olarak bırakılır. JWT'nin kendi süresi yine
            // sunucu tarafında doğrulanır.
            if (beniHatirla)
            {
                options.Expires = DateTimeOffset.UtcNow.Add(tokenSuresi);
            }

            return options;
        }

        private void TokenCookieSil()
        {
            Response.Cookies.Delete(
                GuvenlikSabitleri.TokenCookieAdi,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !_environment.IsDevelopment() || Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
        }

        private string? YerelDonusAdresiGetir(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) ||
                !Url.IsLocalUrl(returnUrl))
            {
                return null;
            }

            return returnUrl;
        }

        private static string EmailNormalizeEt(string? email)
        {
            return (email ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }
    }
}
