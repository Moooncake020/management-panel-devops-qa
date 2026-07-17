using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.Validation
{
    /// <summary>
    /// Formlarda ortak kullanılan temel şifre politikasını uygular.
    /// Alan zorunluluğu ayrıca Required ile belirlenir; bu attribute boş
    /// değerleri geçerli kabul ederek isteğe bağlı yeni şifre alanlarında da
    /// kullanılabilir.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class GucluSifreAttribute : ValidationAttribute
    {
        public GucluSifreAttribute()
        {
            ErrorMessage =
                "Şifre en az 8 karakter olmalı; büyük harf, küçük harf ve rakam içermelidir.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return true;
            }

            var sifre = value.ToString();

            if (string.IsNullOrEmpty(sifre))
            {
                return true;
            }

            return sifre.Length >= 8 &&
                   sifre.Length <= 100 &&
                   sifre.Any(char.IsUpper) &&
                   sifre.Any(char.IsLower) &&
                   sifre.Any(char.IsDigit) &&
                   !sifre.Any(char.IsWhiteSpace);
        }
    }
}
