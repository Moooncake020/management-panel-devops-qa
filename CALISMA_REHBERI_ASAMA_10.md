# AŞAMA 10 — Performans, Sayfalama, Hata Yönetimi ve Test Altyapısı

Bu aşama temel geliştirme planının son aşamasıdır. Amaç uygulamaya yeni bir büyük modül eklemekten çok, önceki dokuz aşamada oluşan sistemi daha büyük veriyle çalışabilecek, hataları daha anlaşılır gösterecek ve kod değişiklikleri test edilebilecek hale getirmektir.

## 1. Sunucu tarafı sayfalama neden eklendi?

Önceki sürüm görevleri ve kullanıcıları veritabanından tamamen alıyor, ardından tarayıcıda JavaScript ile filtreliyordu. 20 kayıt varken sorun olmaz; 5.000 görev olduğunda ise:

- SQL Server gereksiz kayıtları gönderir.
- Sunucu gereksiz nesneler oluşturur.
- HTML çok büyür.
- Tarayıcı bütün kayıtları filtrelemeye çalışır.

Aşama 10'da filtre, sıralama ve sayfalama SQL sorgusuna taşındı. Controller sırası şöyledir:

1. Kullanıcının görebileceği kayıtları belirle.
2. Arama ve filtreleri `IQueryable` üzerine ekle.
3. `Count()` ile toplam eşleşmeyi hesapla.
4. `Skip()` ve `Take()` ile yalnızca istenen sayfayı getir.
5. ViewModel'e dönüştür.

Örnek:

```csharp
var kayitlar = await sorgu
    .OrderByDescending(g => g.OlusturulmaTarihi)
    .Skip((sayfa - 1) * sayfaBoyutu)
    .Take(sayfaBoyutu)
    .ToListAsync(cancellationToken);
```

Bu yapı şu ekranlarda kullanılır:

- `Gorev/Index`
- `Kullanicilar/Index`
- `Bildirim/Index`

## 2. Ortak sayfalama bileşeni

Dosyalar:

```text
ViewModels/Ortak/SayfalamaViewModel.cs
Views/Shared/_Sayfalama.cshtml
```

`SayfalamaViewModel` toplam sayfa, önceki/sonraki sayfa ve görünen kayıt aralığını hesaplar. Partial View mevcut query string değerlerini korur. Örneğin kullanıcı `durum=aktif` filtresindeyken ikinci sayfaya geçtiğinde filtre kaybolmaz.

## 3. Görev sorgusu nasıl optimize edildi?

`GorevController.Index` artık:

- Yetki kapsamını önce SQL sorgusuna uygular.
- Özet sayılarını tek aggregate sorgusunda hesaplar.
- Yalnızca mevcut sayfadaki görevleri yükler.
- Yorum sayılarını yalnızca sayfadaki görev ID'leri için hesaplar.
- Atanan kişi filtresini ayrı, küçük bir sorguyla üretir.
- `AsNoTracking()` kullanır.
- İstek iptal edilirse sorguyu durdurmak için `CancellationToken` kullanır.

Eski yapıda bütün görünür görevler belleğe alınıyordu. Yeni yapıda en fazla seçilen sayfa boyutu kadar görev yüklenir.

## 4. Dashboard performansı

`HomeController.Index` daha önce bütün görevleri belleğe alıp metrikleri C# ile hesaplıyordu. Artık:

- Toplam, açık, devam eden, QA, hata, tamamlanan, geciken ve kritik sayıları SQL'de hesaplanır.
- Son görevlerden yalnızca 6 kayıt alınır.
- Dikkat gerektiren görevlerden yalnızca 6 kayıt alınır.

Bu değişiklik görev sayısı büyüdükçe en fazla fark yaratan performans iyileştirmelerinden biridir.

## 5. Veritabanı indeksleri

Yeni migration:

```text
20260714150000_PerformansIndeksleriEkle
```

Eklenen birleşik indeksler:

```text
Kullanicilar(AktifMi, Role)
Kullanicilar(DepartmanId, AktifMi)
Kullanicilar(YoneticiId, AktifMi)
Gorevler(AtananKullaniciId, Durum, SonTarih)
Gorevler(Durum, OlusturulmaTarihi)
Gorevler(OlusturanKullaniciId, OlusturulmaTarihi)
```

