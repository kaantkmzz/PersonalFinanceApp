using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.Tests
{
    [TestClass]
    public class ReportServiceTests
    {
        private readonly ReportService _reportService = new ReportService();

        [TestMethod]
        public void GetPercentage_HesaplamaDogruMu()
        {
            double result = _reportService.GetPercentage(250, 1000);
            Assert.AreEqual(25.0, result, 0.01);
        }

        [TestMethod]
        public void GetPercentage_ToplamSifirsaSifirDonmeliMi()
        {
            double result = _reportService.GetPercentage(100, 0);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GenerateBar_YuzYuzdedeBosKarakterOlmamaliMi()
        {
            string bar = _reportService.GenerateBar(100);
            Assert.DoesNotContain('░', bar);
        }

        [TestMethod]
        public void GenerateBar_SifirYuzdedeDoluKarakterOlmamaliMi()
        {
            string bar = _reportService.GenerateBar(0);
            Assert.DoesNotContain('█', bar);
        }

        [TestMethod]
        public void GetTopExpenseCategory_EnYuksekTutarliKategoriyiBuluyorMu()
        {
            var report = new MonthlyReport
            {
                ExpenseBreakdown = new List<CategorySummary>
                {
                    new CategorySummary { CategoryName = "Market", TotalAmount = 500 },
                    new CategorySummary { CategoryName = "Fatura", TotalAmount = 900 },
                    new CategorySummary { CategoryName = "Ulaşım", TotalAmount = 200 }
                }
            };

            var topCategory = _reportService.GetTopExpenseCategory(report);

            Assert.IsNotNull(topCategory);
            Assert.AreEqual("Fatura", topCategory!.CategoryName);
        }

        [TestMethod]
        public void GetTopExpenseCategory_GiderYokkenNullDonmeliMi()
        {
            var report = new MonthlyReport { ExpenseBreakdown = new List<CategorySummary>() };
            var topCategory = _reportService.GetTopExpenseCategory(report);
            Assert.IsNull(topCategory);
        }

        [TestMethod]
        public void GetComparisonMessages_BuyukArtisiYakaliyorMu()
        {
            var previous = new MonthlyReport
            {
                TotalExpense = 1000,
                ExpenseBreakdown = new List<CategorySummary> { new CategorySummary { CategoryName = "Market", TotalAmount = 500 } }
            };
            var current = new MonthlyReport
            {
                TotalExpense = 1500,
                ExpenseBreakdown = new List<CategorySummary> { new CategorySummary { CategoryName = "Market", TotalAmount = 900 } }
            };

            var messages = _reportService.GetComparisonMessages(current, previous);

            Assert.IsTrue(messages.Any(m => m.Contains("Market")));
        }

        [TestMethod]
        public void GetComparisonMessages_KucukDegisimGosterilmemeliMi()
        {
            var previous = new MonthlyReport
            {
                TotalExpense = 1000,
                ExpenseBreakdown = new List<CategorySummary> { new CategorySummary { CategoryName = "Market", TotalAmount = 500 } }
            };
            var current = new MonthlyReport
            {
                TotalExpense = 1010,
                ExpenseBreakdown = new List<CategorySummary> { new CategorySummary { CategoryName = "Market", TotalAmount = 505 } }
            };

            var messages = _reportService.GetComparisonMessages(current, previous);

            Assert.IsFalse(messages.Any(m => m.Contains("Market")));
        }
    }
}