using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.ViewModels.Gorevler
{
    public class GorevYorumEkleViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir görev seçilmelidir.")]
        public int GorevId { get; set; }

        [Required(ErrorMessage = "Yorum metni boş bırakılamaz.")]
        [StringLength(
            2000,
            MinimumLength = 2,
            ErrorMessage = "Yorum 2 ile 2000 karakter arasında olmalıdır.")]
        [Display(Name = "Yorumunuz")]
        public string Icerik { get; set; } = string.Empty;
    }
}
