# AŞAMA 7 — ORGANİZASYON ŞEMASI, DEPARTMAN VE UNVAN YÖNETİMİ

Bu aşamada uygulamadaki `YoneticiId`, `DepartmanId` ve `UnvanId` ilişkileri yalnızca
formlarda kullanılan alanlar olmaktan çıkarılıp gerçek yönetim ekranlarına dönüştürüldü.

## 1. Eklenen ekranlar

### `/Organizasyon/Index`

Tüm giriş yapmış kullanıcılar açabilir.

Bu sayfa:

- yalnızca aktif kullanıcıları gösterir,
- `YoneticiId` ilişkisini kullanarak yönetici–ast ağacı oluşturur,
- çalışanları departman, unvan, kıdem ve organizasyon rolüyle gösterir,
- ad, e-posta, unvan ve departman üzerinden arama yapar,
- departman filtresi uygular,
- tüm dalları açıp kapatabilir,
- bozuk veya kopuk bir hiyerarşi kaydını görünmez bırakmaz.

### `/Organizasyon/Yonetim`

Yalnızca `Admin` sistem rolündeki kullanıcılar açabilir.

Bu sayfada:

- departman eklenir,
- departman adı, açıklaması ve üst departmanı değiştirilir,
- unvan eklenir ve düzenlenir,
- kullanılmayan departman ve unvanlar silinir,
- kullanıcıya veya alt departmana bağlı kayıtların yanlışlıkla silinmesi engellenir.

## 2. ViewModel neden kullanıldı?

Organizasyon sayfasına doğrudan `AppUser` listesi göndermek yerine
`OrganizasyonDugumViewModel` kullanıldı.

Bunun faydaları:

1. View yalnızca ihtiyacı olan alanları bilir.
2. Kullanıcının şifresi gibi hassas alanlar View'a taşınmaz.
3. İç içe `Astlar` listesi ile recursive (özyinelemeli) ağaç kurulabilir.
4. Görünüm metinleri controller içinde merkezi olarak hazırlanır.

## 3. Recursive organizasyon ağacı nasıl çalışıyor?

`OrganizasyonController.Index` içinde önce aktif kullanıcılar alınır.

Ardından kullanıcılar yöneticilerine göre gruplanır:

```csharp
var astGruplari = kullanicilar
    .Where(u => u.YoneticiId.HasValue)
    .GroupBy(u => u.YoneticiId!.Value);
```

Kök kullanıcılar, yöneticisi olmayan veya yöneticisi aktif kullanıcı listesinde
bulunmayan kişilerdir.

`DugumOlustur` metodu bir kullanıcı için ViewModel oluşturur ve aynı işlemi astları
için tekrar çağırır. Buna recursion denir.

Döngü güvenliği için mevcut yol üzerinde görülen kullanıcı ID'leri bir `HashSet<int>`
içinde tutulur. Böylece veritabanında hatalı bir ilişki olsa bile sonsuz döngü oluşmaz.

## 4. Recursive partial view

Ağaç görünümü şu partial dosyalarla oluşturulur:

```text
Views/Organizasyon/_OrganizasyonDugumu.cshtml
Views/Organizasyon/_OrganizasyonKisiKarti.cshtml
```

`_OrganizasyonDugumu.cshtml`, her ast için kendisini tekrar çağırır:

```cshtml
<partial name="_OrganizasyonDugumu" model="altDugum" />
```

Bu yöntem sayesinde hiyerarşi seviyesi sabit değildir. İki, beş veya daha fazla seviye
aynı kodla gösterilebilir.

## 5. Departman döngüsü nasıl engelleniyor?

Örneğin:

```text
Teknoloji
└── Yazılım
    └── Backend
```

`Teknoloji` departmanının üst departmanı `Backend` yapılırsa döngü oluşur.

`DepartmanDongusuOlustururMu` metodu seçilen üst departmandan yukarı doğru ilerler.
İncelenen zincirde düzenlenen departmanın ID'sine ulaşırsa işlem reddedilir.

