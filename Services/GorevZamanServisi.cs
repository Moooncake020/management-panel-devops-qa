using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.Services
{
    /// <summary>
    /// Son tarih, gecikme ve öncelik gibi ekranlarda tekrar kullanılan
    /// görev zaman hesaplarını tek yerde toplar. Hesaplamalarda yalnızca
    /// tarih kısmı dikkate alınır; böylece saat farkları sonucu değiştirmez.
    /// </summary>
    public class GorevZamanServisi
    {
        public bool GeciktiMi(DateTime? sonTarih, string? durum, DateTime? referansTarihi = null)
        {
            if (!sonTarih.HasValue || DurumTamamlandiMi(durum))
            {
                return false;
            }

            var bugun = (referansTarihi ?? DateTime.Now).Date;
            return sonTarih.Value.Date < bugun;
        }

        public bool BugunBitiyorMu(DateTime? sonTarih, string? durum, DateTime? referansTarihi = null)
        {
            if (!sonTarih.HasValue || DurumTamamlandiMi(durum))
            {
                return false;
            }

            var bugun = (referansTarihi ?? DateTime.Now).Date;
            return sonTarih.Value.Date == bugun;
        }

        public bool YaklasanMi(DateTime? sonTarih, string? durum, int gun = 3, DateTime? referansTarihi = null)
        {
            if (!sonTarih.HasValue || DurumTamamlandiMi(durum))
            {
                return false;
            }

            var bugun = (referansTarihi ?? DateTime.Now).Date;
            var tarih = sonTarih.Value.Date;

            return tarih > bugun && tarih <= bugun.AddDays(gun);
        }

        public string KalanGunMetni(DateTime? sonTarih, string? durum, DateTime? referansTarihi = null)
        {
            if (!sonTarih.HasValue)
            {
                return "Son tarih yok";
            }

            if (DurumTamamlandiMi(durum))
            {
                return "Tamamlandı";
            }

            var bugun = (referansTarihi ?? DateTime.Now).Date;
            var gunFarki = (sonTarih.Value.Date - bugun).Days;

            return gunFarki switch
            {
                < 0 => $"{Math.Abs(gunFarki)} gün gecikti",
                0 => "Bugün bitiyor",
                1 => "Yarın bitiyor",
                _ => $"{gunFarki} gün kaldı"
            };
        }

        public string OncelikMetni(GorevOnceligi oncelik)
        {
            return oncelik switch
            {
                GorevOnceligi.Dusuk => "Düşük",
                GorevOnceligi.Normal => "Normal",
                GorevOnceligi.Yuksek => "Yüksek",
                GorevOnceligi.Kritik => "Kritik",
                _ => "Normal"
            };
        }

        public string OncelikKodu(GorevOnceligi oncelik)
        {
            return oncelik switch
            {
                GorevOnceligi.Dusuk => "dusuk",
                GorevOnceligi.Normal => "normal",
                GorevOnceligi.Yuksek => "yuksek",
                GorevOnceligi.Kritik => "kritik",
                _ => "normal"
            };
        }

        public string TarihDurumKodu(DateTime? sonTarih, string? durum, DateTime? referansTarihi = null)
        {
            // Tamamlanan görevleri planlı/geciken filtrelerine tekrar sokmuyoruz.
            if (DurumTamamlandiMi(durum))
            {
                return "tamamlandi";
            }

            if (GeciktiMi(sonTarih, durum, referansTarihi))
            {
                return "geciken";
            }

            if (BugunBitiyorMu(sonTarih, durum, referansTarihi))
            {
                return "bugun";
            }

            if (YaklasanMi(sonTarih, durum, 3, referansTarihi))
            {
                return "yaklasan";
            }

            return sonTarih.HasValue ? "planli" : "tarihsiz";
        }

        private static bool DurumTamamlandiMi(string? durum)
        {
            return string.Equals(
                durum,
                "Tamamlandı",
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
