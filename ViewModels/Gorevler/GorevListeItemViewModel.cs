using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Gorevler
{
    /// <summary>
    /// Görev listesinin ihtiyaç duyduğu, ekrana özel sade veri modeli.
    /// Veritabanı modelini doğrudan View'a göndermek yerine yalnızca
    /// listede kullanılacak alanları taşır.
    /// </summary>
    public class GorevListeItemViewModel
    {
        public int Id { get; set; }

        public string Baslik { get; set; } = string.Empty;

        public string Aciklama { get; set; } = string.Empty;

        public string Durum { get; set; } = "Açık";

        /// <summary>
        /// JavaScript filtrelerinde Türkçe karakter ve boşluk problemi
        /// yaşamamak için kullanılan sabit durum kodudur.
        /// Örnek: "QA / Test Bekleyen" -> "qa".
        /// </summary>
        public string DurumKodu { get; set; } = "acik";

        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;

        public string OncelikMetni { get; set; } = "Normal";

        public string OncelikKodu { get; set; } = "normal";

        public DateTime? BaslangicTarihi { get; set; }

        public DateTime? SonTarih { get; set; }

        public DateTime? TamamlanmaTarihi { get; set; }

        public bool GeciktiMi { get; set; }

        public bool BugunBitiyorMu { get; set; }

        public bool YaklasanMi { get; set; }

        public string TarihDurumKodu { get; set; } = "tarihsiz";

        public string KalanGunMetni { get; set; } = "Son tarih yok";

        public int? AtananKullaniciId { get; set; }

        public string AtananKisi { get; set; } = "Atanmamış";

        public int? OlusturanKullaniciId { get; set; }

        public string GoreviVeren { get; set; } = "Eski Kayıt";

        public DateTime OlusturulmaTarihi { get; set; }

        public string? ResimYolu { get; set; }

        public bool SilebilirMi { get; set; }

        public bool KullaniciyaAtanmisMi { get; set; }

        public bool KullaniciTarafindanOlusturulmusMu { get; set; }

        public bool EkipGoreviMi { get; set; }

        public int YorumSayisi { get; set; }

        /// <summary>
        /// Bir görev birden fazla kapsama girebilir. Örneğin kullanıcı
        /// hem görevi oluşturmuş hem de kendisine atamış olabilir.
        /// Bu nedenle tek değer yerine boşlukla ayrılmış kodlar üretiriz.
        /// </summary>
        public string KapsamKodlari
        {
            get
            {
                var kodlar = new List<string>();

                if (KullaniciyaAtanmisMi)
                {
                    kodlar.Add("atanan");
                }

                if (KullaniciTarafindanOlusturulmusMu)
                {
                    kodlar.Add("olusturan");
                }

                if (EkipGoreviMi)
                {
                    kodlar.Add("ekip");
                }

                return string.Join(" ", kodlar);
            }
        }
    }
}
