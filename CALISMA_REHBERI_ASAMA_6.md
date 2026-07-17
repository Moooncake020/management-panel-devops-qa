# Aşama 6 — Görev Önceliği, Tarih Planı ve Dashboard

Bu aşamada görevler yalnızca başlık, açıklama ve durumdan oluşan kayıtlar olmaktan çıkarıldı. Her göreve iş önceliği, planlanan başlangıç tarihi, son tarih ve gerçek tamamlanma zamanı eklendi. Bu alanlar görev listesi filtrelerini, gecikme uyarılarını ve anasayfa metriklerini besliyor.

> Bu dosya, projeyi incelerken hangi kodun neden yazıldığını anlayabilmen için hazırlanmıştır.

---

## 1. Bu aşamada eklenen ana özellikler

- Düşük, Normal, Yüksek ve Kritik görev öncelikleri
- Planlanan başlangıç tarihi
- Son tarih
- Gerçek tamamlanma zamanı
- Geciken görev hesabı
- Bugün biten görev hesabı
- Üç gün içinde bitecek görev hesabı
- Görev listesinde öncelik ve tarih filtreleri
- Önceliğe ve son tarihe göre sıralama
- Görev detayında planlama bilgileri
- Öncelik ve tarih değişikliklerinin aktivite geçmişine yazılması
- Dashboard üzerinde görev durumu, gecikme ve kritik iş metrikleri
- Dashboard kartlarından hazır filtreli görev listesine geçiş
- URL üzerinde korunabilen görev filtreleri

---

## 2. Değiştirilen temel dosyalar

### Veritabanı ve modeller

```text
Models/Enums/GorevOnceligi.cs
Models/Gorev.cs
Models/AppDbContext.cs
Migrations/20260714090000_GorevPlanlamaAlanlariEkle.cs
Migrations/20260714090000_GorevPlanlamaAlanlariEkle.Designer.cs
Migrations/AppDbContextModelSnapshot.cs
```

### Servis

```text
Services/GorevZamanServisi.cs
```

### Controller

```text
Controllers/GorevController.cs
Controllers/HomeController.cs
```

### ViewModel

```text
ViewModels/Gorevler/GorevEkleViewModel.cs
ViewModels/Gorevler/GorevDuzenleViewModel.cs
ViewModels/Gorevler/GorevListeItemViewModel.cs
ViewModels/Gorevler/GorevIndexViewModel.cs
ViewModels/Gorevler/GorevDetayViewModel.cs
ViewModels/Home/HomeGorevOzetViewModel.cs
ViewModels/Home/HomeIndexViewModel.cs
```

### View

```text
Views/Gorev/Ekle.cshtml
Views/Gorev/Duzenle.cshtml
Views/Gorev/Detay.cshtml
Views/Gorev/Index.cshtml
Views/Gorev/_GorevKartlari.cshtml
Views/Gorev/_GorevTablosu.cshtml
Views/Shared/_GorevOncelikRozeti.cshtml
Views/Home/Index.cshtml
```

---

## 3. `GorevOnceligi` enum mantığı

Dosya:

```text
Models/Enums/GorevOnceligi.cs
```

Enum değerleri:

```csharp
public enum GorevOnceligi
{
    Dusuk = 10,
    Normal = 20,
    Yuksek = 30,
    Kritik = 40
}
```

Burada string yerine enum kullanmamızın nedenleri:

1. Yazım hatalarını engeller.
2. Formdan geçersiz değer gönderilmesini kontrol etmeyi kolaylaştırır.
3. Sıralama yapılabilir.
4. Veritabanında küçük ve düzenli bir sayısal değer tutulur.
5. Yeni bir öncelik seviyesi ileride kolayca eklenebilir.

Değerlerin `10, 20, 30, 40` şeklinde aralıklı olması bilinçlidir. İleride örneğin `Acil = 35` gibi yeni bir seviye eklemek gerekirse mevcut değerleri değiştirmek zorunda kalmayız.

---

## 4. Görev modeline eklenen alanlar

Dosya:

```text
Models/Gorev.cs
```

Yeni alanlar:

```csharp
public GorevOnceligi Oncelik { get; set; }
public DateTime? BaslangicTarihi { get; set; }
public DateTime? SonTarih { get; set; }
public DateTime? TamamlanmaTarihi { get; set; }
```

