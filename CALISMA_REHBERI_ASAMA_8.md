# AŞAMA 8 — KULLANICI PROFİLİ, KİŞİSEL İŞ YÜKÜ VE BİLDİRİM MERKEZİ

Bu aşamada kullanıcı kayıtları yalnızca yönetim tablosunda görülen satırlar olmaktan
çıkarıldı. Her kullanıcı için kurumsal profil sayfası, görev iş yükü özeti ve kalıcı
uygulama içi bildirim sistemi eklendi.

## 1. Kullanıcı profil ekranı

Yeni adres:

```text
/Profil/Index
/Profil/Index/4
```

`id` verilmezse giriş yapan kullanıcının kendi profili açılır. `id` verilirse ilgili
kullanıcının kurumsal profili gösterilir.

Profil ekranında:

- ad ve e-posta,
- departman,
- unvan,
- kıdem,
- organizasyon rolü,
- sistem rolü,
- hesap durumu,
- yönetici,
- doğrudan astlar

gösterilir.

Organizasyon şeması, kullanıcı yönetimi, görev kartları ve görev detayındaki kişi adları
profil sayfasına bağlandı.

## 2. İş yükü gizliliği

Temel organizasyon bilgileri giriş yapmış kullanıcılara açıktır. Fakat kişisel görev
metrikleri herkese gösterilmez.

İş yükünü görebilenler:

1. Kullanıcının kendisi,
2. `Admin` sistem rolündeki kullanıcılar,
3. Genel müdür,
4. Hiyerarşi kurallarına göre ilgili kullanıcıya görev verebilen yöneticiler.

Bu kontrol yalnızca View içinde yapılmaz. `ProfilController` içinde backend tarafında
hesaplanır ve `IsYukuGorebilirMi` alanı ile View'a gönderilir.

## 3. Kişisel iş yükü nasıl hesaplanıyor?

Profil ekranı kullanıcıya atanmış görevleri ilişkisel `AtananKullaniciId` alanından ve
eski kayıtlarla uyumluluk için `AtananUserId` alanından bulur.

Hesaplanan metrikler:

```text
Toplam atanan görev
Aktif görev
Geciken görev
Bugün biten görev
Kritik aktif görev
Tamamlanan görev
Bu hafta tamamlanan görev
Tamamlanma oranı
Kullanıcının oluşturduğu görev sayısı
```

Durum dağılımı ayrıca progress bar olarak gösterilir:

```text
Açık
Devam Ediyor
QA / Test
Bug / Hata
Tamamlandı
```

`GorevZamanServisi` kullanıldığı için gecikme ve kalan gün hesapları görev listesi ile
aynı kuralları izler.

## 4. Bildirim modeli

Yeni tabloyu temsil eden model:

```text
Models/Bildirim.cs
```

Temel alanlar:

```csharp
public int KullaniciId { get; set; }
public int? GorevId { get; set; }
public string Tur { get; set; }
public string Baslik { get; set; }
public string Mesaj { get; set; }
public string? Link { get; set; }
public string? TekilAnahtar { get; set; }
public bool OkunduMu { get; set; }
public DateTime OlusturulmaTarihi { get; set; }
public DateTime? OkunmaTarihi { get; set; }
```

Bildirim görev silinse bile geçmişte kalabilir. Bu nedenle `GorevId` nullable tutulur ve
görev silindiğinde foreign key `SetNull` davranışı uygular.

## 5. Bildirim türleri

Merkezi sabit dosyası:

```text
Models/Enums/BildirimTurleri.cs
```

Eklenen türler:

```text
GorevAtandi
GorevDevredildi
DurumDegisti
YorumEklendi
SonTarihYaklasiyor
GorevBugunBitiyor
GorevGecikti
```

String değerler controller dosyalarında elle tekrar yazılmadığı için yazım farklılıkları
ve filtre hataları azalır.

## 6. Bildirim servisi

