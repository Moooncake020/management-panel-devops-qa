# Aşama 9 — Hesap, Şifre ve Oturum Güvenliği

Bu aşamada uygulamanın giriş sistemi eğitim amaçlı düz metin şifre yapısından çıkarılıp ASP.NET Core'un güvenli şifre hashleme ve oturum doğrulama yaklaşımına geçirildi.

> Bu dosyayı proje üzerinde çalışırken kaynak rehber olarak kullanabilirsin. Hangi dosyanın neden değiştiği ve akışın nasıl çalıştığı aşağıda açıklanmıştır.

---

## 1. Bu aşamada çözülen temel problemler

Önceki sürümde:

- Şifreler veritabanında düz metin tutuluyordu.
- JWT imzalama anahtarı yapılandırma dosyasına yazılabiliyordu.
- Çıkış işlemi GET bağlantısı ile çalışıyordu.
- Hatalı giriş denemelerine karşı hesap kilidi yoktu.
- Aynı IP adresinden sınırsız giriş isteği yapılabiliyordu.
- Kullanıcı pasifleştirildiğinde eski JWT süresi dolana kadar çalışabilirdi.
- Test hesapları üretim ortamında da oluşturulabilirdi.

Aşama 9 ile bu alanlar güvenli hale getirildi.

---

## 2. Şifre hashleme nasıl çalışıyor?

Yeni servis:

```text
Services/SifreServisi.cs
```

ASP.NET Core `PasswordHasher<AppUser>` kullanılır. Bu yapı:

- PBKDF2 tabanlı hash üretir.
- Her şifre için ayrı rastgele salt kullanır.
- Aynı şifre iki kullanıcıda aynı hash değerini üretmez.
- Hash formatı sürümlüdür.
- İleride parametreler güçlendiğinde eski hash için `SuccessRehashNeeded` döndürebilir.

### Hashleme ile şifreleme arasındaki fark

Şifreleme geri çözülebilir. Şifre hashleme geri çözülemez. Giriş sırasında kayıtlı şifre açılmaz; kullanıcının yazdığı değer hash doğrulama algoritmasıyla karşılaştırılır.

Yeni kullanıcı oluşturulurken:

```csharp
kullanici.Password = _sifreServisi.Hashle(
    kullanici,
    model.Password);
```

Giriş sırasında:

```csharp
var sonuc = _sifreServisi.Dogrula(
    kullanici,
    model.Password);
```

şeklinde çalışır.

---

## 3. Eski düz metin şifreler ne oluyor?

`SeedData.InitializeAsync` uygulama başlarken eski kullanıcı kayıtlarını kontrol eder.

Şifre ASP.NET Identity hash formatında değilse:

1. Mevcut düz metin değer geçici olarak alınır.
2. Güvenli hash üretilir.
3. Aynı `Password` kolonuna hash yazılır.
4. Güvenlik damgası yenilenir.
5. Değişiklik veritabanına kaydedilir.

Bu sayede mevcut test kullanıcılarının parolaları bozulmadan güvenli formata geçirilir.

> Veritabanını güncelledikten ve uygulamayı bir kez çalıştırdıktan sonra `Kullanicilar.Password` kolonunda `123456` gibi düz metin değerler bulunmamalıdır.

---

## 4. Güçlü şifre politikası

Ortak doğrulama attribute'u:

```text
Validation/GucluSifreAttribute.cs
```

Yeni şifre şu kuralları sağlamalıdır:

- En az 8, en fazla 100 karakter
- En az bir büyük harf
- En az bir küçük harf
- En az bir rakam
- Boşluk içermemeli

Bu doğrulama şu alanlarda kullanılır:

- Admin tarafından yeni kullanıcı oluşturma
- Admin tarafından kullanıcı şifresi değiştirme
- Kullanıcının kendi şifresini değiştirmesi
- Üretimde ilk yönetici hesabının oluşturulması

Development test hesaplarının `123456` parolası yalnızca geliştirme kolaylığı için korunmuştur. Üretimde bu hesaplar oluşturulmaz.

---

## 5. Hesap kilitleme

`AuthController` aynı kullanıcı için art arda hatalı girişleri sayar.

```text
5 hatalı deneme → 15 dakika hesap kilidi
```

Yeni kullanıcı alanları:

```text
BasarisizGirisSayisi
KilitBitisTarihi
SonGirisTarihi
SifreDegistirmeTarihi
GuvenlikDamgasi
```

Başarılı girişte hata sayacı ve süresi dolmuş kilit temizlenir.

Admin, kullanıcı düzenleme ekranından kilitli hesabı manuel olarak açabilir:

```text
Kullanıcılar → Düzenle → Kilidi Aç
```

### Hesap kilidi ile rate limit aynı şey değildir

