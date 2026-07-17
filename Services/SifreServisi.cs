using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using YonetimPaneli.Models;

namespace YonetimPaneli.Services
{
    /// <summary>
    /// Kullanıcı şifrelerini tek bir noktadan hashler ve doğrular.
    /// ASP.NET Core PasswordHasher PBKDF2 tabanlı, salt içeren ve sürümlenebilir
    /// bir hash üretir. Eski düz metin kayıtlar yalnızca geçiş sürecinde
    /// doğrulanır ve ilk başarılı girişte yeniden hashlenir.
    /// </summary>
    public class SifreServisi
    {
        private readonly IPasswordHasher<AppUser> _passwordHasher;

        public SifreServisi(IPasswordHasher<AppUser> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        public string Hashle(AppUser kullanici, string sifre)
        {
            ArgumentNullException.ThrowIfNull(kullanici);

            if (string.IsNullOrWhiteSpace(sifre))
            {
                throw new ArgumentException(
                    "Şifre boş veya yalnızca boşluklardan oluşamaz.",
                    nameof(sifre));
            }

            return _passwordHasher.HashPassword(kullanici, sifre);
        }

        public PasswordVerificationResult Dogrula(
            AppUser kullanici,
            string girilenSifre)
        {
            ArgumentNullException.ThrowIfNull(kullanici);

            if (string.IsNullOrEmpty(kullanici.Password) ||
                string.IsNullOrEmpty(girilenSifre))
            {
                return PasswordVerificationResult.Failed;
            }

            if (!HashFormatindaMi(kullanici.Password))
            {
                return DuzMetinEslesiyorMu(
                        kullanici.Password,
                        girilenSifre)
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Failed;
            }

            try
            {
                return _passwordHasher.VerifyHashedPassword(
                    kullanici,
                    kullanici.Password,
                    girilenSifre);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        public bool HashFormatindaMi(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return false;
            }

            try
            {
                var veri = Convert.FromBase64String(deger);

                // ASP.NET Identity V2 = 0x00, V3 = 0x01 format işareti.
                return veri.Length > 0 &&
                       (veri[0] == 0x00 || veri[0] == 0x01);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public void GuvenlikDamgasiniYenile(AppUser kullanici)
        {
            ArgumentNullException.ThrowIfNull(kullanici);
            kullanici.GuvenlikDamgasi = Guid.NewGuid().ToString("N");
        }

        private static bool DuzMetinEslesiyorMu(
            string kayitliSifre,
            string girilenSifre)
        {
            var kayitliVeri = Encoding.UTF8.GetBytes(kayitliSifre);
            var girilenVeri = Encoding.UTF8.GetBytes(girilenSifre);

            if (kayitliVeri.Length != girilenVeri.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                kayitliVeri,
                girilenVeri);
        }
    }
}
