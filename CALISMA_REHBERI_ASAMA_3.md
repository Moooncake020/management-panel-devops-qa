# AŞAMA 3 — Responsive Görev Listesi, Filtreleme ve Renk Sistemi

Bu aşama, görevler ekranının farklı ekran genişliklerinde daha rahat kullanılmasını ve çok sayıda görev içinden hızlı seçim yapılmasını sağlar.

> Bu aşamada veritabanı modeli değişmediği için migration gerekmez.

---

## 1. Bu aşamada değişen dosyalar

### Yeni ViewModel dosyaları

```text
ViewModels/Gorevler/
├── GorevIndexViewModel.cs
├── GorevListeItemViewModel.cs
└── GorevFiltreSecenegiViewModel.cs
```

### Yeni partial view dosyaları

```text
Views/Gorev/
├── _GorevKartlari.cshtml
└── _GorevTablosu.cshtml

Views/Shared/
└── _GorevDurumRozeti.cshtml
```

### Güncellenen dosyalar

```text
Controllers/GorevController.cs
Views/Gorev/Index.cshtml
Views/Home/Index.cshtml
Views/_ViewImports.cshtml
wwwroot/css/site.css
```

---

# 2. Neden GorevIndexViewModel oluşturuldu?

Eski yapıda `Gorev` veritabanı modeli doğrudan View'a gönderiliyordu. Fakat liste ekranının ihtiyaçları veritabanı modelinden farklıdır.

Liste ekranında ayrıca şu bilgiler gerekir:

- Kullanıcı görevi silebilir mi?
- Görev giriş yapan kullanıcıya mı atanmış?
- Görevi giriş yapan kullanıcı mı oluşturmuş?
- Görev ekip görevi mi?
- JavaScript filtresi için durum kodu nedir?
- Filtre dropdown'ında hangi kullanıcılar gösterilecek?
- Durumlara göre kaç görev var?

Bu bilgiler `GorevIndexViewModel` ve `GorevListeItemViewModel` içinde hazırlanır.

Bu yaklaşımın faydası:

```text
Veritabanı modeli → Controller → Ekrana özel ViewModel → View
```

View, yetki hesabı veya veri dönüştürme yapmak zorunda kalmaz.

---

# 3. Controller'daki yeni görev listeleme akışı

`GorevController.Index()` önce giriş yapan kullanıcıyı bulur.

Daha sonra mevcut yetki sistemine göre kullanıcının görebileceği görevleri getirir:

```text
Admin / Genel Müdür
→ Bütün görevler

Diğer kullanıcılar
→ Kendisine atananlar
→ Kendisinin oluşturdukları
→ Yetkisi altındaki kullanıcılara atananlar
```

Ardından her görev `GorevListeItemViewModel` nesnesine dönüştürülür.

Örnek mantık:

```csharp
KullaniciyaAtanmisMi = atananKullaniciId == aktifKullanici.Id;
KullaniciTarafindanOlusturulmusMu =
    gorev.OlusturanKullaniciId == aktifKullanici.Id;
```

Bu iki bilgi, ekrandaki “Bana atananlar” ve “Benim oluşturduklarım” filtrelerini besler.

---

# 4. Responsive görev görünümü

Aynı görev verisi iki farklı partial view tarafından gösterilir.

## Küçük ve orta ekranlar

```text
_GorevKartlari.cshtml
```

Görevler kart olarak gösterilir. Kart içinde:

- Durum rozeti
- Başlık
- Açıklama
- Atanan kişi
- Görevi oluşturan
- Tarih
- Düzenleme işlemi

bulunur.

## Çok geniş ekranlar

```text
_GorevTablosu.cshtml
```

Tablo yalnızca Tailwind `2xl` breakpoint'inde görünür.

```html
<div class="hidden 2xl:block">
```

Bu sayede sidebar açıkken dar kalan 1024–1400 px ekranlarda geniş tablo zorlanmaz; kart görünümü kullanılır.

---

# 5. Filtre sistemi nasıl çalışıyor?

Her görev kartına ve tablo satırına `data-*` alanları eklenmiştir.

Örnek:

```html
data-search="Görev başlığı açıklama kullanıcı"
data-status="qa"
data-scopes="atanan olusturan"
data-assignee="4"
data-created="2026-07-14T10:00:00"
```

JavaScript bu alanları okuyarak filtreleme yapar.

## Arama filtresi

Başlık, açıklama, atanan kişi, görevi veren ve durum içinde arama yapar.

## Durum filtresi

```text
Açık
Devam Ediyor
QA / Test Bekleyen
Bug / Hata
Tamamlandı
```

## Kapsam filtresi

```text
Bana atananlar
Benim oluşturduklarım
Ekip / diğer görevler
```