- **Hesap kilidi**, belirli kullanıcı hesabını korur.
- **Rate limit**, tek IP adresinden giriş endpointine aşırı istek gönderilmesini sınırlar.

Bu projede iki koruma birlikte kullanılır.

---

## 6. Login rate limit

`Program.cs` içinde `login` isimli politika tanımlanmıştır.

```text
Bir IP için dakikada en fazla 10 giriş isteği
```

Sınır aşılırsa HTTP `429 Too Many Requests` döner.

Controller üzerindeki kullanım:

```csharp
[EnableRateLimiting(
    GuvenlikSabitleri.LoginRateLimitPolitikasi)]
```

---

## 7. Güvenlik damgası ve eski oturumların iptali

Her kullanıcıda rastgele bir `GuvenlikDamgasi` bulunur. Bu değer JWT içine claim olarak yazılır.

Her doğrulanan istekte:

1. JWT imzası ve süresi kontrol edilir.
2. Kullanıcı veritabanından okunur.
3. Hesabın aktif olup olmadığı kontrol edilir.
4. Token içindeki güvenlik damgası ile veritabanındaki damga karşılaştırılır.

Aşağıdaki işlemlerde damga yenilenir:

- Şifre değişikliği
- E-posta değişikliği
- Ad veya soyad değişikliği
- Sistem rolü değişikliği
- Hesabın aktif/pasif yapılması
- Admin tarafından hesap kilidinin kaldırılması

Böylece daha önce oluşturulmuş JWT'ler anında geçersiz hale gelir.

---

## 8. JWT anahtarı neden appsettings.json içinde değil?

JWT imzalama anahtarı gizli bilgidir. Kaynak kod deposuna eklenmemelidir.

`appsettings.json` yalnızca şu genel ayarları içerir:

```json
"Jwt": {
  "Issuer": "YonetimPaneli",
  "Audience": "YonetimPaneliKullanicilari",
  "AccessTokenMinutes": 120,
  "RememberMeDays": 7
}
```

### Development ortamı

Anahtar tanımlanmazsa uygulama bellekte rastgele bir anahtar üretir. Uygulama yeniden başlatıldığında mevcut oturumların geçersiz olması normaldir.

Kalıcı development anahtarı için proje klasöründe:

```powershell
dotnet user-secrets set "Jwt:Key" "en-az-32-byte-uzunlugunda-rastgele-gizli-anahtar"
```

### Üretim ortamı

PowerShell environment variable örneği:

```powershell
$env:Jwt__Key="buraya-uzun-rastgele-ve-gizli-bir-deger"
```

Linux örneği:

```bash
export Jwt__Key="buraya-uzun-rastgele-ve-gizli-bir-deger"
```

Gerçek sunucuda Azure Key Vault, AWS Secrets Manager, Docker Secret veya hosting sağlayıcısının secret alanı tercih edilmelidir.

---

## 9. Üretimde ilk admin hesabı

Development ortamında test hesapları otomatik oluşturulur. Production ortamında test hesapları oluşturulmaz.

Production veritabanı tamamen boşsa ilk admin secret ayarları üzerinden oluşturulur:

```powershell
$env:BootstrapAdmin__Email="admin@sirket.com"
$env:BootstrapAdmin__Password="GucluSifre123"
$env:BootstrapAdmin__Ad="Sistem"
$env:BootstrapAdmin__Soyad="Yöneticisi"
```

Veritabanında kullanıcı zaten varsa bu değerlerden yeni kullanıcı oluşturulmaz.

---

## 10. Güvenli cookie ayarları

JWT tarayıcı JavaScript'ine verilmez. `HttpOnly` cookie içinde saklanır.

Kullanılan ayarlar:

```text
HttpOnly = true
SameSite = Strict
Secure = production ortamında zorunlu
Path = /
```

`Beni hatırla` seçilmezse cookie bir oturum cookie'sidir ve tarayıcı kapanınca silinir. JWT'nin sunucu tarafındaki süresi yine kontrol edilir.

`Beni hatırla` seçilirse cookie yapılandırmadaki gün sayısı kadar kalıcı olur.

---

## 11. Logout neden POST oldu?

Eski GET çıkış bağlantısı:

```text
/Auth/Logout
```

başka bir sayfadaki resim veya link üzerinden istemeden tetiklenebilirdi.

Yeni layout içinde çıkış işlemi POST formudur. Global anti-forgery filtresi ile CSRF token kontrolü yapılır.

```csharp
[HttpPost]
public IActionResult Logout()
```

GET isteği artık çıkış işlemi gerçekleştirmez.

---

## 12. Global anti-forgery koruması

`Program.cs` içindeki:

```csharp
options.Filters.Add(
    new AutoValidateAntiforgeryTokenAttribute());
```

ayarından sonra tüm güvensiz HTTP metotlarında token kontrol edilir:

```text
POST
PUT
PATCH
DELETE
```

