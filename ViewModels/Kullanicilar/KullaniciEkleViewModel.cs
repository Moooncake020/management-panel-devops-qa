using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.Validation;

namespace YonetimPaneli.ViewModels.Kullanicilar
{
    /// <summary>
    /// Yeni kullanıcı formundan alınmasına izin verilen alanlar.
    /// AppUser doğrudan forma bağlanmadığı için fazla alan gönderme (overposting)
    /// riski azaltılır.
    /// </summary>
    public class KullaniciEkleViewModel
    {
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

        [Display(Name = "Şifre")]
        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Şifre 8 ile 100 karakter arasında olmalıdır.")]
        [GucluSifre]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Şifre tekrarı")]
        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password),
            ErrorMessage = "Şifre ve şifre tekrarı aynı olmalıdır.")]
        public string PasswordTekrar { get; set; } = string.Empty;

        [Display(Name = "Departman")]
        public int? DepartmanId { get; set; }

        [Display(Name = "Unvan")]
        public int? UnvanId { get; set; }

        [Display(Name = "Kıdem seviyesi")]
        [EnumDataType(typeof(KidemSeviyesi),
            ErrorMessage = "Geçerli bir kıdem seviyesi seçiniz.")]
        public KidemSeviyesi KidemSeviyesi { get; set; }
            = KidemSeviyesi.Junior;

        [Display(Name = "Organizasyon rolü")]
        [EnumDataType(typeof(OrganizasyonRolu),
            ErrorMessage = "Geçerli bir organizasyon rolü seçiniz.")]
        public OrganizasyonRolu OrganizasyonRolu { get; set; }
            = OrganizasyonRolu.Calisan;

        [Display(Name = "Bağlı olduğu yönetici")]
        public int? YoneticiId { get; set; }

        [Display(Name = "Sistem rolü")]
        [Required(ErrorMessage = "Sistem rolü zorunludur.")]
        [RegularExpression("^(Admin|Kullanici)$",
            ErrorMessage = "Geçerli bir sistem rolü seçiniz.")]
        public string Role { get; set; } = "Kullanici";

        // Seçim listeleri formdan gönderilmez; controller tarafından hazırlanır.
        [ValidateNever]
        public KullaniciFormSecenekleriViewModel Secenekler { get; set; }
            = new();
    }
}
