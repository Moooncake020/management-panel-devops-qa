using YonetimPaneli.Models;

namespace YonetimPaneli.Services
{
    /// <summary>
    /// Controller'ların aktivite kaydı oluştururken aynı kodu tekrar etmesini
    /// önler. Bu servis yalnızca DbContext'e kayıt ekler; transaction ve
    /// SaveChanges çağrısı işlemi başlatan controller tarafından yönetilir.
    /// Böylece görev değişikliği ile geçmiş kaydı aynı veritabanı işleminde
    /// birlikte kaydedilir.
    /// </summary>
    public class GorevAktiviteServisi
    {
        private readonly AppDbContext _context;

        public GorevAktiviteServisi(AppDbContext context)
        {
            _context = context;
        }

        public GorevAktivite Ekle(
            int gorevId,
            int? kullaniciId,
            string islemTuru,
            string aciklama,
            string? eskiDeger = null,
            string? yeniDeger = null)
        {
            var aktivite = AktiviteOlustur(
                kullaniciId,
                islemTuru,
                aciklama,
                eskiDeger,
                yeniDeger
            );

            aktivite.GorevId = gorevId;
            _context.GorevAktiviteleri.Add(aktivite);

            return aktivite;
        }

        public GorevAktivite Ekle(
            Gorev gorev,
            int? kullaniciId,
            string islemTuru,
            string aciklama,
            string? eskiDeger = null,
            string? yeniDeger = null)
        {
            var aktivite = AktiviteOlustur(
                kullaniciId,
                islemTuru,
                aciklama,
                eskiDeger,
                yeniDeger
            );

            aktivite.Gorev = gorev;
            _context.GorevAktiviteleri.Add(aktivite);

            return aktivite;
        }

        private static GorevAktivite AktiviteOlustur(
            int? kullaniciId,
            string islemTuru,
            string aciklama,
            string? eskiDeger,
            string? yeniDeger)
        {
            return new GorevAktivite
            {
                KullaniciId = kullaniciId,
                IslemTuru = ZorunluMetniSinirla(islemTuru, 80),
                Aciklama = ZorunluMetniSinirla(aciklama, 1000),
                EskiDeger = OpsiyonelMetniSinirla(eskiDeger, 500),
                YeniDeger = OpsiyonelMetniSinirla(yeniDeger, 500),
                OlusturulmaTarihi = DateTime.Now
            };
        }

        private static string ZorunluMetniSinirla(string metin, int uzunluk)
        {
            var temizMetin = metin.Trim();
            return temizMetin.Length <= uzunluk
                ? temizMetin
                : temizMetin[..uzunluk];
        }

        private static string? OpsiyonelMetniSinirla(string? metin, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            return ZorunluMetniSinirla(metin, uzunluk);
        }
    }
}
