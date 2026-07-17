using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.ViewModels.Auth
{
    public class LoginViewModel
    {
        [Display(Name = "E-posta adresi")]
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150,
            ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Şifre")]
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool BeniHatirla { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
