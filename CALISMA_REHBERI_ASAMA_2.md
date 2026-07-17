# Aşama 2 — ViewModel, Form Doğrulama ve Kullanıcı Güvenliği

Bu aşama kullanıcı ekleme ve düzenleme ekranlarını yalnızca “çalışan form” olmaktan çıkarıp daha güvenli ve öğrenilebilir bir MVC yapısına dönüştürür.

Aşama 1’de görünüm ve responsive yapı düzenlenmişti. Bu aşamada formun arkasındaki veri akışı düzenlendi.

## Bu aşamada çözülen problemler

1. `AppUser` modeli doğrudan forma bağlanıyordu.
2. Form hatalı gönderildiğinde kullanıcıya alan bazlı açıklama verilmiyordu.
3. Aynı e-posta adresiyle birden fazla kullanıcı oluşturulabiliyordu.
4. Şifre tekrar alanı yoktu.
5. Düzenleme ekranındaki `selected="True/False"` kullanımı güvenilir değildi.
6. Hatalı yeni kullanıcı formu gönderildiğinde drawer kapanıyordu.
7. Düzenleme ekranından pasifleştirme yapılarak güvenlik kontrolleri aşılabiliyordu.
8. Geçersiz departman, unvan, yönetici, enum veya rol değeri manuel POST ile gönderilebiliyordu.

---

# 1. Eklenen ViewModel dosyaları

Konum:

```text
ViewModels/Kullanicilar/
```

## `KullaniciEkleViewModel.cs`

Yeni kullanıcı formundan alınmasına izin verilen alanları içerir.

Örnek doğrulamalar:

```csharp
[Required(ErrorMessage = "E-posta adresi zorunludur.")]
[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
[StringLength(150)]
public string Email { get; set; } = string.Empty;
```

Şifre tekrar kontrolü:

```csharp
[Compare(nameof(Password),
    ErrorMessage = "Şifre ve şifre tekrarı aynı olmalıdır.")]
public string PasswordTekrar { get; set; } = string.Empty;
```

### Neden `AppUser` yerine ViewModel?

`AppUser` veritabanı varlığıdır. Formdan hangi alanların alınacağına form modeli karar vermelidir.

Bu ayrımın faydaları:

- Overposting riski azalır.
- Form doğrulamaları veritabanı modelini kirletmez.
- Şifre tekrar gibi veritabanında saklanmayacak alanlar eklenebilir.
- Ekrana özel alanlar daha rahat yönetilir.

## `KullaniciDuzenleViewModel.cs`

Düzenleme ekranının güçlü tip modelidir.

Yeni şifre zorunlu değildir:

```csharp
public string? YeniPassword { get; set; }
```

Alan boş bırakılırsa controller mevcut şifreyi korur.

## `KullaniciFormSecenekleriViewModel.cs`

Departman, unvan, yönetici, kıdem ve organizasyon rolü seçeneklerini taşır.

Eski yapı:

```csharp
ViewBag.Departmanlar
ViewBag.Unvanlar
```

Yeni yapı:

```csharp
Model.Secenekler.Departmanlar
Model.Secenekler.Unvanlar
```

Güçlü tip kullanıldığı için yanlış alan adı derleme aşamasında fark edilir.

## `KullaniciIndexViewModel.cs`

Kullanıcı yönetimi sayfasının tamamını taşır:

- Kullanıcı listesi
- Yeni kullanıcı formu
- Aktif kullanıcı ID’si
- Drawer açık mı bilgisi
- Aktif/pasif/admin sayaçları

## `KullaniciListeViewModel.cs`

Kart ve tablo partial’larının ortak modelidir.

---

# 2. `KullanicilarController.cs` değişiklikleri

## `ModelState` kontrolü

Form modeli önce Data Annotation kurallarıyla doğrulanır, sonra iş kuralları kontrol edilir.

```csharp
if (!ModelState.IsValid)
{
    return View(model);
}
```

## E-posta benzersizliği

Yeni kullanıcı oluştururken ve mevcut kullanıcıyı düzenlerken e-posta kontrol edilir.