Aynı departmanın kendisine bağlanması da doğrudan engellenir.

## 6. Güvenli silme kuralları

### Departman

Aşağıdakilerden biri varsa departman silinmez:

- departmana bağlı kullanıcı,
- departmana bağlı alt departman.

### Unvan

Unvan herhangi bir kullanıcıya atanmışsa silinmez.

Bu kurallar hem arayüzde açıklanır hem de controller içinde tekrar kontrol edilir.
Arayüz kontrolüne tek başına güvenilmez; kötü niyetli bir POST isteği backend tarafından
yine engellenir.

## 7. Arama ve filtre mantığı

Organizasyon ağacında filtre yalnızca eşleşen kartı göstermekle kalmaz. Eşleşen çalışanın
üst yöneticileri de görünür bırakılır. Böylece kullanıcının organizasyondaki konumu
kaybolmaz.

JavaScript, ağacı aşağıdan yukarı değerlendirir:

- kart eşleşiyorsa görünür,
- kart eşleşmese bile görünür bir astı varsa görünür,
- görünür astı bulunan dallar otomatik açılır.

Departman ve unvan yönetimindeki aramalar ise kartların `data-management-search`
özelliğini kullanır.

## 8. Yetkilendirme

Controller seviyesinde:

```csharp
[Authorize]
```

bulunur. Bu nedenle organizasyon şeması için giriş yapmak zorunludur.

Yönetim ve POST işlemlerinde ayrıca:

```csharp
[Authorize(Roles = "Admin")]
```

kullanılır. Menü bağlantısının gizlenmesi güvenlik değildir; gerçek güvenlik controller
üzerindeki rol kontrolüdür.

## 9. Bu aşamada değişen temel dosyalar

```text
Controllers/OrganizasyonController.cs

ViewModels/Organizasyon/
├── OrganizasyonDugumViewModel.cs
├── OrganizasyonIndexViewModel.cs
├── OrganizasyonYonetimViewModel.cs
├── DepartmanFormViewModel.cs
├── DepartmanYonetimItemViewModel.cs
├── UnvanFormViewModel.cs
└── UnvanYonetimItemViewModel.cs

Views/Organizasyon/
├── Index.cshtml
├── Yonetim.cshtml
├── _OrganizasyonDugumu.cshtml
└── _OrganizasyonKisiKarti.cshtml

Views/Shared/_Layout.cshtml
Views/_ViewImports.cshtml
wwwroot/css/site.css
```

## 10. Migration gerekiyor mu?

Hayır.

Bu aşamada mevcut veritabanı alanları kullanıldı:

```text
AppUser.YoneticiId
AppUser.DepartmanId
AppUser.UnvanId
Departman.UstDepartmanId
```

Yeni tablo veya kolon eklenmedi.

## 11. Test senaryoları

1. Admin hesabıyla giriş yapın.
2. Sol menüden `Organizasyon` sayfasını açın.
3. Ahmet, Mehmet ve Kemal arasındaki yönetici–ast zincirini kontrol edin.
4. Arama alanına `Kemal` yazın; Kemal ve üst yöneticileri görünür kalmalı.
5. Bir departman filtresi uygulayın.
6. `Tümünü Aç` ve `Tümünü Kapat` butonlarını deneyin.
7. `Birim Yönetimi` sayfasından yeni departman oluşturun.
8. Yeni alt departman oluşturup üst departman seçin.
9. Bir departmanı kendi alt departmanına bağlamaya çalışın; işlem reddedilmeli.
10. Kullanıcısı olan bir departmanı silmeye çalışın; işlem reddedilmeli.
11. Kullanılmayan yeni bir unvan oluşturun, düzenleyin ve silin.
12. Normal kullanıcı hesabıyla `/Organizasyon/Yonetim` adresine gitmeyi deneyin;
    yetkisiz sayfasına yönlenmeli.

## 12. Sonraki aşama

Aşama 8'de kullanıcı profil/detay ekranı, bildirim merkezi ve kişiye özel iş yükü özeti
üzerinde çalışılacaktır.
