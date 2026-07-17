using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.Models
{
    public class Departman
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Aciklama { get; set; }

        public int? UstDepartmanId { get; set; }
        public Departman? UstDepartman { get; set; }

        public ICollection<Departman> AltDepartmanlar { get; set; }
            = new List<Departman>();

        public ICollection<AppUser> Kullanicilar { get; set; }
            = new List<AppUser>();
    }
}
