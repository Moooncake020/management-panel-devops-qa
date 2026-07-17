# AŞAMA 5 — Görev Detayı, Yorumlar ve Aktivite Geçmişi

Bu aşamada görev listesi ile görev düzenleme ekranı arasına gerçek bir **görev detay sayfası** eklendi.
Göreve erişim yetkisi olan kullanıcılar görev bilgilerini tek ekranda görebilir, yorum yazabilir ve görevde yapılan değişiklikleri zaman çizelgesinden takip edebilir.

> Bu aşama yeni veritabanı tabloları eklediği için projeyi çalıştırmadan önce `Update-Database` uygulanmalıdır.

---

## 1. Eklenen veritabanı modelleri

### `Models/GorevYorum.cs`

Bir göreve yazılan yorumu temsil eder.

Önemli alanlar:

- `GorevId`: Yorumun bağlı olduğu görev.
- `KullaniciId`: Yorumu yazan kullanıcı.
- `Icerik`: En fazla 2000 karakterlik yorum metni.
- `SilindiMi`: Yorum fiziksel olarak silinmez; geçmişin korunması için pasif işaretlenir.
- `SilinmeTarihi`: Yorumun ne zaman kaldırıldığını saklar.

Yorumları fiziksel silmek yerine soft-delete kullanmamızın nedeni görev geçmişini ve denetlenebilirliği korumaktır.

### `Models/GorevAktivite.cs`

Görev üzerinde gerçekleşen önemli işlemleri saklar.

Örnek aktivite türleri:

- Görev oluşturuldu.
- Durum değişti.
- Atanan kişi değişti.
- İçerik güncellendi.
- Görsel değiştirildi.
- Yorum eklendi veya kaldırıldı.
- Görev toplu olarak devredildi.

`EskiDeger` ve `YeniDeger` alanları, durum veya atanan kişi gibi değişikliklerin önceki ve sonraki değerlerini göstermek için kullanılır.

---

## 2. Aktivite türleri neden sabit sınıfta?

Dosya:

```text
Models/Enums/GorevAktiviteTurleri.cs
```

Controller içinde her yerde serbest metin yazmak yerine merkezi sabitler kullanılır:

```csharp
GorevAktiviteTurleri.DurumDegisti
```

Böylece yazım hataları azalır ve View tarafında hangi aktiviteye hangi ikonun gösterileceği güvenilir biçimde belirlenir.

---

## 3. `GorevAktiviteServisi` nasıl çalışıyor?

Dosya:

```text
Services/GorevAktiviteServisi.cs
```

Servis aktiviteyi `DbContext` içine ekler fakat kendi başına `SaveChanges` çağırmaz.

Bunun önemli bir nedeni vardır:

```text
Görev değişikliği
+
Aktivite kaydı
=
Aynı veritabanı işlemi
```

Örneğin görev durumu değiştirildiğinde hem görev hem aktivite aynı `SaveChanges` çağrısıyla kaydedilir. Görev güncellenemezse aktivite de kaydedilmez.

Yeni bir görev henüz ID almamışken servis şu overload'u kullanır:

```csharp
Ekle(Gorev gorev, ...)
```

Entity Framework görev kaydedildiğinde oluşan ID'yi aktivite kaydına otomatik olarak aktarır.

Mevcut görevlerde ise:

```csharp
Ekle(int gorevId, ...)
```

kullanılır.

---

## 4. Görev detay ViewModel yapısı

Yeni dosyalar:

```text
ViewModels/Gorevler/
├── GorevDetayViewModel.cs
├── GorevYorumEkleViewModel.cs
├── GorevYorumListeItemViewModel.cs
└── GorevAktiviteListeItemViewModel.cs
```

`GorevDetayViewModel`, View'a veritabanı entity'sini doğrudan göndermek yerine ekranın ihtiyaç duyduğu verileri taşır:

- Görev başlığı ve açıklaması
- Atanan kişi ve görevi veren
- Düzenleme/silme yetkileri
- Yorum listesi
- Yeni yorum formu
- Aktivite listesi

Bu yaklaşım overposting riskini azaltır ve View kodunu sadeleştirir.

---

## 5. Görev detay yetkisi

`GorevController.GoreviGorebilirMi` metodu şu kullanıcıların görev detayını görmesine izin verir:

1. Admin
2. Genel Müdür
3. Görevin atandığı kullanıcı
4. Görevi oluşturan kullanıcı
5. Organizasyon hiyerarşisine göre görevin atandığı kişiyi yönetebilen kullanıcı

Sadece URL'deki görev ID'sini değiştirmek yetmez; controller her istekte sunucu tarafında yetki kontrolü yapar.

---

## 6. Yorum ekleme akışı

Endpoint:

```text
POST /Gorev/YorumEkle
```

İşlem sırası:

1. Oturumdaki kullanıcı ID'si alınır.
2. Görev bulunur.
3. Kullanıcının görevi görme yetkisi kontrol edilir.
4. Yorumun başındaki ve sonundaki boşluklar temizlenir.
5. En az 2, en fazla 2000 karakter doğrulaması yapılır.
6. Yorum kaydı oluşturulur.
7. Aynı işlemde `YorumEklendi` aktivitesi oluşturulur.
8. Tek `SaveChanges` ile ikisi birlikte kaydedilir.

