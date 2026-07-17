using System.ComponentModel.DataAnnotations;
using YonetimPaneli.Validation;

namespace YonetimPaneli.ViewModels.Auth
{
    public class SifreDegistirViewModel
    {
        [Display(Name = "Mevcut şifre")]
        [Required(ErrorMessage = "Mevcut şifrenizi giriniz.")]
        [DataType(DataType.Password)]
        public string MevcutSifre { get; set; } = string.Empty;

        [Display(Name = "Yeni şifre")]
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Yeni şifre 8 ile 100 karakter arasında olmalıdır.")]
        [GucluSifre]
        [DataType(DataType.Password)]
        public string YeniSifre { get; set; } = string.Empty;

        [Display(Name = "Yeni şifre tekrarı")]
        [Required(ErrorMessage = "Yeni şifre tekrarını giriniz.")]
        [DataType(DataType.Password)]
        [Compare(nameof(YeniSifre),
            ErrorMessage = "Yeni şifre ve şifre tekrarı aynı olmalıdır.")]
        public string YeniSifreTekrar { get; set; } = string.Empty;
    }
}
