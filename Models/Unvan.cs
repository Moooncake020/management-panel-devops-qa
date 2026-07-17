using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.Models
{
    public class Unvan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Aciklama { get; set; }

        public ICollection<AppUser> Kullanicilar { get; set; }
            = new List<AppUser>();
    }
}