ASP.NET Core Form Tag Helper kullanılan formlara tokenı otomatik ekler.

---

## 13. Ek güvenlik headerları

Uygulama aşağıdaki response headerlarını gönderir:

```text
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
Cross-Origin-Opener-Policy: same-origin
```

Katı Content Security Policy bu aşamada eklenmedi. Çünkü proje hâlâ Tailwind CDN ve inline JavaScript kullanıyor. Aşama 10'da frontend build düzeni kurulurken CSP uyumluluğu tamamlanacaktır.

---

## 14. Güvenlik migrationı

Yeni migration:

```text
Migrations/20260714130000_HesapGuvenligiEkle.cs
```

Uygulama başlangıcında bekleyen migrationlar otomatik uygulanır. İstersen Visual Studio Package Manager Console üzerinden manuel de çalıştırabilirsin:

```powershell
Update-Database
```

Üretimde otomatik migration kullanımı ekibin dağıtım politikasına göre kapatılıp CI/CD adımına taşınabilir. Bu eğitim projesinde kurulumu kolaylaştırmak için açık bırakılmıştır.

---

## 15. Değiştirilen temel dosyalar

```text
Program.cs
Controllers/AuthController.cs
Controllers/KullanicilarController.cs
Models/AppUser.cs
Models/SeedData.cs
Services/SifreServisi.cs
Security/GuvenlikSabitleri.cs
Validation/GucluSifreAttribute.cs
ViewModels/Auth/LoginViewModel.cs
ViewModels/Auth/SifreDegistirViewModel.cs
Views/Auth/Login.cshtml
Views/Auth/SifreDegistir.cshtml
Views/Shared/_Layout.cshtml
Views/Kullanicilar/Duzenle.cshtml
Migrations/20260714130000_HesapGuvenligiEkle.cs
```

---

## 16. Test sırası

### Veritabanı ve hash testi

1. Uygulamayı kapat.
2. `dotnet build` çalıştır.
3. `Update-Database` veya `dotnet ef database update` çalıştır.
4. Uygulamayı başlat.
5. Veritabanında `Kullanicilar.Password` alanını incele.
6. Parolaların düz metin değil uzun hash değerleri olduğunu doğrula.

### Giriş testi

1. Development ortamında `admin@test.com / 123456` ile giriş yap.
2. Çıkış yap.
3. Yanlış şifreyi beş kez dene.
4. Hesabın 15 dakika kilitlendiğini doğrula.
5. Başka admin ile kullanıcı düzenleme ekranından kilidi aç.

### Şifre değiştirme testi

1. Kendi profilini aç.
2. `Şifre Değiştir` butonuna bas.
3. Yanlış mevcut şifreyi dene.
4. Zayıf yeni şifreyi dene.
5. Güçlü şifre ile değiştir.
6. Eski şifrenin çalışmadığını doğrula.
7. Yeni şifre ile giriş yap.

### Oturum iptal testi

1. Bir kullanıcıyla giriş yap.
2. Başka admin oturumundan o kullanıcıyı pasif yap.
3. Kullanıcının sonraki isteğinde login ekranına yönlendirildiğini doğrula.
4. Kullanıcıyı yeniden aktif yap ve tekrar giriş yap.

### Logout/CSRF testi

1. Adres çubuğundan `/Auth/Logout` adresine GET isteği gönder.
2. Bu isteğin çıkış işlemi gerçekleştirmediğini doğrula.
3. Üst bardaki çıkış butonunun POST ile çalıştığını doğrula.

---

## 17. Sık karşılaşılabilecek durumlar

### Uygulama açılırken `Jwt:Key` hatası

Production ortamında secret tanımlanmamıştır. `Jwt__Key` environment variable değerini ekle.

### Uygulama her yeniden başlatıldığında oturum kapanıyor

Development ortamında kalıcı `Jwt:Key` tanımlanmadığında bellek içi anahtar yenilenir. User Secrets kullan.

### Eski kullanıcı giriş yapamıyor

Migrationın uygulandığını ve `SeedData.InitializeAsync` çalıştığını kontrol et. Eski düz metin parola ilk açılışta hashe dönüştürülür.

### Form gönderiminde 400 hatası

Form Tag Helper yerine el ile HTML form yazıldıysa anti-forgery token eksik olabilir. MVC formunda `asp-controller` / `asp-action` kullan veya `@Html.AntiForgeryToken()` ekle.

---

## 18. Sonraki aşama

Aşama 10 son temel aşamadır:

- Server-side görev ve kullanıcı sayfalama
- Veritabanı sorgu performansı
- Tailwind CDN bağımlılığını azaltma / frontend asset düzeni
- Tekrarlanan UI bileşenlerinin toparlanması
- Hata sayfaları ve son erişilebilirlik kontrolü
- Otomatik test altyapısı
- Son proje inceleme raporu