### `BaslangicTarihi`

Görevin planlanan başlangıç günüdür. Nullable olmasının nedeni eski görevlerin ve tarihsiz işlerin çalışmaya devam etmesidir.

### `SonTarih`

Görevin planlanan bitiş günüdür. Görev tamamlanmamışsa ve bu tarih bugünden küçükse görev gecikmiş sayılır.

### `TamamlanmaTarihi`

Görevin gerçekten ne zaman tamamlandığını tutar. `SonTarih` planlanan tarihi, `TamamlanmaTarihi` ise gerçekleşen zamanı ifade eder.

Örnek:

```text
Son tarih:          20.07.2026
Tamamlanma tarihi:  18.07.2026 15:42
```

Bu görev iki gün erken tamamlanmıştır.

---

## 5. Neden ayrı bir `GorevZamanServisi` oluşturuldu?

Dosya:

```text
Services/GorevZamanServisi.cs
```

Gecikme hesabını controller, dashboard, görev kartı ve detay ekranında ayrı ayrı yazsaydık aynı iş kuralı birçok yere dağılırdı.

Serviste toplanan temel işlemler:

```csharp
GeciktiMi(...)
BugunBitiyorMu(...)
YaklasanMi(...)
KalanGunMetni(...)
OncelikMetni(...)
OncelikKodu(...)
TarihDurumKodu(...)
```

### Gecikme kuralı

Bir görev şu koşullarda gecikmiştir:

```text
Son tarih var
VE görev tamamlanmamış
VE son tarih bugünden önce
```

Tamamlanan görevler son tarihleri geçmiş olsa bile aktif gecikmiş görev sayılmaz.

### Tarihleri neden `.Date` ile karşılaştırıyoruz?

```csharp
sonTarih.Value.Date < DateTime.Now.Date
```

Saat bilgisi dikkate alınmaz. Örneğin bugün saat 18.00'de son tarihi bugün olan görev, gün bitmeden gecikmiş sayılmaz.

### Referans tarihi parametresi

Servis metotlarında isteğe bağlı `referansTarihi` vardır. Bunun iki yararı bulunur:

1. Aynı sayfa oluşturulurken bütün görevler aynı an üzerinden hesaplanır.
2. Unit test yazarken bugünün tarihini kontrol edebiliriz.

Örnek test mantığı:

```csharp
var referans = new DateTime(2026, 7, 20);
var sonuc = servis.GeciktiMi(
    new DateTime(2026, 7, 19),
    "Açık",
    referans
);
```

Sonuç `true` olmalıdır.

---

## 6. Formlarda tarih doğrulaması

Dosyalar:

```text
ViewModels/Gorevler/GorevEkleViewModel.cs
ViewModels/Gorevler/GorevDuzenleViewModel.cs
```

Her iki ViewModel de `IValidatableObject` uygular.

Neden yalnızca Data Annotation yeterli değil?

`[Required]` veya `[StringLength]` tek bir alanı kontrol eder. Burada iki alanı birbiriyle karşılaştırmamız gerekir:

```text
SonTarih >= BaslangicTarihi
```

Bu nedenle:

```csharp
public IEnumerable<ValidationResult> Validate(
    ValidationContext validationContext)
```

kullanılır.

Kural:

```csharp
if (BaslangicTarihi.HasValue &&
    SonTarih.HasValue &&
    SonTarih.Value.Date < BaslangicTarihi.Value.Date)
{
    yield return new ValidationResult(
        "Son tarih, başlangıç tarihinden önce olamaz.",
        new[] { nameof(SonTarih) }
    );
}
```

Hata `SonTarih` alanına bağlandığı için mesaj doğru inputun altında görünür.

---

## 7. Tamamlanma tarihinin yönetilmesi

Dosya:

```text
Controllers/GorevController.cs
```

Görev ilk kez `Tamamlandı` durumuna geçerse:

```csharp
gorev.TamamlanmaTarihi = DateTime.Now;
```

Tamamlanan görev yeniden açılırsa:

```csharp
gorev.TamamlanmaTarihi = null;
```

Bu alanı formdan kabul etmiyoruz. Kullanıcı tarayıcı üzerinden sahte bir tamamlanma tarihi gönderemez; değer yalnızca controller iş kuralıyla atanır.