```text
Services/BildirimServisi.cs
```

Servisin görevleri:

- kullanıcıya bildirim oluşturmak,
- görev oluşturan ve atanan kişiyi tekilleştirmek,
- işlemi yapan kullanıcıya kendi işlemi için bildirim göndermemek,
- yaklaşan ve geciken görev uyarılarını üretmek,
- aynı tarih uyarısının tekrar üretilmesini engellemek,
- bildirim modelini UI ViewModel'ine dönüştürmek,
- okunmamış bildirim sayısını hesaplamak.

## 7. Tekil bildirim anahtarı

Sayfa her açıldığında gecikmiş görev sorgulanabilir. Tekilleştirme yapılmazsa aynı görev
için yüzlerce bildirim oluşabilir.

Bu nedenle zaman uyarıları için örneğin şu anahtar üretilir:

```text
gorev:15:gecikti:20260720
```

`TekilAnahtar` alanında filtreli unique index bulunur. Aynı olay ikinci kez eklenemez.
`NULL` değerler ise normal görev ve yorum bildirimleri için kullanılabilir.

## 8. Bildirim menüsü neden ViewComponent?

Bildirim zili bütün uygulama sayfalarında layout içinde görünür. Her controller'a ayrı ayrı
bildirim listesi eklemek yerine şu ViewComponent kullanıldı:

```text
ViewComponents/BildirimMenuViewComponent.cs
Views/Shared/Components/BildirimMenu/Default.cshtml
```

ViewComponent:

1. JWT claim içinden aktif kullanıcı ID'sini alır.
2. Eksik tarih uyarılarını oluşturur.
3. Okunmamış sayısını hesaplar.
4. Son beş bildirimi getirir.
5. Layout içindeki zil menüsünü oluşturur.

Layout çağrısı:

```cshtml
@await Component.InvokeAsync("BildirimMenu")
```

## 9. Bildirim merkezi

Yeni adres:

```text
/Bildirim/Index
```

Bu sayfada:

- tüm bildirimler,
- yalnızca okunmamış bildirimler,
- toplam ve okunmamış sayıları,
- göreve gitme butonu,
- tümünü okundu yap işlemi

bulunur.

Bildirim açma işlemi POST olarak çalışır. Önce bildirimin gerçekten giriş yapan
kullanıcıya ait olduğu kontrol edilir, sonra okundu yapılır ve güvenli bir yerel adrese
yönlendirilir.

`Url.IsLocalUrl` kontrolü harici siteye kötü niyetli yönlendirme yapılmasını engeller.

## 10. Hangi işlemler bildirim oluşturuyor?

### Yeni görev

Görev başka bir kullanıcıya atanmışsa atanan kullanıcıya bildirim gider.

### Görev devri

Yeni atanan kullanıcıya ve gerekiyorsa görevi oluşturana bildirim gider.

### Durum değişikliği

İşlemi yapan kişi dışındaki görev katılımcılarına bildirim gider.

### Yorum

Göreve yorum yazıldığında görevi oluşturan ve atanan kişi bilgilendirilir. Yorumu yazan
kişiye kendi yorumu için bildirim gönderilmez.

### Tarih uyarıları

Atanan görev:

- üç gün içinde bitecekse,
- bugün bitecekse,
- gecikmişse

bildirim oluşturulur.

## 11. ViewModel yapısı

### Profil

```text
ViewModels/Profil/
├── ProfilIndexViewModel.cs
├── ProfilKullaniciOzetViewModel.cs
├── ProfilGorevOzetViewModel.cs
└── ProfilAktiviteOzetViewModel.cs
```

### Bildirim

```text
ViewModels/Bildirimler/
├── BildirimIndexViewModel.cs
├── BildirimListeItemViewModel.cs
└── BildirimMenuViewModel.cs
```

ViewModel kullanılması veritabanı modelinin ve kullanıcı şifresi gibi ilgisiz alanların
View'a gönderilmesini engeller.

