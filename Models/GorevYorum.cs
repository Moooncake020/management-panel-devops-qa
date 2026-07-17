using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.Models
{
    /// <summary>
    /// Göreve yazılan kullanıcı yorumunu temsil eder. Yorumlar fiziksel
    /// olarak silinmez; SilindiMi alanı ile geçmiş korunur.
    /// </summary>
    public class GorevYorum
    {
        [Key]
        public int Id { get; set; }

        public int GorevId { get; set; }
        public Gorev Gorev { get; set; } = null!;

        public int KullaniciId { get; set; }
        public AppUser Kullanici { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Icerik { get; set; } = string.Empty;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public DateTime? DuzenlenmeTarihi { get; set; }

        public bool SilindiMi { get; set; }

        public DateTime? SilinmeTarihi { get; set; }
    }
}
