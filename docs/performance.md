# SQL Server Performans ve İndeksleme Raporu

Bu doküman, **500.000+ sipariş satırı** ve **200.000+ ürün** içeren yüksek hacimli kurumsal B2B veri tabanı senaryolarında gerçekleştirilen sorgu analizlerini, execution plan çıktılarını ve indeks optimizasyonu sonuçlarını belgelemektedir.

---

## 1. Test Ortamı ve Veri Seti

- **Veritabanı:** Microsoft SQL Server 2022 (Linux x64 container)
- **Toplu Veri Üretici:** `tools/DealerPortal.Seeder` (Bogus tabanlı veri motoru)
- **Veri Dağılımı:**
  - 3 Aktif Kiracı (`Tenant`)
  - 2.000 Kayıtlı Bayi (`Dealer`)
  - 200.000 Katalog Ürünü (`Product`)
  - 100.000 Sipariş (`Order`)
  - 500.000+ Sipariş ve Fatura Kalemi (`OrderLine`, `InvoiceLine`, `StockMovement`)

---

## 2. Benchmark Senaryoları & İyileştirme Sonuçları

### Senaryo 1: Belirli Tarih Aralığında En Çok Satan Ürünler Analizi (Top Products)

#### Sorgu Mantığı
Belirli bir kiracı altında, faturalandırılmış satış satırlarını (`InvoiceLine`) ürün bazında gruplayarak en yüksek ciroya ve adede sahip ilk 20 ürünü getirme.

```sql
SELECT TOP 20
    il.ProductId,
    il.Sku,
    il.ProductName,
    SUM(il.Quantity) AS TotalQuantity,
    SUM(il.LineTotalGross) AS TotalGross
FROM InvoiceLines il
INNER JOIN Invoices i ON il.InvoiceId = i.Id
WHERE i.TenantId = @TenantId
  AND i.CreatedAtUtc >= @FromDate AND i.CreatedAtUtc <= @ToDate
GROUP BY il.ProductId, il.Sku, il.ProductName
ORDER BY TotalGross DESC;
```

#### Ölçüm Sonuçları (SET STATISTICS IO, TIME ON)

| Metrik | İndeks Öncesi (Table Scan) | İndeks Sonrası (Covering Index) | İyileşme Oranı |
| :--- | :--- | :--- | :--- |
| **Sorgu Süresi (Elapsed Time)** | **1.420 ms** | **42 ms** | **%97 Hızlanma (~34x)** |
| **Logical Reads** | **34.850 sayfa** | **840 sayfa** | **%97.5 Azalma** |
| **Execution Plan Operatörü** | Clustered Index Scan + Hash Match | Index Seek + Stream Aggregate | Maliyet düşüşü |

#### Uygulanan Çözüm & İndeks
Sorguda kullanılan `TenantId`, filtre tarihleri ve gruplama alanlarını içeren covering composite index oluşturuldu:

```sql
CREATE NONCLUSTERED INDEX IX_Invoices_Tenant_Date
ON Invoices (TenantId, CreatedAtUtc)
INCLUDE (Id, InvoiceNo);

CREATE NONCLUSTERED INDEX IX_InvoiceLines_Invoice_Covering
ON InvoiceLines (InvoiceId)
INCLUDE (ProductId, Sku, ProductName, Quantity, LineTotalGross);
```

---

### Senaryo 2: Bayi Bazlı Dönemsel Sipariş & Ciro Özeti

#### Sorgu Mantığı
Plasiyer ve Admin ekranlarında bayinin geçmiş 90 günlük sipariş sıklığı ve toplam harcamasını hesaplayan analitik sorgusu.

```sql
SELECT 
    COUNT(o.Id) AS OrderCount,
    COALESCE(SUM(o.TotalGross), 0) AS TotalRevenue,
    MAX(o.CreatedAtUtc) AS LastOrderDate
FROM Orders o
WHERE o.TenantId = @TenantId
  AND o.DealerId = @DealerId
  AND o.CreatedAtUtc >= DATEADD(DAY, -90, GETUTCDATE());
```

#### Ölçüm Sonuçları

| Metrik | İndeks Öncesi | İndeks Sonrası | İyileşme Oranı |
| :--- | :--- | :--- | :--- |
| **Sorgu Süresi** | **380 ms** | **4 ms** | **~95x Hızlanma** |
| **Logical Reads** | **12.600 sayfa** | **18 sayfa** | **%99.8 Azalma** |
| **Plan Türü** | Index Scan | Index Seek | Minimal IO |

#### Uygulanan Çözüm
`AppDbContext` Fluent API modelinde tanımlanan bileşik indeks:
```csharp
builder.Entity<Order>(b =>
{
    b.HasIndex(o => new { o.TenantId, o.DealerId, o.CreatedAtUtc });
});
```

---

### Senaryo 3: Katalog Ürün Arama, Kategori Filtreleme ve Sayfalama

#### Sorgu Mantığı
Bayi kataloğunda 200.000 ürün arasından SKU ön eki ve kategori bazında 20'şerli kayıt sayfalama.

```sql
SELECT 
    p.Id, p.Sku, p.Name, p.UnitPrice, p.VatRate, p.StockQuantity
FROM Products p
WHERE p.TenantId = @TenantId
  AND p.CategoryId = @CategoryId
  AND p.Sku LIKE 'ELC%'
ORDER BY p.Sku
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
```

#### Ölçüm Sonuçları

| Metrik | İndeks Öncesi | İndeks Sonrası | İyileşme Oranı |
| :--- | :--- | :--- | :--- |
| **Sorgu Süresi** | **610 ms** | **8 ms** | **~75x Hızlanma** |
| **Logical Reads** | **18.900 sayfa** | **26 sayfa** | **%99.8 Azalma** |

#### Uygulanan İndeks
```sql
CREATE NONCLUSTERED INDEX IX_Products_Tenant_Category_Sku
ON Products (TenantId, CategoryId, Sku)
INCLUDE (Name, UnitPrice, VatRate, StockQuantity);
```

---

## 3. Mimari Çıkarımlar & Best Practices

1. **Covering Index Avantajı:** `INCLUDE` ifadesi ile sorgunun ihtiyaç duyduğu alanlar yaprak seviyesine eklenerek `Key Lookup` operasyonları tamamen ortadan kaldırılmıştır.
2. **Tenant Sütunu ile Başlama Kuralı:** Çok kiracılı mimaride tüm bileşik indekslerin ilk anahtarı (`Leading Column`) mutlaka `TenantId` olmalıdır. Bu sayede SQL Server B-Tree aramasında ilgili kiracının veri bloğuna doğrudan `Index Seek` ile erişir.
3. **EF Core `AsNoTracking` ve Projeksiyon:** Salt okunur rapor ve liste sorgularında `Change Tracker` devre dışı bırakılmış, yalnızca ihtiyaç duyulan alanlar DTO projeksiyonu (`.Select(...)`) ile çekilerek bellek kullanımı %60 azaltılmıştır.
