using PersonalFinanceApp.Data;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class ReportHistoryService
    {
        private readonly ReportHistoryRepository _historyRepository = new ReportHistoryRepository();
        private readonly AccountRepository _accountRepository = new AccountRepository();
        private readonly ReportService _reportService = new ReportService();

        public List<ReportHistoryEntry> GetHistory(int userId)
        {
            return _historyRepository.GetByUserId(userId);
        }

        // Seçili periyot (günlük/haftalık/aylık) süresi dolmuşsa raporu geçmişe kaydedip
        // bir sonraki periyodu başlatır. Uygulama uzun süre kapalı kalmışsa birden fazla
        // periyodu art arda tamamlayıp her birini ayrı ayrı kaydeder. Güncel (olası yeni)
        // periyot başlangıcını döner.
        public DateTime CheckAndSnapshotCompletedPeriods(int userId, string periodType, DateTime periodStart)
        {
            DateTime now = DateTime.Now;

            while (ReportPeriodHelper.GetPeriodEnd(periodStart, periodType) <= now)
            {
                DateTime periodEnd = ReportPeriodHelper.GetPeriodEnd(periodStart, periodType);
                var report = _reportService.GenerateReport(userId, periodStart, periodEnd);

                _historyRepository.Insert(new ReportHistoryEntry
                {
                    UserId = userId,
                    PeriodType = periodType,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TotalIncome = report.TotalIncome,
                    TotalExpense = report.TotalExpense,
                    TotalGoal = report.TotalGoal,
                    IncomeBreakdown = report.IncomeBreakdown,
                    ExpenseBreakdown = report.ExpenseBreakdown,
                    GoalBreakdown = report.GoalBreakdown
                });

                periodStart = periodEnd;
            }

            _accountRepository.SetReportPeriod(userId, periodType, periodStart);
            return periodStart;
        }
    }
}
