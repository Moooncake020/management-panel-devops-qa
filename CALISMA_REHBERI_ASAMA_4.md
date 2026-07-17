# AŞAMA 4 — Görev Formları, ViewModel ve Güvenli Görsel Yükleme

Bu aşamada görev ekleme ve düzenleme ekranları veritabanı modelinden ayrıldı. Formlar artık güçlü tipli ViewModel kullanıyor; hatalı veri girildiğinde kullanıcı aynı sayfada alan bazlı Türkçe uyarı görüyor. Görev görselleri de yalnızca uzantıya bakılarak değil, boyut, MIME türü ve dosya imzası kontrol edilerek kaydediliyor.

> Bu aşamada veritabanı modeli değişmediği için migration gerekmez.

---

## 1. Değişen ve eklenen dosyalar

### Yeni ViewModel dosyaları

```text
ViewModels/Gorevler/
├── GorevEkleViewModel.cs
├── GorevDuzenleViewModel.cs
└── GorevKullaniciSecenegiViewModel.cs
```

### Yeni servis

```text
Services/GorevResimServisi.cs
```

### Güncellenen dosyalar

```text
Controllers/GorevController.cs
Views/Gorev/Ekle.cshtml
Views/Gorev/Duzenle.cshtml
Views/Gorev/Index.cshtml
Program.cs
```

### Çalışma zamanında kullanılan klasör

```text
wwwroot/uploads/gorevler/
```

---

# 2. Neden Gorev modeli doğrudan forma bağlanmıyor?

Eski yöntemde controller şu şekildeydi:

```csharp
public IActionResult Ekle(Gorev yeniGorev, IFormFile? resimDosyasi)
```

Bu yaklaşımda tarayıcıdan gönderilen alanlar doğrudan veritabanı modeline bağlanır. Kullanıcı arayüzünde gösterilmeyen bir alan elle POST isteğine eklenebilir. Buna overposting denir.

Yeni yöntem:

```csharp
public async Task<IActionResult> Ekle(
    GorevEkleViewModel model,
    CancellationToken cancellationToken)
```

`GorevEkleViewModel` yalnızca formdan kabul edilen alanları içerir:

```text
Başlık
Açıklama
Atanacak kullanıcı
Görsel
```

Aşağıdaki alanlar kullanıcıdan alınmaz; controller tarafından güvenli şekilde atanır:

```text
Durum = Açık
Oluşturan kullanıcı = giriş yapan kullanıcı
Oluşturulma tarihi = DateTime.Now
```

Akış:

```text
Form → ViewModel → Yetki ve doğrulama → Yeni Gorev nesnesi → Veritabanı
```

---

# 3. Data Annotation doğrulamaları

`GorevEkleViewModel` ve `GorevDuzenleViewModel` üzerinde şu kurallar bulunur:

```csharp
[Required]
[StringLength(120, MinimumLength = 3)]
public string Baslik { get; set; }
```

Başlık için:

```text
Boş bırakılamaz
En az 3 karakter
En fazla 120 karakter
```

Açıklama için:

```text
Boş bırakılamaz
En az 5 karakter
En fazla 2000 karakter
```

Atanacak kullanıcı için:

```text
Seçim zorunludur
Seçilen kişi aktif olmalıdır
Giriş yapan kullanıcının bu kişiye görev verme yetkisi olmalıdır
```

Data Annotation temel biçim kontrolünü yapar. Organizasyon yetkisi gibi iş kuralları controller içinde ayrıca kontrol edilir.

---

# 4. Neden ModelState yeniden doğrulanıyor?

ASP.NET Core formu controller metoduna bağlamadan önce doğrular. Ancak formdaki metinler daha sonra `Trim()` ile temizlenir.

Örnek kötü giriş:

```text
Başlık = "     "
```

Bu metin başlangıçta karakter içeriyor gibi görünür. Trim işleminden sonra boş olur.