## 12. Veritabanı indeksleri

Bildirim listesi en çok kullanıcı, okunma durumu ve tarihe göre sorgulanır. Bu nedenle:

```text
KullaniciId + OkunduMu + OlusturulmaTarihi
```

birleşik indeksi eklendi.

Göreve bağlı bildirimler için `GorevId`, tekrar uyarıları engellemek için
`TekilAnahtar` indeksi bulunur.

## 13. Bu aşamada eklenen temel dosyalar

```text
Models/Bildirim.cs
Models/Enums/BildirimTurleri.cs
Services/BildirimServisi.cs
Controllers/ProfilController.cs
Controllers/BildirimController.cs
ViewComponents/BildirimMenuViewComponent.cs

ViewModels/Profil/
ViewModels/Bildirimler/

Views/Profil/Index.cshtml
Views/Bildirim/Index.cshtml
Views/Shared/Components/BildirimMenu/Default.cshtml

Migrations/20260714110000_BildirimMerkeziEkle.cs
Migrations/20260714110000_BildirimMerkeziEkle.Designer.cs
```

Değiştirilen önemli dosyalar:

```text
Models/AppDbContext.cs
Program.cs
Controllers/GorevController.cs
Controllers/KullanicilarController.cs
Views/Shared/_Layout.cshtml
Views/Gorev/Detay.cshtml
Views/Gorev/_GorevKartlari.cshtml
Views/Gorev/_GorevTablosu.cshtml
Views/Organizasyon/_OrganizasyonKisiKarti.cshtml
Views/Kullanicilar/_KullaniciKartlari.cshtml
Views/Kullanicilar/_KullaniciTablosu.cshtml
Views/_ViewImports.cshtml
wwwroot/css/site.css
```

## 14. Migration zorunlu

Yeni `Bildirimler` tablosu eklendiği için uygulamayı açmadan önce:

```powershell
dotnet build
```

Package Manager Console:

```powershell
Update-Database
```

CLI kullanılıyorsa:

```powershell
dotnet ef database update
```

Ardından:

```powershell
dotnet run
```

## 15. Test senaryoları

1. `Update-Database` komutunu çalıştırın.
2. Admin hesabıyla giriş yapın ve sağ üstteki bildirim zilini kontrol edin.
3. Başka bir kullanıcıya görev atayın.
4. Atanan kullanıcıyla giriş yapın; yeni görev bildirimi görünmeli.
5. Bildirime basın; bildirim okundu olmalı ve görev detayı açılmalı.
6. Göreve yorum yazın; diğer katılımcı bildirim almalı.
7. Görev durumunu değiştirin; işlemi yapmayan katılımcı bildirim almalı.
8. Son tarihi üç gün içinde olan bir görev oluşturun; atanan kullanıcıda tarih uyarısı
   oluşmalı.
9. Sayfayı birkaç kez yenileyin; aynı tarih uyarısı çoğalmamalı.
10. `Bildirimler` sayfasında `Okunmamış` filtresini deneyin.
11. `Tümünü okundu yap` butonunu kullanın.
12. Sol menüden `Profilim` sayfasını açın.
13. Admin olarak bir çalışanın profilini açın; iş yükü metrikleri görünmeli.
14. Normal çalışan olarak yetkisi olmayan başka bir profili açın; temel bilgiler görünmeli,
    iş yükü gizli kalmalı.
15. Organizasyon şemasındaki kişi adına basınca profil açılmalı.
16. Görev kartındaki atanan kişi adına basınca ilgili profil açılmalı.

## 16. Sonraki aşama

Aşama 9'da güvenlik ve üretime hazırlık yapılacaktır:

- düz metin parolaların hashlenmesi,
- güvenli giriş ve parola doğrulama,
- JWT secret yönetimi,
- POST logout,
- login hata ve kilitleme deneyimi,
- development test hesaplarının üretimde gizlenmesi.
