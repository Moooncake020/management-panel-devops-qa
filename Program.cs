using YonetimPaneli.HealthChecks;
using System.Text.Json;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using YonetimPaneli.Models;
using YonetimPaneli.Security;
using YonetimPaneli.Services;

var turkceKultur = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkceKultur;
CultureInfo.DefaultThreadCurrentUICulture = turkceKultur;

var builder = WebApplication.CreateBuilder(args);

static bool JsonCevapBekleniyor(HttpRequest request)
{
    var accept = request.Headers.Accept.ToString();
    var requestedWith = request.Headers["X-Requested-With"].ToString();

    return requestedWith.Equals(
               "XMLHttpRequest",
               StringComparison.OrdinalIgnoreCase) ||
           accept.Contains(
               "application/json",
               StringComparison.OrdinalIgnoreCase) ||
           request.Path.StartsWithSegments("/Arama") ||
           request.Path.StartsWithSegments("/Planlama/DurumGuncelle");
}

// Güvenli olmayan POST/PUT/PATCH/DELETE isteklerine otomatik olarak
// anti-forgery doğrulaması uygular. Tag Helper ile oluşturulan formlar tokenı
// kendiliğinden üretir.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Ters proxy arkasında gerçek istemci IP'si ve HTTPS bilgisinin güvenli
// biçimde işlenmesini sağlar. Varsayılan güvenilir proxy sınırları korunur.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = builder.Environment.IsDevelopment()
        ? "yonetim_paneli_antiforgery"
        : "__Host-yonetim_paneli_antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Formun tamamı için üst sınır 6 MB'tır. Görsel servisinin dosya sınırı 5 MB
// olduğu için multipart başlıkları ve diğer form alanları için pay bırakılır.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 6_291_456;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Şifreler PBKDF2 tabanlı ASP.NET Core PasswordHasher ile saklanır.
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = 120_000;
});
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<SifreServisi>();

// Uygulama servisleri
builder.Services.AddScoped<OrganizasyonServisi>();
builder.Services.AddScoped<GorevYetkiServisi>();
builder.Services.AddScoped<GorevDurumServisi>();
builder.Services.AddScoped<GorevResimServisi>();
builder.Services.AddScoped<GorevAktiviteServisi>();
builder.Services.AddScoped<GorevZamanServisi>();
builder.Services.AddScoped<BildirimServisi>();

// Aynı IP adresinden giriş endpointine kısa sürede aşırı istek gönderilmesini
// sınırlar. Hesap bazlı kilitleme AuthController içinde ayrıca uygulanır.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        GuvenlikSabitleri.LoginRateLimitPolitikasi,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "bilinmeyen-ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Global arama, yazarken çalışan bir endpoint olduğu için kullanıcı başına
    // makul bir üst sınır uygulanır. Normal kullanımda debounce sayesinde bu
    // limite yaklaşılmaz; otomatik veya kötüye kullanım sorguları sınırlandırılır.
    options.AddPolicy(
        GuvenlikSabitleri.AramaRateLimitPolitikasi,
        httpContext => RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "bilinmeyen-kullanici",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(
                    CultureInfo.InvariantCulture);
        }

        if (context.HttpContext.Request.Path.StartsWithSegments("/Arama"))
        {
            context.HttpContext.Response.ContentType =
                "application/json; charset=utf-8";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    mesaj = "Çok fazla arama isteği gönderildi. Kısa bir süre sonra tekrar deneyin."
                },
                cancellationToken);
            return;
        }

        context.HttpContext.Response.ContentType =
            "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Çok fazla giriş denemesi yapıldı. Lütfen kısa bir süre sonra tekrar deneyin.",
            cancellationToken);
    };
});

// JWT anahtarı appsettings.json içinde tutulmaz. Development ortamındaki
// örnek anahtar appsettings.Development.json'dan, üretimde ise User Secrets,
// environment variable veya secret manager üzerinden gelmelidir.
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];
var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Issuer ve Jwt:Audience yapılandırmaları zorunludur.");
}

// Development ortamında anahtar tanımlanmamışsa bellekte rastgele anahtar
// oluşturulur. Bu anahtar dosyaya yazılmaz ve uygulama yeniden başladığında
// değişir. Kalıcı development oturumu için User Secrets kullanılabilir.
if (string.IsNullOrWhiteSpace(secretKey) &&
    builder.Environment.IsDevelopment())
{
    secretKey = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Jwt:Key"] = secretKey;
}