İndeks her sorguyu hızlandırmaz. Sık kullanılan `WHERE`, `ORDER BY` ve ilişki alanlarına göre seçildi. Gereksiz her indeks kayıt ekleme ve güncelleme maliyetini artıracağı için yalnızca temel sorgular hedeflendi.

## 6. Ortak UI bileşenleri

`site.css` içine tekrar kullanılan şu sınıflar eklendi:

```text
.app-input
.app-select
.app-alert
.empty-state
.metric-chip
.pagination-number
```

Amaç her View dosyasında aynı uzun Tailwind sınıf zincirini tekrar etmemektir. Tasarım değişikliği gerektiğinde tek dosya düzenlenebilir.

Ana sayfa JavaScriptleri de dış dosyalara ayrıldı:

```text
wwwroot/js/layout.js
wwwroot/js/site.js
wwwroot/js/pages/gorev-index.js
wwwroot/js/pages/kullanicilar-index.js
```

Böylece layout, görev listesi ve kullanıcı listesinde yüzlerce satır inline JavaScript bulunmaz.

## 7. Hata sayfaları

Yeni hata sistemi iki farklı durumu ele alır:

### Exception

Beklenmeyen uygulama hataları production ortamında:

```text
/Home/Error
```

sayfasına gider.

### HTTP durum kodu

Bulunmayan adres gibi durumlar:

```text
/Home/StatusCode?code=404
```

üzerinden aynı tasarım sistemiyle gösterilir.

Hata ekranında `RequestId` gösterilir. Loglarda aynı kod aranarak ilgili istek bulunabilir. Kullanıcıya exception stack trace gösterilmez.

## 8. Sağlık kontrolü

Yeni endpoint:

```text
/saglik
```

SQL Server bağlantısını kontrol eder ve JSON döndürür. Docker, IIS, reverse proxy veya uptime servisleri bu adresi kullanabilir.

Örnek sonuç:

```json
{
  "durum": "Healthy",
  "sureMs": 18.4,
  "kontroller": {
    "veritabani": {
      "durum": "Healthy",
      "aciklama": "Veritabanı bağlantısı kullanılabilir."
    }
  }
}
```

## 9. Response compression ve static cache

Brotli ve Gzip response compression etkinleştirildi. Statik dosyalar `asp-append-version` ile hashli URL kullandığı için bir hafta `immutable` cache ile saklanır.

Bu sayede CSS, JavaScript ve görseller her sayfa geçişinde yeniden indirilmez.

## 10. Otomatik test projesi

Ana proje klasörünün yanında şu test projesi bulunur:

```text
YonetimPaneli.Tests/
```

Test edilen alanlar:

- Görev gecikme ve son tarih hesapları
- Öncelik metinleri
- Güçlü şifre doğrulaması
- Sayfalama hesapları

Çalıştırma:

```powershell
dotnet test .\YonetimPaneli.Tests\YonetimPaneli.Tests.csproj
```

Yeni bir iş kuralı eklediğinizde önce servise, ardından test projesine örnekler eklemek önerilir.

## 11. Uygulama ve test sırası

Uygulamayı kapatın ve proje klasöründe:

```powershell
dotnet build
```

Migration:

```powershell
dotnet ef database update
```

veya Package Manager Console:

```powershell
Update-Database
```

Testler:

```powershell
dotnet test ..\YonetimPaneli.Tests\YonetimPaneli.Tests.csproj
```

Uygulama:

```powershell
dotnet run
```

Manuel kontroller:

1. Görev filtresi uygula, ikinci sayfaya geç ve filtrenin korunduğunu doğrula.
2. Sayfa boyutunu 10, 20 ve 50 olarak değiştir.
3. Kullanıcı araması ve departman filtresini birlikte kullan.
4. Bildirimlerde yalnızca okunmamışları gösterip sayfa değiştir.
5. Olmayan bir adrese girerek 404 ekranını kontrol et.
6. `/saglik` adresini aç.
7. Test projesini çalıştır.

## 12. Bundan sonra ne eklenebilir?

Temel 10 aşama tamamlandı. Bundan sonraki çalışmalar zorunlu düzeltme değil, ürün modülü sayılır:

- Kanban görünümü
- Takvim görünümü
- Proje ve ekip modülü
- Dosya ekleri
- E-posta bildirimleri
- Gerçek zamanlı SignalR bildirimleri
- Excel/PDF raporlama
- Çok kiracılı şirket yapısı
- Mobil uygulama/API
