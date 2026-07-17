using Microsoft.EntityFrameworkCore;
using YonetimPaneli.Models;
using YonetimPaneli.Models.Enums;
using YonetimPaneli.ViewModels.Bildirimler;

namespace YonetimPaneli.Services
{
    /// <summary>
    /// Bildirim üretme, zaman uyarılarını tekilleştirme ve bildirimleri
    /// ekrana uygun ViewModel'e dönüştürme işlemlerini tek merkezde tutar.
    /// </summary>
    public class BildirimServisi
    {
        private readonly AppDbContext _context;

        public BildirimServisi(AppDbContext context)
        {
            _context = context;
        }

        public bool Olustur(
            int kullaniciId,
            string tur,
            string baslik,
            string mesaj,
            string? link = null,
            int? gorevId = null,
            string? tekilAnahtar = null)
        {
            if (kullaniciId <= 0 || string.IsNullOrWhiteSpace(baslik))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(tekilAnahtar))
            {
                var yereldeVarMi = _context.Bildirimler.Local
                    .Any(b => b.TekilAnahtar == tekilAnahtar);

                var veritabanindaVarMi = !yereldeVarMi && _context.Bildirimler
                    .AsNoTracking()
                    .Any(b => b.TekilAnahtar == tekilAnahtar);

                if (yereldeVarMi || veritabanindaVarMi)
                {
                    return false;
                }
            }

            var kullaniciAktifMi = _context.Kullanicilar
                .AsNoTracking()
                .Any(u => u.Id == kullaniciId && u.AktifMi);

            if (!kullaniciAktifMi)
            {
                return false;
            }

            _context.Bildirimler.Add(new Bildirim
            {
                KullaniciId = kullaniciId,
                GorevId = gorevId,
                Tur = tur.Trim(),
                Baslik = baslik.Trim(),
                Mesaj = mesaj.Trim(),
                Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
                TekilAnahtar = string.IsNullOrWhiteSpace(tekilAnahtar)
                    ? null
                    : tekilAnahtar.Trim(),
                OkunduMu = false,
                OlusturulmaTarihi = DateTime.Now
            });

            return true;
        }

        public void GorevKatilimcilarinaOlustur(
            Gorev gorev,
            int islemiYapanKullaniciId,
            string tur,
            string baslik,
            string mesaj,
            string? tekilAnahtarOnEki = null)
        {
            var aliciIdleri = new HashSet<int>();

            var atananKullaniciId = gorev.AtananKullaniciId;

            if (!atananKullaniciId.HasValue &&
                int.TryParse(gorev.AtananUserId, out var eskiAtananId))
            {
                atananKullaniciId = eskiAtananId;
            }

            if (atananKullaniciId.HasValue)
            {
                aliciIdleri.Add(atananKullaniciId.Value);
            }

            if (gorev.OlusturanKullaniciId.HasValue)
            {
                aliciIdleri.Add(gorev.OlusturanKullaniciId.Value);
            }

            aliciIdleri.Remove(islemiYapanKullaniciId);

            foreach (var aliciId in aliciIdleri)
            {
                var tekilAnahtar = string.IsNullOrWhiteSpace(tekilAnahtarOnEki)
                    ? null
                    : $"{tekilAnahtarOnEki}:{aliciId}";

                Olustur(
                    aliciId,
                    tur,
                    baslik,
                    mesaj,
                    $"/Gorev/Detay/{gorev.Id}",
                    gorev.Id,
                    tekilAnahtar
                );
            }
        }

        /// <summary>
        /// Kullanıcı menüyü açtığında görev tarihlerine göre eksik uyarıları
        /// oluşturur. TekilAnahtar sayesinde aynı görev için aynı uyarı ikinci
        /// kez üretilmez.
        /// </summary>
        public int ZamanUyarilariniOlustur(int kullaniciId, DateTime? simdi = null)
        {
            var tarih = (simdi ?? DateTime.Now).Date;
            var kullaniciIdMetni = kullaniciId.ToString();

            var adayGorevler = _context.Gorevler
                .AsNoTracking()
                .Where(g =>
                    g.Durum != "Tamamlandı" &&
                    g.SonTarih.HasValue &&
                    g.SonTarih.Value.Date <= tarih.AddDays(3) &&
                    (g.AtananKullaniciId == kullaniciId ||
                     g.AtananUserId == kullaniciIdMetni))
                .Select(g => new
                {
                    g.Id,
                    g.Baslik,
                    g.SonTarih
                })
                .ToList();

            var eklenenSayisi = 0;

            foreach (var gorev in adayGorevler)
            {
                var sonTarih = gorev.SonTarih!.Value.Date;
                var baslik = gorev.Baslik ?? "Başlıksız görev";

                string tur;
                string bildirimBasligi;
                string mesaj;
                string anahtar;

                if (sonTarih < tarih)
                {
                    var gecikenGun = (tarih - sonTarih).Days;
                    tur = BildirimTurleri.GorevGecikti;
                    bildirimBasligi = "Görev gecikti";
                    mesaj = $"'{baslik}' görevi {gecikenGun} gündür gecikmiş durumda.";
                    anahtar = $"gorev:{gorev.Id}:gecikti:{sonTarih:yyyyMMdd}";
                }
                else if (sonTarih == tarih)
                {
                    tur = BildirimTurleri.GorevBugunBitiyor;
                    bildirimBasligi = "Görev bugün bitiyor";
                    mesaj = $"'{baslik}' görevinin son tarihi bugün.";
                    anahtar = $"gorev:{gorev.Id}:bugun:{sonTarih:yyyyMMdd}";
                }
                else
                {
                    var kalanGun = (sonTarih - tarih).Days;
                    tur = BildirimTurleri.SonTarihYaklasiyor;
                    bildirimBasligi = "Son tarih yaklaşıyor";
                    mesaj = $"'{baslik}' görevinin bitmesine {kalanGun} gün kaldı.";
                    anahtar = $"gorev:{gorev.Id}:yaklasiyor:{sonTarih:yyyyMMdd}";
                }

                if (Olustur(
                        kullaniciId,
                        tur,
                        bildirimBasligi,
                        mesaj,
                        $"/Gorev/Detay/{gorev.Id}",
                        gorev.Id,
                        anahtar))
                {
                    eklenenSayisi++;
                }
            }

            return eklenenSayisi;
        }

