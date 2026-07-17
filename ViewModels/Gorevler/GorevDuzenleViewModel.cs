using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Gorevler
{
    /// <summary>
    /// Görev düzenleme ekranının form ve ekran bilgilerini birlikte taşır.
    /// Controller, kullanıcının tam yetkisi yoksa içerik, öncelik ve tarih
    /// alanlarını dikkate almaz; yalnızca izinli durum değişikliğini işler.
    /// </summary>
    public class GorevDuzenleViewModel : IValidatableObject
    {
        [Required]
        public int Id { get; set; }

        [Display(Name = "Görev başlığı")]
        [Required(ErrorMessage = "Görev başlığı zorunludur.")]
        [StringLength(120, MinimumLength = 3,
            ErrorMessage = "Görev başlığı 3 ile 120 karakter arasında olmalıdır.")]
        public string Baslik { get; set; } = string.Empty;

        [Display(Name = "Görev açıklaması")]
        [Required(ErrorMessage = "Görev açıklaması zorunludur.")]
        [StringLength(2000, MinimumLength = 5,
            ErrorMessage = "Görev açıklaması 5 ile 2000 karakter arasında olmalıdır.")]
        public string Aciklama { get; set; } = string.Empty;

        [Display(Name = "Atanan kişi")]
        [Required(ErrorMessage = "Görevin atanacağı kullanıcıyı seçiniz.")]
        public int? AtananKullaniciId { get; set; }

        [Display(Name = "Görev durumu")]
        [Required(ErrorMessage = "Görev durumu zorunludur.")]
        public string Durum { get; set; } = "Açık";

        [Display(Name = "Öncelik")]
        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;

        [Display(Name = "Başlangıç tarihi")]
        [DataType(DataType.Date)]
        public DateTime? BaslangicTarihi { get; set; }

        [Display(Name = "Son tarih")]
        [DataType(DataType.Date)]
        public DateTime? SonTarih { get; set; }

        [Display(Name = "Yeni görev görseli")]
        [ValidateNever]
        public IFormFile? ResimDosyasi { get; set; }

        [Display(Name = "Mevcut görseli kaldır")]
        public bool MevcutResmiKaldir { get; set; }

        [ValidateNever]
        public bool TamDuzenlemeYetkisi { get; set; }

        [ValidateNever]
        public string AtananKisi { get; set; } = "Atanmamış";

        [ValidateNever]
        public string GoreviVeren { get; set; } = "Eski Kayıt";

        [ValidateNever]
        public string? MevcutResimYolu { get; set; }

        [ValidateNever]
        public List<GorevKullaniciSecenegiViewModel> Kullanicilar { get; set; }
            = new();

        [ValidateNever]
        public List<string> IzinliDurumlar { get; set; }
            = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.IsDefined(typeof(GorevOnceligi), Oncelik))
            {
                yield return new ValidationResult(
                    "Geçerli bir öncelik seviyesi seçiniz.",
                    new[] { nameof(Oncelik) }
                );
            }

            if (BaslangicTarihi.HasValue &&
                SonTarih.HasValue &&
                SonTarih.Value.Date < BaslangicTarihi.Value.Date)
            {
                yield return new ValidationResult(
                    "Son tarih, başlangıç tarihinden önce olamaz.",
                    new[] { nameof(SonTarih) }
                );
            }
        }
    }
}
