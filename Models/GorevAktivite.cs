using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.Models
{
    /// <summary>
    /// Görev üzerinde gerçekleşen önemli değişiklikleri zaman çizelgesinde
    /// göstermek için saklar. Bu kayıtlar görev geçmişinin denetlenebilir
    /// ve geriye dönük okunabilir olmasını sağlar.
    /// </summary>
    public class GorevAktivite
    {
        [Key]
        public int Id { get; set; }

        public int GorevId { get; set; }
        public Gorev Gorev { get; set; } = null!;

        public int? KullaniciId { get; set; }
        public AppUser? Kullanici { get; set; }

        [Required]
        [MaxLength(80)]
        public string IslemTuru { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Aciklama { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? EskiDeger { get; set; }

        [MaxLength(500)]
        public string? YeniDeger { get; set; }

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