        public int OkunmamisSayisi(int kullaniciId)
        {
            return _context.Bildirimler
                .AsNoTracking()
                .Count(b => b.KullaniciId == kullaniciId && !b.OkunduMu);
        }

        public List<BildirimListeItemViewModel> Listele(
            int kullaniciId,
            bool sadeceOkunmamis = false,
            int adet = 50)
        {
            var sorgu = _context.Bildirimler
                .AsNoTracking()
                .Where(b => b.KullaniciId == kullaniciId);

            if (sadeceOkunmamis)
            {
                sorgu = sorgu.Where(b => !b.OkunduMu);
            }

            var simdi = DateTime.Now;

            return sorgu
                .OrderByDescending(b => b.OlusturulmaTarihi)
                .Take(Math.Clamp(adet, 1, 100))
                .ToList()
                .Select(b => ViewModelOlustur(b, simdi))
                .ToList();
        }

        public BildirimListeItemViewModel ViewModelOlustur(
            Bildirim bildirim,
            DateTime? simdi = null)
        {
            var referansZamani = simdi ?? DateTime.Now;
            var (ikon, renk) = TurGorunumu(bildirim.Tur);
            var (kategoriKodu, kategoriMetni) = TurKategorisi(bildirim.Tur);

            return new BildirimListeItemViewModel
            {
                Id = bildirim.Id,
                Tur = bildirim.Tur,
                Baslik = bildirim.Baslik,
                Mesaj = bildirim.Mesaj,
                Link = bildirim.Link,
                OkunduMu = bildirim.OkunduMu,
                OlusturulmaTarihi = bildirim.OlusturulmaTarihi,
                GecenSureMetni = GecenSureMetni(
                    bildirim.OlusturulmaTarihi,
                    referansZamani),
                IkonKodu = ikon,
                RenkKodu = renk,
                KategoriKodu = kategoriKodu,
                KategoriMetni = kategoriMetni,
                TarihGrubu = TarihGrubu(
                    bildirim.OlusturulmaTarihi,
                    referansZamani)
            };
        }

        public static IReadOnlyCollection<string> KategoriTurleri(string kategori)
        {
            return kategori switch
            {
                "gorev" => new[]
                {
                    BildirimTurleri.GorevAtandi,
                    BildirimTurleri.GorevDevredildi,
                    BildirimTurleri.DurumDegisti
                },
                "yorum" => new[]
                {
                    BildirimTurleri.YorumEklendi
                },
                "tarih" => new[]
                {
                    BildirimTurleri.SonTarihYaklasiyor,
                    BildirimTurleri.GorevBugunBitiyor,
                    BildirimTurleri.GorevGecikti
                },
                _ => Array.Empty<string>()
            };
        }

        private static (string Ikon, string Renk) TurGorunumu(string tur)
        {
            return tur switch
            {
                BildirimTurleri.GorevAtandi => ("gorev", "teal"),
                BildirimTurleri.GorevDevredildi => ("devret", "amber"),
                BildirimTurleri.DurumDegisti => ("durum", "blue"),
                BildirimTurleri.YorumEklendi => ("yorum", "indigo"),
                BildirimTurleri.GorevGecikti => ("uyari", "red"),
                BildirimTurleri.GorevBugunBitiyor => ("takvim", "amber"),
                BildirimTurleri.SonTarihYaklasiyor => ("takvim", "purple"),
                _ => ("bilgi", "slate")
            };
        }


        private static (string Kod, string Metin) TurKategorisi(string tur)
        {
            if (KategoriTurleri("gorev").Contains(tur))
            {
                return ("gorev", "Görev");
            }

            if (KategoriTurleri("yorum").Contains(tur))
            {
                return ("yorum", "Yorum");
            }

            if (KategoriTurleri("tarih").Contains(tur))
            {
                return ("tarih", "Tarih uyarısı");
            }

            return ("diger", "Diğer");
        }

        private static string TarihGrubu(DateTime tarih, DateTime simdi)
        {
            var gunFarki = (simdi.Date - tarih.Date).Days;

            return gunFarki switch
            {
                <= 0 => "Bugün",
                1 => "Dün",
                <= 7 => "Bu hafta",
                _ => "Daha önce"
            };
        }

        private static string GecenSureMetni(DateTime tarih, DateTime simdi)
        {
            var fark = simdi - tarih;

            if (fark.TotalMinutes < 1)
            {
                return "Şimdi";
            }

            if (fark.TotalHours < 1)
            {
                return $"{Math.Max(1, (int)fark.TotalMinutes)} dk önce";
            }

            if (fark.TotalDays < 1)
            {
                return $"{Math.Max(1, (int)fark.TotalHours)} sa önce";
            }

            if (fark.TotalDays < 7)
            {
                return $"{Math.Max(1, (int)fark.TotalDays)} gün önce";
            }

            return tarih.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
