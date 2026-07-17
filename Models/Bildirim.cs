using System.ComponentModel.DataAnnotations;

namespace YonetimPaneli.Models
{
    /// <summary>
    /// Kullanıcının uygulama içindeki önemli gelişmeleri daha sonra da
    /// görebilmesini sağlayan kalıcı bildirim kaydıdır.
    /// </summary>
    public class Bildirim
    {
        [Key]
        public int Id { get; set; }

        public int KullaniciId { get; set; }
        public AppUser Kullanici { get; set; } = null!;

        public int? GorevId { get; set; }
        public Gorev? Gorev { get; set; }

        [Required]
        [MaxLength(60)]
        public string Tur { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string Baslik { get; set; } = string.Empty;

        [Required]
        [MaxLength(600)]
        public string Mesaj { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Link { get; set; }

        /// <summary>
        /// Son tarih uyarıları gibi aynı olayın tekrar tekrar üretilmesini
        /// engellemek için kullanılan uygulama içi benzersiz anahtardır.
        /// </summary>
        [MaxLength(180)]
        public string? TekilAnahtar { get; set; }

        public bool OkunduMu { get; set; }

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public DateTime? OkunmaTarihi { get; set; }
    }
}