```csharp
if (EmailKullaniliyorMu(model.Email))
{
    ModelState.AddModelError(
        nameof(model.Email),
        "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor."
    );
}
```

Düzenlemede kullanıcının kendi kaydı karşılaştırma dışında bırakılır.

## E-posta normalizasyonu

Kaydedilmeden önce e-posta:

- baştaki ve sondaki boşluklardan temizlenir,
- küçük harfe dönüştürülür.

```csharp
email.Trim().ToLowerInvariant();
```

Bu sayede şu adresler aynı kabul edilir:

```text
ADMIN@TEST.COM
admin@test.com
 admin@test.com 
```

## Sunucu tarafı seçim doğrulaması

Kullanıcı tarayıcı geliştirici araçlarıyla sahte ID gönderse bile controller kontrol eder:

- Departman gerçekten var mı?
- Unvan gerçekten var mı?
- Yönetici aktif mi?
- Kıdem enum değeri geçerli mi?
- Organizasyon rolü geçerli mi?
- Sistem rolü yalnızca `Admin` veya `Kullanici` mi?

## Güvenli pasifleştirme

Düzenleme ekranından `AktifMi = false` gönderilse bile aşağıdaki kontroller çalışır:

- Kendi hesabını pasifleştiremez.
- Son aktif admin pasifleştirilemez.
- Aktif astı olan kullanıcı pasifleştirilemez.
- Tamamlanmamış görevi olan kullanıcı pasifleştirilemez.

Bu kontrol artık yalnızca `PasifYap` butonunda değil, düzenleme POST metodunda da uygulanır.

## Veritabanı hatası yakalama

`SaveChanges()` sırasında hata olursa kullanıcı genel hata sayfasına atılmaz. Form aynı değerlerle tekrar gösterilir.

```csharp
catch (DbUpdateException)
{
    ModelState.AddModelError(
        string.Empty,
        "Kullanıcı kaydedilirken veritabanı hatası oluştu."
    );
}
```

---

# 3. Yeni kullanıcı drawer değişiklikleri

Dosya:

```text
Views/Kullanicilar/_YeniKullaniciDrawer.cshtml
```

Eklenenler:

- `asp-for`
- `asp-validation-for`
- `asp-validation-summary`
- Şifre tekrar alanı
- İki şifre alanı için göster/gizle
- Autocomplete değerleri
- Zorunlu alan işaretleri
- Gönderim sırasında buton kilitleme

Örnek:

```cshtml
<input asp-for="Email" />
<span asp-validation-for="Email"></span>
```

## Hatalı gönderimde drawer neden açık kalıyor?

Controller şu değeri gönderir:

```csharp
YeniKullaniciDrawerAcik = true
```

Sayfa JavaScript’i bu değeri okuyup drawer’ı tekrar açar.

Böylece kullanıcı:

- formu yeniden açmak zorunda kalmaz,
- yazdığı değerleri kaybetmez,
- hatalı alanı doğrudan görür.

---

# 4. Düzenleme ekranı değişiklikleri

Dosya:

```text
Views/Kullanicilar/Duzenle.cshtml
```

Eski riskli kullanım kaldırıldı:

```cshtml
selected="@(Model.DepartmanId == departman.Id)"
```

Yerine `asp-for` kullanıldı:

```cshtml
<select asp-for="DepartmanId">
```

ASP.NET Core doğru seçeneği otomatik olarak seçer.

Eklenenler:

- Görünür label’lar
- Alan bazlı hata mesajları
- Şifre ve şifre tekrar alanları
- Kendi hesabını pasifleştirme alanının kilitlenmesi
- Güçlü tip select bağlama
- Gönderim sırasında buton kilitleme

---

# 5. Client-side doğrulama

Dosya:

```text
Views/Shared/_ValidationScriptsPartial.cshtml
```

Script sırası önemlidir:

1. jQuery
2. jquery.validate
3. jquery.validate.unobtrusive

