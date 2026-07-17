using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Validation;

namespace YonetimPaneli.ViewModels.Kullanicilar
{
    /// <summary>
    /// Kullanıcı düzenleme ekranının form modeli.
    /// Yeni şifre alanları boş bırakılırsa mevcut şifre korunur.
    /// </summary>
    public class KullaniciDuzenleViewModel
    {
        [Required]
        public int Id { get; set; }

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Ad 2 ile 50 karakter arasında olmalıdır.")]
        public string Ad { get; set; } = string.Empty;

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Soyad 2 ile 50 karakter arasında olmalıdır.")]
        public string Soyad { get; set; } = string.Empty;

        [Display(Name = "E-posta adresi")]
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150,
            ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Yeni şifre")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Yeni şifre 8 ile 100 karakter arasında olmalıdır.")]
        [GucluSifre]
        [DataType(DataType.Password)]
        public string? YeniPassword { get; set; }

        [Display(Name = "Yeni şifre tekrarı")]
        [DataType(DataType.Password)]
        [Compare(nameof(YeniPassword),
            ErrorMessage = "Yeni şifre ve şifre tekrarı aynı olmalıdır.")]
        public string? YeniPasswordTekrar { get; set; }

        [Display(Name = "Departman")]
        public int? DepartmanId { get; set; }

        [Display(Name = "Unvan")]
        public int? UnvanId { get; set; }

        [Display(Name = "Kıdem seviyesi")]
        [EnumDataType(typeof(KidemSeviyesi),
            ErrorMessage = "Geçerli bir kıdem seviyesi seçiniz.")]
        public KidemSeviyesi KidemSeviyesi { get; set; }

        [Display(Name = "Organizasyon rolü")]
        [EnumDataType(typeof(OrganizasyonRolu),
            ErrorMessage = "Geçerli bir organizasyon rolü seçiniz.")]
        public OrganizasyonRolu OrganizasyonRolu { get; set; }

        [Display(Name = "Bağlı olduğu yönetici")]
        public int? YoneticiId { get; set; }

        [Display(Name = "Sistem rolü")]
        [Required(ErrorMessage = "Sistem rolü zorunludur.")]
        [RegularExpression("^(Admin|Kullanici)$",
            ErrorMessage = "Geçerli bir sistem rolü seçiniz.")]
        public string Role { get; set; } = "Kullanici";

        [Display(Name = "Hesap durumu")]
        public bool AktifMi { get; set; }

        [ValidateNever]
        public bool KendiHesabiMi { get; set; }

        [ValidateNever]
        public bool HesapKilitliMi { get; set; }

        [ValidateNever]
        public DateTime? KilitBitisTarihi { get; set; }

        [ValidateNever]
        public int BasarisizGirisSayisi { get; set; }

        [ValidateNever]
        public KullaniciFormSecenekleriViewModel Secenekler { get; set; }
            = new();

        public string TamAd => $"{Ad} {Soyad}".Trim();
    }
}
