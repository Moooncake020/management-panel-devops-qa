using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.ViewModels.Organizasyon
{
    /// <summary>
    /// Departman ekleme ve düzenleme formunda izin verilen alanlar.
    /// </summary>
    public class DepartmanFormViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Departman adı")]
        [Required(ErrorMessage = "Departman adı zorunludur.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Departman adı 2 ile 100 karakter arasında olmalıdır.")]
        public string Ad { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        [StringLength(500,
            ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Aciklama { get; set; }

        [Display(Name = "Üst departman")]
        public int? UstDepartmanId { get; set; }
    }
}
