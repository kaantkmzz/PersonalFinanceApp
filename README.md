# Finans Takip (PersonalFinanceApp)

Cüzdan ve Kasa'yı tek defterde tutan, Windows için masaüstü kişisel finans takip uygulaması. C# / WinForms ile yazıldı, PostgreSQL üzerinde çalışır.

Tanıtım sitesi: **https://kaantkmzz.github.io/PersonalFinanceApp/** — tarayıcıda çalışan canlı demoyu deneyebilir, "İndir" bölümünden uygulamanın kendisini (self-contained .exe, kurulum gerektirmez, ortak örnek veritabanına bağlı — gerçek/kişisel verilerinizi bu sürümde saklamayın) indirebilirsiniz. Kendi verilerinizle kullanmak isterseniz aşağıdaki "Kurulum" adımlarıyla kendi veritabanınızı kurabilirsiniz.

> **Not:** Uygulama henüz bir kod imzalama sertifikasıyla imzalanmadığı için indirip çalıştırırken Windows SmartScreen "Bilinmeyen yayıncı" uyarısı gösterebilir. Bu beklenen bir durumdur — uyarı ekranında **"Ek bilgi"** ardından **"Yine de çalıştır"** seçeneğine tıklayarak devam edebilirsiniz.

## Özellikler

- **Cüzdan & Kasa** — günlük harcama gücünüz ile birikiminiz ayrı tutulur, ikisi arasında anında transfer yapılır; transfer geçmişi ayrıca izlenebilir
- **İşlemler** — gelir/gider kayıtları; kategori, tutar, tarih, açıklama; arama, tarih aralığı filtresi, toplu işlem modu ve CSV'ye aktarma
- **Tekrarlanan İşlemler** — aylık/haftalık düzenli gelir-giderler otomatik işlenir
- **Kategoriler** — kullanıcı tanımlı gelir/gider/hedef/yatırım kategorileri; renk/ikon özelleştirme ve aylık bütçe limiti (aşılınca bildirim)
- **Varlıklarım** — döviz/altın/kripto pozisyonları, canlı fiyatlarla anlık kâr/zarar, satışta gerçekleşen kâr/zarar hesaplaması, fiyat alarmı (eşik aşılınca bildirim)
- **Rapor** — aylık gelir-gider dağılımı, kategori bazlı grafik, portföy değeri gelişimi, Varlıklarım için Trend modu ve CSV'ye aktarma
- **Hedeflerim** — birikim hedefleri koyup Kasa'dan aktararak ilerleme takibi; tarih ve otomatik katkı, tamamlanınca bildirim
- **Notlar & Hatırlatıcılar** — finansal kararlar için aranabilir not defteri; ödeme tarihleri için tekrar sıklığı ve erteleme destekli hatırlatıcı
- **Ana Sayfa widget'ları** — sürükle-bırakla düzenlenebilir ızgara: Varlık Bildirimleri, Notlar, Yaklaşan Hatırlatıcılar, Bu Ayın Özeti, Son İşlemler, Hedeflerim, Nakit Akışı Tahmini (aktif tekrarlayan işlemlere göre 30 gün sonrası tahmini bakiye) ve Hızlı İşlem Ekle (ekrandan ayrılmadan tek satırda gelir/gider girme)
- **Sistem tepsisi** — pencere kapatılınca arkaplanda çalışmaya devam eder; simge üzerine gelince anlık Cüzdan/Kasa özeti, arkaplanda hatırlatıcı/bütçe/fiyat alarmı bildirimleri
- **Profil & Ayarlar** — avatar, tutarları gizleme (göz ikonuyla anında aç/kapat), veri temizleme sıklığı, açık/koyu tema

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