---

## 8. Çalışanın yetkisi neden korunuyor?

Atanan çalışan yalnızca durum değiştirebilir. Kullanıcı manuel HTTP isteğiyle öncelik veya tarih göndermeye çalışsa bile controller bu değerleri veritabanındaki eski değerlerle değiştirir:

```csharp
model.Oncelik = gorev.Oncelik;
model.BaslangicTarihi = gorev.BaslangicTarihi;
model.SonTarih = gorev.SonTarih;
```

Bu önemli bir güvenlik ilkesidir:

> Bir alanı arayüzde gizlemek yetki kontrolü değildir. Sunucu gelen değeri ayrıca kontrol etmelidir.

---

## 9. Öncelik ve tarih aktivite kayıtları

Yeni aktivite türleri:

```csharp
GorevAktiviteTurleri.OncelikDegisti
GorevAktiviteTurleri.TarihPlaniDegisti
```

Öncelik değiştiğinde geçmişte şu bilgiler saklanır:

```text
Eski değer: Normal
Yeni değer: Kritik
```

Tarih planı değiştiğinde:

```text
Eski: 14.07.2026 → 20.07.2026
Yeni: 15.07.2026 → 18.07.2026
```

Görev detayındaki zaman çizelgesi bu kayıtları gösterir.

---

## 10. Görev listesi filtre mantığı

Görev kartı ve tablo satırlarına `data-*` alanları yazılır:

```html
data-priority="kritik"
data-date-status="geciken"
data-due="2026-07-20T00:00:00"
data-status="devam"
```

JavaScript bu verileri kullanarak sayfayı yeniden yüklemeden filtreleme ve sıralama yapar.

### Tarih durum kodları

```text
geciken     Son tarihi geçti
bugun      Bugün bitiyor
yaklasan    Üç gün içinde bitiyor
planli      Daha ileri bir tarihte bitiyor
tarihsiz    Son tarih girilmedi
tamamlandi  Görev tamamlandı
```


### URL üzerinden filtre

Filtreler URL üzerinde de saklanır:

```text
/Gorev/Index?tarih=geciken
/Gorev/Index?durum=aktif&oncelik=kritik
```

Böylece:

- Dashboard kartları görev sayfasını hazır filtreyle açabilir.
- Filtrelenmiş görünüm paylaşılabilir.
- Sayfa yenilendiğinde filtre kaybolmaz.

Bunu sağlayan iki JavaScript metodu:

```javascript
loadFiltersFromUrl()
updateUrlFromFilters()
```

---

## 11. Dashboard ViewModel yaklaşımı

Dosyalar:

```text
ViewModels/Home/HomeIndexViewModel.cs
ViewModels/Home/HomeGorevOzetViewModel.cs
Controllers/HomeController.cs
```

View içinde veritabanı sorgusu yapılmaz. Controller yetkiye göre görevleri toplar, metrikleri hesaplar ve ViewModel'e koyar.

Metrikler:

```text
Toplam görev
Açık görev
Devam eden
QA / Test bekleyen
Bug / Hata
Tamamlanan
Geciken
Bugün biten
Kritik aktif
Bu hafta tamamlanan
Tamamlanma oranı
```

### Haftanın ilk günü hesabı

Haftanın başlangıcı pazartesi kabul edilir:

```csharp
var haftaninIlkGunu = bugun.AddDays(
    -(((int)bugun.DayOfWeek + 6) % 7)
);
```

Bu hafta tamamlanan görevler `TamamlanmaTarihi` üzerinden hesaplanır.

---

## 12. Veritabanı indeksleri

Dosya:

```text
Models/AppDbContext.cs
```

Eklenen indeksler:

```csharp
modelBuilder.Entity<Gorev>().HasIndex(g => g.SonTarih);
modelBuilder.Entity<Gorev>().HasIndex(g => g.Oncelik);
```

İndeks, veritabanının sık kullanılan filtrelerde tüm tabloyu satır satır taramasını azaltabilir.

Bu aşamada özellikle şu sorgular sık yapılır:

```text
Son tarihi geçen görevler
Kritik görevler
Son tarihe göre sıralama
Önceliğe göre sıralama
```

İndeks her zaman otomatik hızlanma garantisi değildir; ancak görev sayısı arttığında bu alanlar için doğru bir başlangıçtır.

