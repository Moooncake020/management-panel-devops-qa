using System.ComponentModel.DataAnnotations;
using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.Models
{
    public class Gorev
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Baslik { get; set; }

        [Required]
        public string? Aciklama { get; set; }

        // Durum alanı performans indekslerinde kullanıldığı için nvarchar(max)
        // yerine sabit ve yeterli bir uzunlukta tutulur.
        [MaxLength(40)]
        public string Durum { get; set; } = "Açık";

        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;

        /// <summary>
        /// Görevin planlanan başlangıç günüdür. Zorunlu değildir; eski kayıtların
        /// ve esnek işlerin tarih vermeden kullanılabilmesini sağlar.
        /// </summary>
        public DateTime? BaslangicTarihi { get; set; }

        /// <summary>
        /// Görevin planlanan bitiş günüdür. Tamamlanmamış görevlerde bu tarih
        /// geçmişse görev gecikmiş kabul edilir.
        /// </summary>
        public DateTime? SonTarih { get; set; }

        /// <summary>
        /// Görevin gerçekten Tamamlandı durumuna geçtiği zamanı saklar.
        /// Dashboard üzerindeki haftalık tamamlanma metrikleri bu alandan üretilir.
        /// </summary>
        public DateTime? TamamlanmaTarihi { get; set; }

        // Eski alanlar: geçiş sürecinde korunuyor.
        public string? AtananUserId { get; set; }
        public string? AtananKullaniciAdi { get; set; }

        // Yeni ilişkisel alanlar
        public int? AtananKullaniciId { get; set; }
        public AppUser? AtananKullanici { get; set; }

        public int? OlusturanKullaniciId { get; set; }
        public AppUser? OlusturanKullanici { get; set; }

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public string? ResimYolu { get; set; }

        public ICollection<GorevYorum> Yorumlar { get; set; }
            = new List<GorevYorum>();

        public ICollection<GorevAktivite> Aktiviteler { get; set; }
            = new List<GorevAktivite>();
    }
}
