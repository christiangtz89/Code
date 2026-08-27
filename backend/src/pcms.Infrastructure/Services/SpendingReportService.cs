using Microsoft.EntityFrameworkCore;
using pcms.Application.Reporting;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

/// <summary>Uses database-side aggregates. Purchases are recognized once per receipt from historical receipt-item costs; legacy Received purchases without receipts use Purchase.Total once.</summary>
public class SpendingReportService(AppDbContext db) : ISpendingReportService
{
    public async Task<SpendingReportDto> GetAsync(DateTime? startDate, DateTime? endDate, Guid? expenseCategoryId, Guid? supplierId, bool includeExpenses, bool includePurchases)
    {
        var today = DateTime.UtcNow.Date;
        var start = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
        var end = endDate?.Date ?? today;
        if (end < start) throw new ArgumentException("La fecha final debe ser igual o posterior a la inicial.");
        var finish = end.AddDays(1);
        var expenseQuery = db.Expenses.AsNoTracking().Where(x => x.IsActive && x.ExpenseDate >= start && x.ExpenseDate < finish && (!expenseCategoryId.HasValue || x.ExpenseCategoryId == expenseCategoryId) && (!supplierId.HasValue || x.SupplierId == supplierId));
        var receiptItems = db.PurchaseReceiptItems.AsNoTracking().Where(x => x.PurchaseReceipt.ReceivedAt >= start && x.PurchaseReceipt.ReceivedAt < finish && (!supplierId.HasValue || x.PurchaseItem.Purchase.SupplierId == supplierId));
        var legacyPurchases = db.Purchases.AsNoTracking().Where(x => x.Status == PurchaseStatus.Received && !x.Receipts.Any() && x.PurchaseDate >= start && x.PurchaseDate < finish && (!supplierId.HasValue || x.SupplierId == supplierId));

        var expenseCurrency = includeExpenses ? await expenseQuery.GroupBy(x => x.Currency).Select(g => new Aggregate(g.Key, g.Sum(x => x.Total), g.Count())).ToListAsync() : [];
        var receiptCurrency = includePurchases ? await receiptItems.GroupBy(x => x.CurrencySnapshot).Select(g => new Aggregate(g.Key, g.Sum(x => x.QuantityReceived * (x.UnitCostSnapshot + (x.PurchaseReceipt.Purchase.Subtotal == 0 ? 0 : x.PurchaseReceipt.Purchase.Tax * x.PurchaseItem.LineSubtotal / x.PurchaseReceipt.Purchase.Subtotal / x.PurchaseItem.Quantity))), g.Select(x => x.PurchaseReceipt.PurchaseId).Distinct().Count())).ToListAsync() : [];
        var legacyCurrency = includePurchases ? await legacyPurchases.GroupBy(x => x.Currency).Select(g => new Aggregate(g.Key, g.Sum(x => x.Total), g.Count())).ToListAsync() : [];

        var categoryRaw = includeExpenses ? await expenseQuery.GroupBy(x => new { x.ExpenseCategoryId, x.ExpenseCategory.Name, x.Currency }).Select(g => new CategoryAggregate(g.Key.ExpenseCategoryId, g.Key.Name, g.Key.Currency, g.Sum(x => x.Total), g.Count())).ToListAsync() : [];
        var expenseSupplierRaw = includeExpenses ? await expenseQuery.Where(x => x.SupplierId != null).GroupBy(x => new { x.SupplierId, x.Supplier!.Name, x.Currency }).Select(g => new SupplierAggregate(g.Key.SupplierId!.Value, g.Key.Name, g.Key.Currency, g.Sum(x => x.Total), g.Count())).ToListAsync() : [];
        var receiptSupplierRaw = includePurchases ? await receiptItems.GroupBy(x => new { x.PurchaseItem.Purchase.SupplierId, x.PurchaseItem.Purchase.Supplier.Name, x.CurrencySnapshot }).Select(g => new SupplierAggregate(g.Key.SupplierId, g.Key.Name, g.Key.CurrencySnapshot, g.Sum(x => x.QuantityReceived * (x.UnitCostSnapshot + (x.PurchaseReceipt.Purchase.Subtotal == 0 ? 0 : x.PurchaseReceipt.Purchase.Tax * x.PurchaseItem.LineSubtotal / x.PurchaseReceipt.Purchase.Subtotal / x.PurchaseItem.Quantity))), g.Select(x => x.PurchaseReceipt.PurchaseId).Distinct().Count())).ToListAsync() : [];
        var legacySupplierRaw = includePurchases ? await legacyPurchases.GroupBy(x => new { x.SupplierId, x.Supplier.Name, x.Currency }).Select(g => new SupplierAggregate(g.Key.SupplierId, g.Key.Name, g.Key.Currency, g.Sum(x => x.Total), g.Count())).ToListAsync() : [];
        var expenseMonth = includeExpenses ? await expenseQuery.GroupBy(x => new { x.ExpenseDate.Year, x.ExpenseDate.Month, x.Currency }).Select(g => new MonthlyAggregate(g.Key.Year, g.Key.Month, g.Key.Currency, g.Sum(x => x.Total), 0m)).ToListAsync() : [];
        var receiptMonth = includePurchases ? await receiptItems.GroupBy(x => new { x.PurchaseReceipt.ReceivedAt.Year, x.PurchaseReceipt.ReceivedAt.Month, x.CurrencySnapshot }).Select(g => new MonthlyAggregate(g.Key.Year, g.Key.Month, g.Key.CurrencySnapshot, 0m, g.Sum(x => x.QuantityReceived * (x.UnitCostSnapshot + (x.PurchaseReceipt.Purchase.Subtotal == 0 ? 0 : x.PurchaseReceipt.Purchase.Tax * x.PurchaseItem.LineSubtotal / x.PurchaseReceipt.Purchase.Subtotal / x.PurchaseItem.Quantity))))).ToListAsync() : [];
        var legacyMonth = includePurchases ? await legacyPurchases.GroupBy(x => new { x.PurchaseDate.Year, x.PurchaseDate.Month, x.Currency }).Select(g => new MonthlyAggregate(g.Key.Year, g.Key.Month, g.Key.Currency, 0m, g.Sum(x => x.Total))).ToListAsync() : [];

        var expenseByCurrency = expenseCurrency.ToDictionary(x => x.Currency);
        var purchaseByCurrency = receiptCurrency.Concat(legacyCurrency).GroupBy(x => x.Currency).ToDictionary(g => g.Key, g => new Aggregate(g.Key, g.Sum(x => x.Total), g.Sum(x => x.Count)));
        var summary = expenseByCurrency.Keys.Concat(purchaseByCurrency.Keys).Distinct().OrderBy(x => x).Select(currency => { var expense = expenseByCurrency.GetValueOrDefault(currency) ?? new Aggregate(currency, 0, 0); var purchase = purchaseByCurrency.GetValueOrDefault(currency) ?? new Aggregate(currency, 0, 0); return new SpendingSummaryDto(currency, expense.Total, purchase.Total, expense.Total + purchase.Total, expense.Count, purchase.Count); }).ToList();
        var expenseTotals = summary.ToDictionary(x => x.Currency, x => x.OperatingExpenses);
        var purchaseTotals = summary.ToDictionary(x => x.Currency, x => x.Purchases);
        var categories = categoryRaw.Select(x => new ExpenseCategorySpendingDto(x.CategoryId, x.Name, x.Currency, x.Total, x.Count, expenseTotals.GetValueOrDefault(x.Currency) == 0 ? 0 : decimal.Round(x.Total * 100 / expenseTotals[x.Currency], 2))).OrderByDescending(x => x.Total).ToList();
        var purchaseSuppliers = receiptSupplierRaw.Concat(legacySupplierRaw).GroupBy(x => new { x.SupplierId, x.Name, x.Currency }).Select(g => { var total = g.Sum(x => x.Total); return new SupplierSpendingDto(g.Key.SupplierId, g.Key.Name, g.Key.Currency, total, g.Sum(x => x.Count), purchaseTotals.GetValueOrDefault(g.Key.Currency) == 0 ? 0 : decimal.Round(total * 100 / purchaseTotals[g.Key.Currency], 2)); }).OrderByDescending(x => x.Total).ToList();
        var expenseSuppliers = expenseSupplierRaw.Select(x => new SupplierSpendingDto(x.SupplierId, x.Name, x.Currency, x.Total, x.Count, expenseTotals.GetValueOrDefault(x.Currency) == 0 ? 0 : decimal.Round(x.Total * 100 / expenseTotals[x.Currency], 2))).OrderByDescending(x => x.Total).ToList();
        var monthly = expenseMonth.Concat(receiptMonth).Concat(legacyMonth).GroupBy(x => new { x.Year, x.Month, x.Currency }).Select(g => new MonthlySpendingDto(new DateTime(g.Key.Year, g.Key.Month, 1), g.Key.Currency, g.Sum(x => x.Expenses), g.Sum(x => x.Purchases), g.Sum(x => x.Expenses) + g.Sum(x => x.Purchases))).OrderBy(x => x.Month).ThenBy(x => x.Currency).ToList();
        return new SpendingReportDto(start, end, includeExpenses, includePurchases, "Las compras se reconocen una sola vez por recepción usando costos históricos y una parte proporcional del impuesto; compras históricas Recibidas sin recepciones se reconocen una sola vez por su total.", summary, categories, purchaseSuppliers, expenseSuppliers, monthly);
    }

    private record Aggregate(string Currency, decimal Total, int Count);
    private record CategoryAggregate(Guid CategoryId, string Name, string Currency, decimal Total, int Count);
    private record SupplierAggregate(Guid SupplierId, string Name, string Currency, decimal Total, int Count);
    private record MonthlyAggregate(int Year, int Month, string Currency, decimal Expenses, decimal Purchases);
}
