namespace pcms.Application.Reporting;

public interface ISpendingReportService
{
    Task<SpendingReportDto> GetAsync(DateTime? startDate, DateTime? endDate, Guid? expenseCategoryId, Guid? supplierId, bool includeExpenses, bool includePurchases);
}
