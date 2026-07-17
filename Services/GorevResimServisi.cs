using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace YonetimPaneli.Services
{
    /// <summary>
    /// Görsel kaydetme işleminin başarılı olup olmadığını ve kullanıcıya
    /// gösterilecek hata mesajını birlikte taşır.
    /// </summary>
    public class GorevResimKaydetSonucu
    {
        public bool Basarili { get; init; }
        public string? WebYolu { get; init; }
        public string? HataMesaji { get; init; }

        public static GorevResimKaydetSonucu Basari(string webYolu) =>
            new()
            {
                Basarili = true,
                WebYolu = webYolu
            };

        public static GorevResimKaydetSonucu Hata(string hataMesaji) =>
            new()
            {
                Basarili = false,
                HataMesaji = hataMesaji
            };
    }

    /// <summary>
    /// Görev görsellerinin güvenli biçimde doğrulanması, kaydedilmesi ve
    /// gerektiğinde silinmesinden sorumludur.
    /// </summary>
    public class GorevResimServisi
    {
        public const long MaksimumDosyaBoyutu = 5 * 1024 * 1024;

        private static readonly HashSet<string> IzinliUzantilar =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private static readonly Dictionary<string, HashSet<string>> IzinliMimeTipleri =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg",
                    "image/jpg"
                },
                [".jpeg"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg",
                    "image/jpg"
                },
                [".png"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "image/png"
                },
                [".webp"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "image/webp"
                }
            };

        private readonly IWebHostEnvironment _environment;

        public GorevResimServisi(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<GorevResimKaydetSonucu> KaydetAsync(
            IFormFile dosya,
            CancellationToken cancellationToken = default)
        {
            if (dosya.Length <= 0)
            {
                return GorevResimKaydetSonucu.Hata(
                    "Seçilen görsel boş veya okunamıyor."
                );
            }

            if (dosya.Length > MaksimumDosyaBoyutu)
            {
                return GorevResimKaydetSonucu.Hata(
                    "Görev görseli en fazla 5 MB olabilir."
                );
            }

            var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();

            if (!IzinliUzantilar.Contains(uzanti))
            {
                return GorevResimKaydetSonucu.Hata(
                    "Yalnızca JPG, JPEG, PNG veya WEBP görselleri yüklenebilir."
                );
            }

            if (!IzinliMimeTipleri.TryGetValue(uzanti, out var mimeTipleri) ||
                !mimeTipleri.Contains(dosya.ContentType))
            {
                return GorevResimKaydetSonucu.Hata(
                    "Dosyanın içerik türü ile uzantısı uyuşmuyor."
                );
            }

            string? tamYol = null;

            try
            {
                if (!await DosyaImzasiGecerliMiAsync(
                        dosya,
                        uzanti,
                        cancellationToken))
                {
                    return GorevResimKaydetSonucu.Hata(
                        "Seçilen dosya geçerli bir görsel dosyası değil."
                    );
                }

                var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                    ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                    : _environment.WebRootPath;

                var klasor = Path.Combine(
                    webRootPath,
                    "uploads",
                    "gorevler"
                );

                Directory.CreateDirectory(klasor);

                // Kullanıcının gönderdiği dosya adı kullanılmaz.
                // GUID tabanlı ad, klasör geçişi ve çakışma risklerini azaltır.
                var dosyaAdi = $"{Guid.NewGuid():N}{uzanti}";
                tamYol = Path.Combine(klasor, dosyaAdi);

                await using var hedefStream = new FileStream(
                    tamYol,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true
                );

                await dosya.CopyToAsync(hedefStream, cancellationToken);

                return GorevResimKaydetSonucu.Basari(
                    $"/uploads/gorevler/{dosyaAdi}"
                );
            }
            catch (OperationCanceledException)
            {
                KismiDosyayiTemizle(tamYol);
                throw;
            }
            catch (IOException)
            {
                KismiDosyayiTemizle(tamYol);

                return GorevResimKaydetSonucu.Hata(
                    "Görsel dosyası kaydedilirken dosya sistemi hatası oluştu."
                );
            }
            catch (UnauthorizedAccessException)
            {
                KismiDosyayiTemizle(tamYol);

                return GorevResimKaydetSonucu.Hata(
                    "Görsel klasörüne yazma izni bulunamadı."
                );
            }
        }

        /// <summary>
        /// Yalnızca bu servisin yönettiği /uploads/gorevler klasöründeki
        /// dosyaları siler. Eski veya dışarıdan gelen yollar bilerek silinmez.
        /// </summary>
        public void Sil(string? webYolu)
        {
            if (string.IsNullOrWhiteSpace(webYolu) ||
                !webYolu.StartsWith(
                    "/uploads/gorevler/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var dosyaAdi = Path.GetFileName(webYolu);

            if (string.IsNullOrWhiteSpace(dosyaAdi))
            {
                return;
            }

            var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;

            var tamYol = Path.Combine(
                webRootPath,
                "uploads",
                "gorevler",
                dosyaAdi
            );

            try
            {
                if (File.Exists(tamYol))
                {
                    File.Delete(tamYol);
                }
            }
            catch (IOException)
            {
                // Veritabanı işlemi başarıyla tamamlandıysa yalnızca dosya silme
                // hatası nedeniyle kullanıcı işlemini başarısız göstermiyoruz.
            }
            catch (UnauthorizedAccessException)
            {
                // Üretim ortamında bu durum loglanmalıdır.
            }
        }

        private static void KismiDosyayiTemizle(string? tamYol)
        {
            if (string.IsNullOrWhiteSpace(tamYol))
            {
                return;
            }

            try
            {
                if (File.Exists(tamYol))
                {
                    File.Delete(tamYol);
                }
            }
            catch
            {
                // Asıl yükleme hatasını maskelememek için temizleme hatası yutulur.
            }
        }

        private static async Task<bool> DosyaImzasiGecerliMiAsync(
            IFormFile dosya,
            string uzanti,
            CancellationToken cancellationToken)
        {
            var baslik = new byte[12];

            await using var stream = dosya.OpenReadStream();
            var okunanBayt = await stream.ReadAsync(
                baslik.AsMemory(0, baslik.Length),
                cancellationToken
            );

            return uzanti switch
            {
                ".jpg" or ".jpeg" =>
                    okunanBayt >= 3 &&
                    baslik[0] == 0xFF &&
                    baslik[1] == 0xD8 &&
                    baslik[2] == 0xFF,

                ".png" =>
                    okunanBayt >= 8 &&
                    baslik[0] == 0x89 &&
                    baslik[1] == 0x50 &&
                    baslik[2] == 0x4E &&
                    baslik[3] == 0x47 &&
                    baslik[4] == 0x0D &&
                    baslik[5] == 0x0A &&
                    baslik[6] == 0x1A &&
                    baslik[7] == 0x0A,

                ".webp" =>
                    okunanBayt >= 12 &&
                    baslik[0] == 0x52 &&
                    baslik[1] == 0x49 &&
                    baslik[2] == 0x46 &&
                    baslik[3] == 0x46 &&
                    baslik[8] == 0x57 &&
                    baslik[9] == 0x45 &&
                    baslik[10] == 0x42 &&
                    baslik[11] == 0x50,

                _ => false
            };
        }
    }
}
