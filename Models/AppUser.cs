using System.ComponentModel.DataAnnotations;
using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        public string? Email { get; set; }

        /// <summary>
        /// Düz metin şifre değil, ASP.NET Core PasswordHasher tarafından
        /// üretilen PBKDF2 hash değeri tutulur. Eski kolon adı veritabanı
        /// uyumluluğu için korunmuştur.
        /// </summary>
        public string? Password { get; set; }

        public string? Ad { get; set; }
        public string? Soyad { get; set; }

        // Sistem rolü: Admin / Kullanici
        // SQL Server indeks anahtarlarında nvarchar(max) kullanılamadığı için
        // rol alanının uzunluğu bilinçli olarak sınırlandırılır.
        [MaxLength(20)]
        public string? Role { get; set; } = "Kullanici";

        // Organizasyon bilgileri
        public int? DepartmanId { get; set; }
        public Departman? Departman { get; set; }

        public int? UnvanId { get; set; }
        public Unvan? Unvan { get; set; }

        public KidemSeviyesi KidemSeviyesi { get; set; }
            = KidemSeviyesi.Junior;

        public OrganizasyonRolu OrganizasyonRolu { get; set; }
            = OrganizasyonRolu.Calisan;

        // Doğrudan yönetici / ast ilişkisi
        public int? YoneticiId { get; set; }
        public AppUser? Yonetici { get; set; }

        public ICollection<AppUser> Astlar { get; set; }
            = new List<AppUser>();

        public bool AktifMi { get; set; } = true;

        // -----------------------------------------------------
        // HESAP GÜVENLİĞİ
        // -----------------------------------------------------

        /// <summary>
        /// Arka arkaya başarısız giriş denemelerinin sayısıdır.
        /// Başarılı girişte sıfırlanır.
        /// </summary>
        public int BasarisizGirisSayisi { get; set; }

        /// <summary>
        /// Hesap geçici olarak kilitliyse kilidin UTC bitiş zamanıdır.
        /// </summary>
        public DateTime? KilitBitisTarihi { get; set; }

        public DateTime? SonGirisTarihi { get; set; }
        public DateTime? SifreDegistirmeTarihi { get; set; }

        /// <summary>
        /// JWT içine de yazılır. Şifre, rol, e-posta veya hesap durumu
        /// değiştiğinde yenilenerek eski oturumların geçersiz olması sağlanır.
        /// </summary>
        [MaxLength(64)]
        public string GuvenlikDamgasi { get; set; }
            = Guid.NewGuid().ToString("N");

        public string TamAd => $"{Ad} {Soyad}".Trim();
    }
}