Bu nedenle controller şu işlemi yapar:

```csharp
GorevFormMetinleriniTemizle(model);
ModelState.Clear();
TryValidateModel(model);
```

Sıra:

```text
1. Metni temizle
2. Eski doğrulama sonucunu temizle
3. Temizlenmiş modeli yeniden doğrula
```

Böylece yalnızca boşluk girilerek form gönderilemez.

---

# 5. Görev neden her zaman Açık durumunda oluşturuluyor?

Yeni görev ekranından durum seçimi kaldırıldı. Controller yeni görevi şu şekilde oluşturur:

```csharp
Durum = "Açık"
```

Bunun nedeni kontrollü görev durum akışıdır:

```text
Açık
→ Devam Ediyor
→ QA / Test Bekleyen
→ Tamamlandı veya Bug / Hata
```

Yeni görev doğrudan “Devam Ediyor” veya “Tamamlandı” başlatılamaz. İş akışının başlangıç noktası tek ve nettir.

---

# 6. Düzenleme yetkisi nasıl korunuyor?

`GorevDuzenleViewModel` tam form alanlarını içerir. Ancak controller önce yetkiyi hesaplar:

```csharp
var tamDuzenlemeYetkisi =
    TamDuzenlemeYetkisiVar(aktifKullaniciId, gorev);
```

Tam yetkili kişi:

```text
Başlığı değiştirebilir
Açıklamayı değiştirebilir
Atanan kişiyi değiştirebilir
Durumu değiştirebilir
Görseli değiştirebilir veya kaldırabilir
```

Yalnızca görevin atandığı çalışan:

```text
Sadece izinli durum geçişini yapabilir
```

Çalışan tarayıcı geliştirici araçlarıyla gizli alanları değiştirirse controller formdan gelen içerik değerlerini kullanmaz:

```csharp
model.Baslik = gorev.Baslik;
model.Aciklama = gorev.Aciklama;
model.AtananKullaniciId = gorev.AtananKullaniciId;
```

Güvenlik kuralı yalnızca View tarafında uygulanmaz; sunucu tarafında yeniden uygulanır.

---

# 7. Güvenli görsel yükleme servisi

Eski kod kullanıcı tarafından gönderilen dosya adını saklıyor ve yalnızca dosyayı klasöre kopyalıyordu.

Yeni servis:

```text
GorevResimServisi
```

şu kontrolleri yapar.

## Dosya boyutu

```text
En fazla 5 MB
```

Controller metodundaki toplam istek sınırı:

```text
Yaklaşık 6 MB
```

Aradaki alan multipart başlıkları ve diğer form verileri içindir.

## İzin verilen uzantılar

```text
.jpg
.jpeg
.png
.webp
```

## MIME türü kontrolü

Örneğin `.png` dosyasının içerik türü `image/png` olmalıdır.

## Dosya imzası kontrolü

Dosya uzantısı değiştirilebilir. Örneğin bir `.exe` dosyası `.png` olarak yeniden adlandırılabilir.

Servis dosyanın ilk baytlarını kontrol eder:

```text
JPEG → FF D8 FF
PNG  → 89 50 4E 47 0D 0A 1A 0A
WEBP → RIFF .... WEBP
```

Bu kontrol dosyanın gerçekten beklenen görsel formatında olup olmadığını doğrular.

## Güvenli dosya adı

Kullanıcının gönderdiği dosya adı sunucuda kullanılmaz.

Yeni dosya adı:

```text
GUID + doğrulanmış uzantı
```

Örnek:

```text
1db1c53f29f749b1bd4e829c303a3a5f.png
```

Dosyalar şu klasöre kaydedilir:

```text
wwwroot/uploads/gorevler/
```

---

# 8. Eski görsel ne zaman silinir?

Görev düzenlenirken yeni bir görsel yüklenirse:

```text
1. Yeni görsel doğrulanır ve kaydedilir
2. Veritabanı güncellenir
3. Güncelleme başarılıysa eski görsel silinir
```

Veritabanı güncellemesi başarısız olursa yeni dosya silinir. Böylece klasörde kullanılmayan dosyaların birikmesi azaltılır.

Kullanıcı “Mevcut görseli kaldır” seçeneğini işaretlerse veritabanındaki görsel yolu temizlenir ve servis tarafından yönetilen eski dosya silinir.

Servis güvenlik nedeniyle yalnızca şu klasördeki yolları siler:

```text
/uploads/gorevler/
```

Eski `/images/` dosyaları otomatik olarak silinmez. Bu, yanlış bir yolun fiziksel dosya sisteminden silinmesini önler.

---

# 9. Form kullanıcı deneyimi

Görev formlarına şu geliştirmeler eklendi:

```text
Alan bazlı Türkçe hata mesajları
Başlık karakter sayacı
Açıklama karakter sayacı
Görsel ön izlemesi
Dosya adı ve boyut bilgisi
İzin verilen format açıklaması
Form gönderirken butonu kilitleme
Başarılı işlem mesajı
Mevcut görseli kaldırma seçeneği
```

JavaScript kontrolleri kullanıcıya hızlı geri bildirim verir. Ancak güvenlik JavaScript'e bırakılmaz; aynı kurallar sunucuda tekrar kontrol edilir.

---

# 10. Program.cs içindeki kayıtlar

Servis dependency injection sistemine kaydedildi:

```csharp
builder.Services.AddScoped<GorevResimServisi>();
```

Multipart istek sınırı:

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 6_291_456;
});
```

Controller servisi constructor üzerinden alır:

```csharp
public GorevController(
    AppDbContext context,
    GorevYetkiServisi gorevYetkiServisi,
    GorevDurumServisi gorevDurumServisi,
    GorevResimServisi gorevResimServisi)
```

Bu yapıya dependency injection denir.

---

# 11. Test senaryoları

## Doğrulama testi

```text
1. Başlığı boş bırak
2. Sadece boşluk yaz
3. 2 karakterlik başlık yaz
4. Açıklamayı boş bırak
5. Kullanıcı seçmeden gönder
```

Beklenen: Form aynı sayfada kalmalı ve ilgili alanın altında Türkçe hata görünmelidir.

## Yetki testi

```text
1. Takım lideri hesabıyla giriş yap
2. Yeni görev ekranını aç
3. Listede yalnızca kendisi ve doğrudan astları görünmeli
4. Elle başka kullanıcı ID'si gönderilmeye çalışıldığında sunucu reddetmeli
```

## Görsel testi

```text
1. Geçerli JPG yükle
2. Geçerli PNG yükle
3. Geçerli WEBP yükle
4. 5 MB'tan büyük dosya seç
5. TXT dosyasını PNG olarak yeniden adlandırıp yüklemeyi dene
```

Beklenen:

```text
Geçerli görseller kaydedilmeli
Büyük dosya reddedilmeli
Sahte uzantılı dosya reddedilmeli
```

## Düzenleme testi

```text
1. Yönetici olarak görev içeriğini değiştir
2. Atanan kişiyi değiştir
3. Yeni görsel yükle
4. Mevcut görseli kaldır
5. Çalışan olarak aynı göreve gir
6. Çalışan yalnızca durum değiştirebilmeli
```

---

# 12. Çalıştırma

Bu aşamada migration yoktur.

```powershell
dotnet build
dotnet run
```

Build sırasında hata olursa önce çalışan uygulamayı durdurun:

```text
Shift + F5
```

Ardından tekrar build alın.

---

# 13. Sonraki aşama

Bir sonraki aşamada önerilen geliştirme:

```text
Görev detay sayfası
Görev aktivite geçmişi
Yorum sistemi
Özel onay modalı
Silme işlemini fiziksel silme yerine arşivleme
```