Yalnızca boşluklardan oluşan yorumlar kabul edilmez.

---

## 7. Yorum silme neden soft-delete?

Endpoint:

```text
POST /Gorev/YorumSil/{id}
```

Yorumu şu kişiler kaldırabilir:

- Yorumu yazan kişi
- Görev üzerinde tam düzenleme yetkisi olan kişi

Kayıt veritabanından silinmez:

```csharp
yorum.SilindiMi = true;
yorum.SilinmeTarihi = DateTime.Now;
```

Bu işlem ayrıca görev zaman çizelgesine kaydedilir.

---

## 8. Görev değişiklikleri nasıl kaydediliyor?

`GorevController.Duzenle` içinde güncellemeden önce eski değerler tutulur:

```csharp
var eskiDurum = gorev.Durum;
var eskiAtananKullaniciId = gorev.AtananKullaniciId;
```

Form değerleri uygulandıktan sonra eski ve yeni değerler karşılaştırılır.

Değişiklik varsa ilgili aktivite eklenir:

```csharp
GorevAktiviteTurleri.DurumDegisti
GorevAktiviteTurleri.AtananDegisti
GorevAktiviteTurleri.IcerikGuncellendi
GorevAktiviteTurleri.GorselGuncellendi
```

Form gönderildiği hâlde hiçbir değer değişmemişse gereksiz aktivite kaydı oluşmaz.

---

## 9. Görev devretme geçmişi

`KullanicilarController` üzerinden görevler toplu şekilde başka kullanıcıya devredildiğinde her görev için ayrı aktivite oluşturulur.

Aktivitede:

- Önceki kullanıcı
- Yeni kullanıcı
- İşlemi yapan admin
- İşlem tarihi

saklanır.

---

## 10. View ve partial yapısı

Yeni ekran:

```text
Views/Gorev/Detay.cshtml
```

Yeni partial'lar:

```text
Views/Gorev/_GorevYorumlar.cshtml
Views/Gorev/_GorevAktiviteZamanCizelgesi.cshtml
```

Detay sayfası iki ana bölüme ayrılır:

```text
Sol alan
├── Görev açıklaması
├── Görev görseli
└── Yorumlar

Sağ alan
├── Görev bilgileri
└── Aktivite geçmişi
```

Küçük ekranlarda bu alanlar alt alta gelir. Geniş ekranda sağ alan sticky çalışır.

---

## 11. Migration

Eklenen migration:

```text
Migrations/20260714073000_GorevYorumVeAktiviteEkle.cs
```

Oluşturduğu tablolar:

```text
GorevYorumlari
GorevAktiviteleri
```

Migration ayrıca eski görevler için birer `SistemKaydi` aktivitesi oluşturur. Böylece önceden var olan görevlerin zaman çizelgesi tamamen boş görünmez.

### Uygulama sırası

Visual Studio Package Manager Console:

```powershell
Update-Database
```

veya terminal:

```powershell
dotnet ef database update
```

Ardından:

```powershell
dotnet build
dotnet run
```

---

## 12. Test senaryoları

### Test 1 — Yeni görev

1. Yeni görev oluştur.
2. Kaydetme sonrası görev detay sayfası açılmalı.
3. Aktivite geçmişinde “Görev oluşturuldu” görünmeli.

### Test 2 — Durum değişikliği

1. Görevi düzenle.
2. Durumu izinli başka bir duruma geçir.
3. Detay sayfasında önceki ve yeni durum görünmeli.

### Test 3 — Atanan kişi değişikliği

1. Yetkili kullanıcıyla görevin atanan kişisini değiştir.
2. Zaman çizelgesinde önceki ve yeni kişi görünmeli.

### Test 4 — Yorum

1. Detay sayfasına yorum ekle.
2. Yorum listede görünmeli.
3. Görev listesinde yorum sayısı artmalı.
4. Aktivite geçmişinde yorum eklendi kaydı görünmeli.

### Test 5 — Boş yorum

1. Yalnızca boşluk içeren yorum gönder.
2. Form hata göstermeli ve kayıt oluşmamalı.

### Test 6 — Yorum kaldırma

1. Kendi yorumunu kaldır.
2. Yorum listeden kaybolmalı.
3. Aktivite geçmişinde yorum kaldırıldı kaydı görünmeli.

### Test 7 — Yetkisiz erişim

1. Kullanıcının göremediği bir görev ID'sini URL'de elle yaz.
2. Yetkisiz sayfasına yönlendirilmelidir.

---

## 13. Bu aşamadan sonra

Sıradaki mantıklı geliştirme grubu:

- Görev önceliği
- Başlangıç ve son tarih
- Geciken görev hesabı
- Dashboard metrikleri
- Görev detayında son tarih ve öncelik gösterimi
- Takvim ve Kanban altyapısına hazırlık