Data Annotation kuralları HTML üzerinde `data-val-*` niteliklerine dönüşür. Böylece birçok hata sunucuya gitmeden tarayıcıda gösterilir.

Sunucu doğrulaması yine zorunludur. JavaScript kapatılabilir veya POST isteği elle gönderilebilir.

---

# 6. Doğrulama stilleri

Dosya:

```text
wwwroot/css/site.css
```

Eklenen ortak sınıflar:

- `.input-validation-error`
- `.field-validation-valid`
- `.validation-summary-valid`
- `.validation-summary-errors`

Hatalı input kırmızı kenarlık ve açık kırmızı zemin alır.

---

# 7. Test senaryoları

## Test 1 — Boş form

1. Kullanıcılar sayfasına girin.
2. Yeni kullanıcı drawer’ını açın.
3. Hiçbir alanı doldurmadan gönderin.

Beklenen:

- Form gönderilmemeli veya sunucudan hata dönmeli.
- Ad, soyad, e-posta, şifre ve şifre tekrar hataları görünmeli.
- Drawer açık kalmalı.

## Test 2 — Geçersiz e-posta

```text
kemal@
```

Beklenen:

```text
Geçerli bir e-posta adresi giriniz.
```

## Test 3 — Kısa şifre

```text
123
```

Beklenen:

```text
Şifre en az 6, en fazla 100 karakter olmalıdır.
```

## Test 4 — Şifreler farklı

```text
Şifre: 123456
Tekrar: 654321
```

Beklenen:

```text
Şifre ve şifre tekrarı aynı olmalıdır.
```

## Test 5 — Tekrarlanan e-posta

Var olan bir e-postayla yeni kullanıcı oluşturun.

Beklenen:

```text
Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.
```

## Test 6 — Düzenlemede e-posta

Kullanıcı kendi mevcut e-postasını değiştirmeden kaydedebilmeli.
Başka kullanıcının e-postasını yazarsa hata almalı.

## Test 7 — Şifreyi değiştirmeden düzenleme

Yeni şifre alanlarını boş bırakıp kullanıcıyı kaydedin.

Beklenen:

- Kullanıcı güncellenmeli.
- Eski şifre korunmalı.

## Test 8 — Son admin

Sistemde tek aktif admin kaldığında rolünü Kullanıcı yapmayı veya hesabı pasifleştirmeyi deneyin.

Beklenen:

```text
Sistemde en az bir aktif admin kalmalıdır.
```

## Test 9 — Aktif astı olan kullanıcı

Aktif astı olan bir yöneticiyi düzenleme ekranından pasif yapmayı deneyin.

Beklenen:

```text
Bu kullanıcının aktif astları var.
```

## Test 10 — Tamamlanmamış görev

Üzerinde açık görev bulunan kullanıcıyı pasif yapmayı deneyin.

Beklenen:

- Pasifleştirme engellenmeli.
- Önce görev devretme istenmeli.

---

# Bu aşamada migration gerekiyor mu?

Hayır.

Bu aşama veritabanı kolonlarını değiştirmez. ViewModel, controller ve view katmanlarını düzenler.

```text
Update-Database çalıştırmayın.
```

---

# Bilerek sonraya bırakılan güvenlik konusu

Şifreler hâlâ eski sistemle uyumluluk için düz metin olarak tutuluyor.

Bu üretim için güvenli değildir. Şifre hashleme ayrı aşamada yapılacak çünkü:

- login kontrolü değişecek,
- mevcut seed kullanıcıları dönüştürülecek,
- eski kullanıcıların geçiş yöntemi belirlenecek.

---

# Sonraki aşama

**Aşama 3 — Görevler ekranının responsive kart/tablo tasarımı ve gelişmiş filtreleme**

Plan:

- 1536 px altı görev kartları
- Masaüstünde sade görev tablosu
- Durum, atanan kişi ve görev sahibi filtreleri
- “Bana atanan / Benim oluşturduklarım” filtreleri
- Renk sistemi standardizasyonu
- Görev işlem menülerinin sadeleştirilmesi