---

## 13. Migration ne yapıyor?

Migration:

```text
20260714090000_GorevPlanlamaAlanlariEkle
```

Eklenen sütunlar:

```text
BaslangicTarihi
Oncelik
SonTarih
TamamlanmaTarihi
```

Eski görevler için:

- Öncelik `Normal` değerine karşılık gelen `20` olarak oluşturulur.
- Başlangıç tarihi, görev oluşturulma tarihinden alınır.
- Son tarih boş bırakılır.
- Tamamlanma tarihi boş bırakılır; geçmiş görevlerin gerçek tamamlanma zamanı bilinmediği için sahte tarih üretilmez.

---

## 14. Çalıştırma sırası

Bu aşamada veritabanı şeması değiştiği için migration uygulanmalıdır.

Önce uygulamayı durdur:

```text
Shift + F5
```

Ardından:

```powershell
dotnet build
```

Package Manager Console:

```powershell
Update-Database
```

Sonra:

```powershell
dotnet run
```

---

## 15. Test senaryoları

### Senaryo 1 — Tarih doğrulaması

```text
Başlangıç: 20.07.2026
Son tarih: 19.07.2026
```

Beklenen: Form kaydedilmemeli ve son tarih alanında hata görünmeli.

### Senaryo 2 — Gecikmiş görev

Son tarihi dünden önce olan, tamamlanmamış bir görev oluştur.

Beklenen:

- Görev listesinde kırmızı uyarı
- Geciken özet sayısında artış
- Dashboard dikkat listesinde görünme

### Senaryo 3 — Bugün biten

Son tarihi bugün olan görev oluştur.

Beklenen:

- `Bugün bitiyor` metni
- Dashboard `Bugün Biten` sayısında artış

### Senaryo 4 — Kritik aktif

Kritik öncelikli tamamlanmamış görev oluştur.

Beklenen:

- Kırmızı kritik rozeti
- Kritik aktif metrik artışı
- Dashboard kartına basınca görev listesi `aktif + kritik` filtreleriyle açılmalı

### Senaryo 5 — Tamamlama zamanı

Görevi izinli akış üzerinden `Tamamlandı` yap.

Beklenen:

- `TamamlanmaTarihi` dolmalı
- Görev artık geciken sayılmamalı
- Detay ekranında tamamlanma tarihi görünmeli

Görevi yeniden aç:

Beklenen:

- `TamamlanmaTarihi` temizlenmeli

### Senaryo 6 — Çalışan yetkisi

Çalışan hesabıyla görevi düzenle.

Beklenen:

- Öncelik ve tarih alanları salt okunur görünmeli
- Manuel POST isteği gönderilse bile öncelik/tarih değişmemeli
- Yalnızca izinli durum geçişi yapılabilmeli

### Senaryo 7 — Aktivite geçmişi

Görev önceliğini ve son tarihini değiştir.

Beklenen:

- Öncelik değişimi aktivitesi
- Tarih planı değişimi aktivitesi
- Eski ve yeni değerler

### Senaryo 8 — URL filtreleri

Tarayıcıda aç:

```text
/Gorev/Index?tarih=geciken
```

Beklenen: Geciken filtresi otomatik seçilmeli.

---

## 16. Bu aşamada öğrenmen gereken temel kavramlar

1. Enum ile sabit iş değerlerini yönetmek
2. Nullable tarih alanları
3. Planlanan tarih ile gerçekleşen tarihi ayırmak
4. `IValidatableObject` ile alanlar arası doğrulama
5. İş kuralını servis içinde merkezileştirmek
6. UI gizleme ile backend yetkisinin farklı şeyler olması
7. ViewModel ile dashboard hazırlamak
8. `data-*` alanlarıyla client-side filtreleme
9. URL query string ile filtre durumunu korumak
10. Veritabanı indeksi ve migration mantığı

---

## 17. Sonraki aşama önerisi

Bir sonraki mantıklı geliştirme:

```text
Aşama 7 — Organizasyon şeması ve departman/unvan yönetimi
```

Bu aşamada mevcut yönetici–ast ilişkileri görsel ağaç hâline getirilebilir; departman ve unvanlar ayrı yönetim ekranlarından eklenip düzenlenebilir.