Bir görev birden fazla kapsama girebilir. Örneğin kullanıcı görevi hem oluşturmuş hem de kendisine atamış olabilir.

Bu nedenle `KapsamKodlari` tek değer değil, boşlukla ayrılmış birden fazla değer üretir:

```text
atanan olusturan
```

## Atanan kişi filtresi

Controller, görünür görevlerdeki kullanıcıları gruplayarak dropdown seçeneği üretir.

## Sıralama

- En yeni
- En eski
- Başlık A–Z
- Duruma göre

Sıralama hem kart container'ına hem tablo body alanına ayrı ayrı uygulanır.

---

# 6. Özet kartları neden buton oldu?

Durum sayıları yalnızca bilgi göstermek yerine filtre görevi de yapar.

Örneğin “QA / Test” kartına basıldığında durum filtresi otomatik olarak `qa` olur.

Aynı karta ikinci defa basılırsa filtre kaldırılır.

Bu davranış:

```javascript
statusFilter.value =
    statusFilter.value === selectedStatus
        ? 'all'
        : selectedStatus;
```

mantığıyla çalışır.

---

# 7. Ortak görev renk sistemi

Görev durumlarının renkleri `site.css` içinde merkezi olarak tanımlandı.

```text
Açık          → Slate
Devam Ediyor  → Teal
QA / Test     → Purple
Bug / Hata    → Red
Tamamlandı    → Emerald
```

Durum rozeti tek bir shared partial üzerinden üretilir:

```text
Views/Shared/_GorevDurumRozeti.cshtml
```

Kullanım örneği:

```cshtml
<partial name="_GorevDurumRozeti" model="item.Durum" />
```

Bu partial hem görev listesinde hem anasayfada kullanılır. Böylece QA durumu bir sayfada sarı, başka sayfada mor görünmez.

---

# 8. Görsel modalındaki erişilebilirlik geliştirmeleri

Görev görseli artık yalnızca `onclick` verilen bir resim değildir. Klavyeyle odaklanabilen bir `button` içine alınmıştır.

Modal üzerinde:

```html
role="dialog"
aria-modal="true"
aria-labelledby="taskImageModalTitle"
```

alanları bulunur.

Modal açıldığında kapatma butonuna focus verilir. Kapatıldığında focus, modalı açan göreve geri döner.

Escape tuşu ve arka alana tıklama da modalı kapatır.

---

# 9. Home ekranında yapılan küçük düzeltmeler

- QA özet kartı sarı yerine ortak Purple rengine geçirildi.
- Durum rozetleri shared partial kullanmaya başladı.
- “Son Aktiviteler” başlığı, içerik gerçekten aktivite kaydı olmadığı için “Son Eklenen Görevler” olarak değiştirildi.

---

# 10. Test senaryoları

Projeyi açtıktan sonra:

```powershell
dotnet build
dotnet run
```

çalıştır.

## Responsive test

Tarayıcı genişliğini değiştir:

```text
Telefon / tablet / küçük laptop
→ Kart görünümü

Çok geniş masaüstü
→ Tablo görünümü
```

## Filtre testi

1. Arama alanına görev başlığı yaz.
2. Durum seç.
3. “Bana atananlar” filtresini seç.
4. Atanan kişi seç.
5. Sıralamayı değiştir.
6. “Filtreleri Temizle” butonuna bas.

## Özet kart testi

- QA kartına bas.
- Yalnızca QA görevleri görünmeli.
- QA kartına tekrar bas.
- Bütün görevler yeniden görünmeli.

## Görsel modal testi

- Tab tuşuyla görev görseline gel.
- Enter ile modalı aç.
- Escape ile kapat.
- Focus tekrar aynı görsele dönmeli.

## Yetki testi

- Admin hesabıyla bütün görevlerin göründüğünü kontrol et.
- Normal çalışan hesabıyla yalnızca yetkili görevlerin göründüğünü kontrol et.
- Görevi silme menüsünün yalnızca yetkili kullanıcılarda çıktığını kontrol et.

---

# 11. Bu aşamada özellikle yapılmayanlar

Bu aşamanın odağı görev listeleme deneyimidir. Aşağıdakiler sonraki aşamalara bırakıldı:

- Görev ekleme/düzenleme ViewModel doğrulaması
- Öncelik ve son tarih
- Server-side sayfalama
- Özel silme onay modalı
- Görev detay sayfası
- Yorum ve görev geçmişi

---

# 12. Sonraki aşama

Aşama 4'te görev oluşturma ve düzenleme formları ele alınabilir:

```text
Görev ViewModel'leri
Alan bazlı doğrulama
Güvenli görsel yükleme
Dosya boyutu ve uzantı kontrolü
Form hata mesajları
Gönderim sırasında loading durumu
```
