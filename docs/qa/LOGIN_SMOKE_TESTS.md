\# Login Smoke Testleri



\## Doküman Bilgileri



| Alan | Değer |

|---|---|

| Modül | Kimlik doğrulama |

| Test türü | Manuel smoke test |

| Ortam | Local |

| Test eden | Kemal Kapak |

| Durum | Hazırlanıyor |



\## Durum Değerleri



\- `PASS`: Test başarılı

\- `FAIL`: Test başarısız

\- `BLOCKED`: Test başka bir problem nedeniyle uygulanamadı

\- `NOT RUN`: Test henüz çalıştırılmadı



\## Test Senaryoları



\### LOGIN-SMOKE-001 — Giriş sayfası açılmalı



\*\*Ön koşul\*\*



Uygulama çalışıyor olmalı.



\*\*Adımlar\*\*



1\. Tarayıcıyı aç.

2\. Giriş sayfasının adresine git.

3\. Sayfanın yüklenmesini bekle.



\*\*Beklenen sonuç\*\*



\- Giriş sayfası hata vermeden açılmalı.

\- E-posta veya kullanıcı adı alanı görünmeli.

\- Parola alanı görünmeli.

\- Giriş butonu görünmeli.



\*\*Durum:\*\* `NOT RUN`



\---



\### LOGIN-SMOKE-002 — Geçerli bilgilerle giriş yapılabilmeli



\*\*Ön koşul\*\*



Sistemde aktif bir test kullanıcısı bulunmalı.



\*\*Adımlar\*\*



1\. Geçerli kullanıcı bilgilerini gir.

2\. Giriş butonuna bas.



\*\*Beklenen sonuç\*\*



\- Kullanıcı sisteme giriş yapmalı.

\- Kullanıcı dashboard veya yetkili ana sayfaya yönlendirilmeli.

\- Giriş sayfasında kalmamalı.



\*\*Durum:\*\* `NOT RUN`



\---



\### LOGIN-SMOKE-003 — Hatalı parola reddedilmeli



\*\*Ön koşul\*\*



Sistemde kayıtlı bir kullanıcı bulunmalı.



\*\*Adımlar\*\*



1\. Geçerli kullanıcı adını veya e-postayı gir.

2\. Hatalı bir parola gir.

3\. Giriş butonuna bas.



\*\*Beklenen sonuç\*\*



\- Kullanıcı sisteme alınmamalı.

\- Anlaşılır bir hata mesajı gösterilmeli.

\- Parola ekranda açık biçimde görünmemeli.



\*\*Durum:\*\* `NOT RUN`



\---



\### LOGIN-SMOKE-004 — Boş alanlarla giriş engellenmeli



\*\*Adımlar\*\*



1\. Kullanıcı adı/e-posta alanını boş bırak.

2\. Parola alanını boş bırak.

3\. Giriş butonuna bas.



\*\*Beklenen sonuç\*\*



\- Form gönderilmemeli veya sunucu isteği reddetmeli.

\- Zorunlu alan uyarıları gösterilmeli.

\- Uygulama hata sayfasına düşmemeli.



\*\*Durum:\*\* `NOT RUN`



\---



\### LOGIN-SMOKE-005 — Yetkisiz kullanıcı korumalı sayfaya erişememeli



\*\*Ön koşul\*\*



Kullanıcı giriş yapmamış olmalı.



\*\*Adımlar\*\*



1\. Tarayıcı adres çubuğuna korumalı bir sayfanın adresini yaz.

2\. Sayfaya git.



\*\*Beklenen sonuç\*\*



\- Kullanıcı korumalı içeriği görememeli.

\- Giriş sayfasına veya yetkisiz erişim sayfasına yönlendirilmeli.



\*\*Durum:\*\* `NOT RUN`



\---



\### LOGIN-SMOKE-006 — Kullanıcı çıkış yapabilmeli



\*\*Ön koşul\*\*



Kullanıcı sisteme giriş yapmış olmalı.



\*\*Adımlar\*\*



1\. Çıkış butonuna bas.

2\. Tarayıcı geri butonuyla korumalı sayfaya dönmeyi dene.



\*\*Beklenen sonuç\*\*



\- Kullanıcının oturumu kapanmalı.

\- Korumalı sayfa tekrar görüntülenmemeli.

\- Gerekirse giriş sayfasına yönlendirilmeli.



\*\*Durum:\*\* `NOT RUN`

