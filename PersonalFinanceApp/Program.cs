using PersonalFinanceApp.Data;

Console.WriteLine("=== Kişisel Finans Takip Sistemi ===");
Console.WriteLine("Veritabanı bağlantısı test ediliyor...\n");

bool isConnected = DatabaseHelper.TestConnection();

if (isConnected)
{
    Console.WriteLine("\nHer şey hazır, uygulama geliştirmeye devam edebiliriz.");
}
else
{
    Console.WriteLine("\nBağlantı başarısız oldu. Lütfen config.json içindeki bilgileri kontrol et.");
}

Console.WriteLine("\nDevam etmek için bir tuşa basın...");
Console.ReadKey();