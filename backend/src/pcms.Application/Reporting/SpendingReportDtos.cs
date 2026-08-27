namespace pcms.Application.Reporting;

public record SpendingSummaryDto(string Currency, decimal OperatingExpenses, decimal Purchases, decimal Total, int ExpenseCount, int PurchaseCount);
public record ExpenseCategorySpendingDto(Guid CategoryId, string CategoryName, string Currency, decimal Total, int ExpenseCount, decimal Percentage);
public record SupplierSpendingDto(Guid? SupplierId, string SupplierName, string Currency, decimal Total, int PurchaseCount, decimal Percentage);
public record MonthlySpendingDto(DateTime Month, string Currency, decimal OperatingExpenses, decimal Purchases, decimal Total);
public record SpendingReportDto(DateTime StartDate, DateTime EndDate, bool IncludesOperatingExpenses, bool IncludesPurchases, string PurchaseAmountBasis, IReadOnlyList<SpendingSummaryDto> Summary, IReadOnlyList<ExpenseCategorySpendingDto> ExpenseCategories, IReadOnlyList<SupplierSpendingDto> PurchaseSuppliers, IReadOnlyList<SupplierSpendingDto> ExpenseSuppliers, IReadOnlyList<MonthlySpendingDto> MonthlyTrend);
