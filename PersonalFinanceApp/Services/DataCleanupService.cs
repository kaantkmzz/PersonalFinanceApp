using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class DataCleanupService
    {
        private readonly AccountRepository _accountRepository = new AccountRepository();
        private readonly TransactionRepository _transactionRepository = new TransactionRepository();
        private readonly CategoryRepository _categoryRepository = new CategoryRepository();

        public const string Weekly = "weekly";
        public const string Monthly = "monthly";
        public const string Never = "never";

        // Girişte çağrılır: seçili temizleme sıklığının (haftalık/aylık) süresi dolmuşsa kullanıcının
        // işlemlerini ve kategorilerini temizler ("hiçbir zaman" seçiliyse hiçbir şey yapmaz).
        // Tekrarlanan işlemler temizlik sonrası da çalışmaya devam etmeli, bu yüzden onlara bağlı
        // kategoriler silinmez (bkz. CategoryRepository.DeleteUnusedByRecurring).
        public (bool Cleaned, string? ExportedCsvPath) CheckAndRunCleanup(User user)
        {
            if (user.CleanupFrequency == Never) return (false, null);

            DateTime periodEnd = user.CleanupFrequency == Weekly
                ? user.CleanupPeriodStart.AddDays(7)
                : user.CleanupPeriodStart.AddMonths(1);

            if (DateTime.Now < periodEnd) return (false, null);

            string? csvPath = user.CleanupExportBeforeClear ? ExportBeforeCleanup(user.Id) : null;

            _transactionRepository.DeleteAllForUser(user.Id);
            _categoryRepository.DeleteUnusedByRecurring(user.Id);

            user.CleanupPeriodStart = DateTime.Now;
            _accountRepository.SetCleanupPeriodStart(user.Id, user.CleanupPeriodStart);

            return (true, csvPath);
        }

        private string ExportBeforeCleanup(int userId)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FinansTakip", "Yedekler");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"temizlik_yedek_{DateTime.Now:yyyy_MM_dd_HHmm}.csv");

            var transactions = _transactionRepository.GetByUserId(userId);
            var categories = _categoryRepository.GetByUserId(userId);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            using (var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("=== İşlemler ===");
                writer.WriteLine("Tarih;Tip;Kategori;Tutar;Açıklama");
                foreach (var t in transactions)
                {
                    string aciklama = (t.Description ?? "").Replace(";", ",");
                    writer.WriteLine($"{t.TransactionDate:dd.MM.yyyy};{TypeToTr(t.Type)};{t.CategoryName};{t.Amount.ToString("0.00", tr)};{aciklama}");
                }

                writer.WriteLine();
                writer.WriteLine("=== Kategoriler ===");
                writer.WriteLine("Ad;Tip");
                foreach (var c in categories)
                {
                    writer.WriteLine($"{c.Name};{TypeToTr(c.Type)}");
                }
            }

            return path;
        }

        private static string TypeToTr(string type) => type switch { "income" => "Gelir", "goal" => "Hedef", _ => "Gider" };
    }
}
