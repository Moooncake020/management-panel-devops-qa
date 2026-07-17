# Aşama 1 — Responsive UI Temeli ve Kullanıcı Ekranı

Bu sürüm, UI/UX analiz raporundaki en görünür iki problemi çözer:

1. Sidebar küçültüldüğünde ikonların üst üste binmesi.
2. Kullanıcı listesinde işlem butonuna gidildiğinde hangi kullanıcı üzerinde işlem yapıldığının kaybolması.

## Değiştirilen dosyalar

### `Views/Shared/_Layout.cshtml`

Bu dosya uygulamanın ortak kabuğudur.

Eklenenler:

- Mobil menü ve masaüstü daraltma işlemleri ayrıldı.
- Masaüstü daraltma butonu sidebar'ın dış kenarına alındı.
- Sidebar'ın açık/kapalı durumu `localStorage` içinde saklandı.
- Klavye kullanıcıları için “Ana içeriğe geç” bağlantısı eklendi.
- Sidebar butonlarına `aria-label`, `aria-expanded` ve `aria-controls` eklendi.
- `@RenderSectionAsync("Scripts")` eklenerek sayfaların kendi JavaScript kodlarını düzenli biçimde eklemesi sağlandı.

Öğrenilecek kavramlar:

- Layout mantığı
- Responsive sidebar
- DOM class yönetimi
- `localStorage`
- Erişilebilirlik nitelikleri

### `wwwroot/css/site.css`

Ortak görsel davranışlar bu dosyaya taşındı.

Eklenenler:

- Renk değişkenleri
- Klavye focus stilleri
- Sidebar açık/dar durumları
- Sağdan açılan drawer animasyonu
- İşlem menüsü stilleri
- `prefers-reduced-motion` desteği

Öğrenilecek kavramlar:

- CSS custom properties
- Responsive CSS
- Component benzeri ortak sınıflar
- Animasyon erişilebilirliği

### `Views/Kullanicilar/Index.cshtml`

Ana kullanıcı sayfası sadeleştirildi.

Eklenenler:

- Yeni kullanıcı formu sayfanın üzerinden kaldırıldı.
- “Yeni kullanıcı” butonu drawer açıyor.
- Metin araması eklendi.
- Tümü / Aktif / Pasif / Admin hızlı filtreleri eklendi.
- Filtre sonucunda kaç benzersiz kullanıcı kaldığı gösteriliyor.
- İşlem menülerinin aynı anda birden fazla açık kalması önlendi.

Öğrenilecek kavramlar:

- Partial view kullanımı
- Client-side filtreleme
- Dataset alanları
- Drawer focus trap
- Sayfaya özel `Scripts` section

### `Views/Kullanicilar/_YeniKullaniciDrawer.cshtml`

Yeni kullanıcı formu ayrı partial view içine taşındı.

Önemli karar:

Controller yeni kullanıcıyı her zaman aktif oluşturduğu için, eski formdaki yanıltıcı Aktif/Pasif seçimi kaldırıldı.

Eklenenler:

- Bütün alanlara görünür label
- Şifre göster/gizle
- Form alanlarının anlamlı gruplara ayrılması
- Dialog semantiği
- Mobil uyumlu drawer

### `Views/Kullanicilar/_KullaniciKartlari.cshtml`

1536 px altındaki ekranlarda kart görünümü kullanılır.

Neden?

Sidebar açıkken 1024–1440 px aralığındaki ekranlarda geniş tablo okunamıyordu. Kart görünümü sayesinde kullanıcı adı, pozisyon, hiyerarşi ve işlemler aynı bağlam içinde kalır.

### `Views/Kullanicilar/_KullaniciTablosu.cshtml`

Tablo yalnızca çok geniş ekranlarda gösterilir.

Eski dokuz kolon yerine beş anlamlı grup kullanılır:

1. Kullanıcı
2. Pozisyon
3. Hiyerarşi
4. Yetki / Durum
5. İşlemler

Riskli ve ikincil işlemler üç nokta menüsüne taşındı.

## Test planı

1. Projeyi Visual Studio ile açın.
2. Çalışan uygulama varsa durdurun.
3. `dotnet build` veya Visual Studio Build çalıştırın.
4. Admin hesabıyla giriş yapın.
5. Sidebar'ı küçültüp büyütün.
6. Sayfa değiştirin; sidebar tercihi korunmalı.
7. Tarayıcı genişliğini küçültün; kullanıcılar kart görünümüne geçmeli.
8. Yeni kullanıcı butonuna basın; drawer açılmalı.
9. Escape tuşuna basın; drawer kapanmalı.
10. Arama ve Aktif/Pasif/Admin filtrelerini deneyin.
11. Bir kullanıcı kartındaki üç nokta menüsünü açın.
12. Başka kullanıcı menüsü açıldığında önceki menü kapanmalı.

## Bu aşamada bilerek yapılmayanlar

Aşağıdaki işler sonraki aşamalarda yapılacak:

- Server-side validation ve alan bazlı hata mesajları
- E-posta benzersizliği
- Şifre hashleme
- Özel onay modalı
- Görevler sayfasının kart/tablo responsive tasarımı
- Login hata mesajı ve production test hesabı kontrolü
- Gelişmiş server-side filtreleme ve pagination

## Sonraki aşama

**Aşama 2 — Form doğrulama ve kullanıcı oluşturma güvenliği**

Planlananlar:

- Kullanıcı oluşturma/düzenleme ViewModel'leri
- `ModelState` kontrolü
- E-posta benzersizlik doğrulaması
- Alan bazlı Türkçe hata mesajları
- Şifre kuralları
- Drawer'ın hatalı form gönderiminden sonra tekrar açık gelmesi
