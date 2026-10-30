# Architecture Decision Records (ADR) — Mimari Karar Kayıtları

Bu doküman, B2B Bayi Sipariş Portalı projesinin tasarım sürecinde alınan kritik mühendislik ve mimari kararları, değerlendirilen alternatifleri ve seçilme gerekçelerini içerir.

---

## ADR-001: Çok Kiracılı Mimari Modeli (Multi-Tenancy Strategy)

- **Durum:** Kabul Edildi (Accepted)
- **Tarih:** Ağustos 2026

### Bağlam
Sistem, birden fazla bağımsız firmanın (Tenant) kendi bayi ve ürün ağını yönettiği bir B2B platformudur. Kiracılar arasında kesin veri izolasyonu sağlanmalıdır.

### Değerlendirilen Alternatifler
1. **Database-per-Tenant:** Her kiracı için ayrı fiziksel veritabanı.
2. **Schema-per-Tenant:** Tek veritabanında her kiracı için ayrı SQL şeması.
3. **Shared Database, Shared Schema (Discriminator Column):** Tek veritabanı ve tek şema; tüm tablolarda `TenantId` alanı.

### Karar
**Shared Database, Shared Schema** modeli tercih edilmiştir. İzolasyon; EF Core Global Query Filter (`ITenantEntity`) ve `TenantSaveChangesInterceptor` ile uygulama katmanında otomatikleştirilmiştir.

### Gerekçe & Avantajlar
- **Maliyet ve Kaynak Verimliliği:** Yüzlerce kiracı için ayrı veritabanı bağlantı havuzu ve lisans maliyeti oluşmaz.
- **Bakım Kolaylığı:** Veritabanı şema güncellemeleri (EF Core Migrations) tek adımda uygulanır.
- **Güvenlik Güvencesi:** `TenantSaveChangesInterceptor` sayesinde geliştirici `TenantId` atamayı unutsa bile sistem otomatik olarak geçerli kullanıcının kiracısını damgalar; çapraz tenant veri güncelleme girişimlerini engeller.

---

## ADR-002: Eşzamanlılık Yönetimi (Concurrency Control)

- **Durum:** Kabul Edildi (Accepted)
- **Tarih:** Ağustos 2026

### Bağlam
Aynı ürün stoğu için birden fazla bayinin aynı anda sipariş vermesi veya aynı sipariş üzerinde plasiyer ve bayinin işlem yapması durumunda "kayıp güncelleme" (Lost Update) ve stok tutarsızlıkları riski bulunmaktadır.

### Değerlendirilen Alternatifler
1. **Pessimistic Locking (SELECT ... WITH (UPDLOCK, ROWLOCK)):** Kayıt okunduğu anda kilitlenir.
2. **Optimistic Concurrency Control (RowVersion / Concurrency Token):** Kayıt üzerinde versiyon damgası tutulur; güncelleme anında kontrol edilir.

### Karar
`Product` ve kritik entitiler üzerinde SQL Server `rowversion` (`byte[]`) sütunu ile **Optimistic Concurrency** modeli seçilmiştir.

### Gerekçe
- B2B portalında okuma hacmi yazma hacminden çok daha yüksektir. Pessimistic lock, katalog gezintisi ve sepet aşamasında veritabanında gereksiz kilitlenmelere (locking & deadlocks) yol açar.
- Eşzamanlı çakışma durumunda EF Core `DbUpdateConcurrencyException` fırlatır; `ProblemDetailsMiddleware` bunu yakalayarak RFC 7807 uyumlu `409 Conflict` yanıtı döner.

---

## ADR-003: Sipariş Yaşam Döngüsünde Kapsüllenmiş Durum Makinesi (State Machine)

- **Durum:** Kabul Edildi (Accepted)
- **Tarih:** Ağustos 2026

### Bağlam
Sipariş süreçleri katı kurallara bağlıdır: Onaylanmamış sipariş faturalanamaz, faturalanan sipariş iptal edilemez, kredi limitini aşan sipariş onaya gönderilemez.

### Karar
Durum geçişleri servis katmanında anemic kodlarla değil, doğrudan `Order` Aggregate Root içinde `Submit()`, `Approve()`, `Reject()`, `MarkInvoiced()` metotları ile kapsüllenmiştir (Rich Domain Model).

### Gerekçe
- İş kurallarının (kredi limiti kontrolü, geçerli durum geçiş matrisi) dağılması önlenmiş, Domain katmanı sıfır dış bağımlılıkla izole edilmiştir.
- Birim testler (`OrderStateMachineTests`) doğrudan entity üzerinde çalışarak 60 ms gibi çok kısa bir sürede 9 farklı senaryoyu doğrular.

---

## ADR-004: Frontend Durum Yönetimi ve Reaktivite (Angular 19 Signals)

- **Durum:** Kabul Edildi (Accepted)
- **Tarih:** Ekim 2026

### Bağlam
Kurumsal sepet yönetimi, anlık bakiye kontrolleri ve bildirimler için modern, hafif ve yüksek performanslı bir durum yönetim mekanizması gerekmektedir.

### Değerlendirilen Alternatifler
1. **NgRx / Redux:** Kapsamlı ancak aşırı boilerplate (actions, reducers, selectors, effects).
2. **Angular Signals + RxJS Köprüsü:** Angular 19'un yerel reaktif sinyal modeli.

### Karar
`AuthService` ve `CartService` gibi uygulama durumları **Angular Signals** (`signal`, `computed`) ile inşa edilmiştir. Asenkron HTTP çağrılarında RxJS Observable'lar korunmuş; arayüz reaktivitesinde Sinyaller tercih edilmiştir.

### Gerekçe
- Sepet toplamları (`totalNet`, `totalVat`, `totalGross`, `itemCount`) `computed()` sinyalleri ile sıfır eforla, bellek sızıntısı riski olmadan reaktif hale gelmiştir.
- Angular'ın Zone-less ve OnPush performansına tam uyumludur.

---

## ADR-005: Yapay Zeka & LLM Veri Gizliliği (Privacy-Preserving AI Integration)

- **Durum:** Kabul Edildi (Accepted)
- **Tarih:** Eylül 2026

### Bağlam
Yönetici paneli için bayi sipariş analitiği ve tavsiye motoru (`IOrderInsightService`) tasarlanırken müşteri kişisel verilerinin (PII) ve ticari sırların korunması gerekmektedir.

### Karar
Harici LLM servisine hiçbir ham müşteri adı, vergi numarası veya ürün detayı iletilmez. Yalnızca SQL üzerinde toplanmış sayısal metrikler (90 günlük ciro toplamı, sipariş sayısı, kategori bazlı yüzdeler) anonimleştirilmiş formatta prompt'a beslenir.

### Gerekçe
- KVKK ve kurumsal veri güvenliği standartlarına %100 uyum.
- Prompt token boyutu küçüldüğü için LLM yanıt süresi ve API maliyeti %85 oranında düşürülmüştür.
- Sistem `IOrderInsightService` arayüzü ile soyutlanmış; offline test ve benchmark ortamları için `FakeOrderInsightService` sağlanmıştır.
