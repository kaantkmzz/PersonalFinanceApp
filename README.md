# 💜 Finans Takip (PersonalFinanceApp)

Cüzdan ve Kasa'yı tek defterde tutan, Windows için masaüstü kişisel finans takip uygulaması. C# / WinForms ile yazıldı, PostgreSQL üzerinde çalışır.

Tanıtım sitesi (canlı demo dahil): **https://kaantkmzz.github.io/PersonalFinanceApp/**

## Özellikler

- **Cüzdan & Kasa** — günlük harcama gücünüz ile birikiminiz ayrı tutulur, ikisi arasında anında transfer yapılır
- **İşlemler** — gelir/gider kayıtları; kategori, tutar, tarih, açıklama; arama ve CSV'ye aktarma
- **Tekrarlanan İşlemler** — aylık/haftalık düzenli gelir-giderler otomatik işlenir
- **Kategoriler** — kullanıcı tanımlı gelir/gider/hedef/yatırım kategorileri
- **Varlıklarım** — döviz/altın/kripto pozisyonları, canlı fiyatlarla anlık kâr/zarar
- **Rapor** — aylık gelir-gider dağılımı, kategori bazlı grafik, portföy değeri gelişimi
- **Hedeflerim** — birikim hedefleri koyup Kasa'dan aktararak ilerleme takibi
- **Notlar & Hatırlatıcılar** — finansal kararlar için not, ödeme tarihleri için hatırlatıcı
- **Profil & Ayarlar** — avatar, tutarları gizleme, veri temizleme sıklığı, açık/koyu tema

## Teknoloji

- **.NET 10 / WinForms** (`net10.0-windows`)
- **PostgreSQL** — [Npgsql](https://www.npgsql.org/) ile erişim
- **BCrypt.Net-Next** — parola hash'leme
- **WinForms.DataVisualization** — rapor grafikleri
- **xUnit** — `PersonalFinanceApp.Tests` altında birim testleri

## Kurulum (geliştirme ortamı)

1. **PostgreSQL** kurun ve boş bir veritabanı oluşturun:
   ```bash
   createdb PersonalFinanceDb
   ```
2. **Şemayı yükleyin**:
   ```bash
   psql -h <host> -p <port> -U <kullanici> -d PersonalFinanceDb -f database/schema.sql
   ```
3. **Bağlantı bilgisini ayarlayın** — `PersonalFinanceApp/config.json` dosyasını oluşturun (bu dosya `.gitignore`'da hariç tutulur, repoya gitmez):
   ```json
   {"ConnectionString": "Host=localhost;Port=5432;Database=PersonalFinanceDb;Username=postgres;Password=<sifreniz>"}
   ```
4. **Derleyin ve çalıştırın**:
   ```bash
   dotnet build PersonalFinanceApp.slnx
   dotnet run --project PersonalFinanceApp
   ```

### Testleri çalıştırma

```bash
dotnet test PersonalFinanceApp.slnx
```

## Proje yapısı

```
PersonalFinanceApp/          WinForms uygulaması (ekranlar, servisler, veri erişimi)
  ├─ Data/                   PostgreSQL erişim katmanı (repository'ler)
  ├─ Services/                İş mantığı (hesap, işlem, rapor, varlık fiyatlama, ...)
  ├─ Models/                  Veri modelleri
  ├─ Helpers/                 UI yardımcıları (tema, ikon, yuvarlak köşe çizimi, ...)
  └─ Resources/                Uygulama ikonu
PersonalFinanceApp.Tests/     xUnit birim testleri
database/schema.sql           PostgreSQL şeması
website/                      Tanıtım sitesi (GitHub Pages)
```

## Katkı

Bu, kişisel bir proje olarak geliştirilmektedir; pull request'ler değerlendirilir ama önceden issue açarak konuşmak tercih edilir.
