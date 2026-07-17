using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.ViewModels.Organizasyon
{
    /// <summary>
    /// Unvan ekleme ve düzenleme formunda izin verilen alanlar.
    /// </summary>
    public class UnvanFormViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Unvan adı")]
        [Required(ErrorMessage = "Unvan adı zorunludur.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Unvan adı 2 ile 100 karakter arasında olmalıdır.")]
        public string Ad { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        [StringLength(500,
            ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Aciklama { get; set; }
    }
}