if (string.IsNullOrWhiteSpace(secretKey) ||
    Encoding.UTF8.GetByteCount(secretKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key yapılandırması bulunamadı veya en az 32 byte değil. " +
        "Üretimde anahtarı environment variable ya da secret manager ile tanımlayın.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = false;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue(
                    GuvenlikSabitleri.TokenCookieAdi,
                    out var token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        },

        // JWT geçerli olsa bile kullanıcı pasifleştirildiyse veya güvenlik
        // damgası değiştiyse eski oturum anında reddedilir.
        OnTokenValidated = async context =>
        {
            var kullaniciIdMetni = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenDamgasi = context.Principal?
                .FindFirstValue(GuvenlikSabitleri.GuvenlikDamgasiClaim);

            if (!int.TryParse(kullaniciIdMetni, out var kullaniciId) ||
                string.IsNullOrWhiteSpace(tokenDamgasi))
            {
                context.Fail("Geçersiz oturum bilgisi.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<AppDbContext>();

            var kullanici = await dbContext.Kullanicilar
                .AsNoTracking()
                .Where(u => u.Id == kullaniciId)
                .Select(u => new
                {
                    u.AktifMi,
                    u.GuvenlikDamgasi
                })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (kullanici == null ||
                !kullanici.AktifMi ||
                !string.Equals(
                    kullanici.GuvenlikDamgasi,
                    tokenDamgasi,
                    StringComparison.Ordinal))
            {
                context.HttpContext.Response.Cookies.Delete(
                    GuvenlikSabitleri.TokenCookieAdi);
                context.Fail("Oturum artık geçerli değil.");
            }
        },

        OnChallenge = async context =>
        {
            context.HandleResponse();

            if (context.Response.HasStarted)
            {
                return;
            }

            var returnUrl =
                $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            var girisUrl =
                "/Auth/Login?returnUrl=" +
                Uri.EscapeDataString(returnUrl);

            if (JsonCevapBekleniyor(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType =
                    "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    mesaj = "Oturumunuz sona erdi. Yeniden giriş yapmanız gerekiyor.",
                    girisUrl
                });
                return;
            }

            context.Response.Redirect(girisUrl);
        },

        OnForbidden = async context =>
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            if (JsonCevapBekleniyor(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType =
                    "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    mesaj = "Bu işlemi gerçekleştirmek için gerekli yetkiniz bulunmuyor."
                });
                return;
            }

            context.Response.Redirect("/Auth/Yetkisiz");
        }
    };
});

builder.Services.AddAuthorization();

// Sıkıştırma statik dosya ve HTML yanıtlarının ağ üzerinden daha küçük
// taşınmasını sağlar. HTTPS üzerinde de etkinleştirilir.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddHealthChecks()
    .AddCheck<VeritabaniSaglikKontrolu>("veritabani");

var app = builder.Build();

app.UseForwardedHeaders();

// Migrationlar bilinçli olarak uygulama başlangıcında otomatik çalıştırılmaz.
// İlk kurulumda Package Manager Console üzerinden Update-Database çalıştırılır.
// Bu yaklaşım migration hatalarının uygulama açılışında gizlenmesini önler.

// Referans verileri, development test hesapları ve eski düz metin şifrelerin
// güvenli hashlere dönüştürülmesi.
await SeedData.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Controller tarafından ele alınmayan 404 gibi durumlar tutarlı hata
// sayfasına yeniden yürütülür; adres çubuğu değiştirilmez.
app.UseStatusCodePagesWithReExecute(
    "/Home/StatusCode",
    "?code={0}");

app.UseHttpsRedirection();
app.UseResponseCompression();

// Frontend varlıkları yerel olarak üretildiği ve inline script/stil
// kullanılmadığı için üretimde katı Content Security Policy uygulanabilir.
// Development ortamında hata sayfası ve browser refresh araçlarını bozmamak
// için aynı politika yalnızca Report-Only olarak gönderilir.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Request-ID"] = context.TraceIdentifier;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

    var contentSecurityPolicy = string.Join("; ", new[]
    {
        "default-src 'self'",
        "base-uri 'self'",
        "object-src 'none'",
        "frame-ancestors 'none'",
        "form-action 'self'",
        "script-src 'self'",
        "style-src 'self'",
        "img-src 'self' data: blob:",
        "font-src 'self'",
        "connect-src 'self'",
        "media-src 'self'",
        "worker-src 'self' blob:",
        "manifest-src 'self'",
        "upgrade-insecure-requests"
    });

    context.Response.Headers[
        app.Environment.IsDevelopment()
            ? "Content-Security-Policy-Report-Only"
            : "Content-Security-Policy"] = contentSecurityPolicy;

    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // asp-append-version ile kullanılan dosyalar içerik hashine sahip olduğu
        // için tarayıcıda güvenle uzun süre saklanabilir.
        context.Context.Response.Headers.CacheControl =
            "public,max-age=2592000,immutable";
    }
});
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Kimlik doğrulamalı sayfaların ve kişiye özel JSON yanıtlarının tarayıcı
// geçmişinde veya ortak proxylerde saklanmasını engeller. Statik dosyalar bu
// middleware'e gelmeden önce uzun süreli cache politikasıyla servis edilir.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.User.Identity?.IsAuthenticated == true ||
            context.Request.Path.StartsWithSegments("/Auth"))
        {
            context.Response.Headers.CacheControl = "no-store, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
        }

        return Task.CompletedTask;
    });

    await next();
});

app.MapHealthChecks("/saglik", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store, max-age=0";
        object sonuc = app.Environment.IsDevelopment()
            ? new
            {
                durum = report.Status.ToString(),
                sureMs = report.TotalDuration.TotalMilliseconds,
                kontroller = report.Entries.ToDictionary(
                    x => x.Key,
                    x => new
                    {
                        durum = x.Value.Status.ToString(),
                        aciklama = x.Value.Description
                    })
            }
            : new
            {
                durum = report.Status.ToString(),
                sureMs = Math.Round(report.TotalDuration.TotalMilliseconds)
            };

        await context.Response.WriteAsync(JsonSerializer.Serialize(sonuc));
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// WebApplicationFactory tabanlı entegrasyon testlerinin Program sınıfına
// erişebilmesi için partial tanım kullanılır.
public partial class Program { }
