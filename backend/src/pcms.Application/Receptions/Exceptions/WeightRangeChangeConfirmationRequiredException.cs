namespace pcms.Application.Receptions.Exceptions;

public sealed class WeightRangeChangeConfirmationRequiredException
    : InvalidOperationException
{
    public decimal PreviousWeightKg { get; }

    public decimal NewWeightKg { get; }

    public decimal PreviousMinimumWeightKg { get; }

    public decimal PreviousMaximumWeightKg { get; }

    public decimal NewMinimumWeightKg { get; }

    public decimal NewMaximumWeightKg { get; }

    public decimal? PreviousPrice { get; }

    public decimal? NewPrice { get; }

    public decimal? PriceDifference { get; }

    public decimal? AmountPaid { get; }

    public decimal? RemainingBalance { get; }

    public decimal? OverpaymentAmount { get; }

    public bool RequiresFinancialReview { get; }

    public WeightRangeChangeConfirmationRequiredException(
        decimal previousWeightKg,
        decimal newWeightKg,
        decimal previousMinimumWeightKg,
        decimal previousMaximumWeightKg,
        decimal newMinimumWeightKg,
        decimal newMaximumWeightKg,
        decimal? previousPrice,
        decimal? newPrice,
        decimal? amountPaid = null)
        : base(
            "El cambio de peso modifica el rango de precio y requiere confirmación.")
    {
        PreviousWeightKg = previousWeightKg;
        NewWeightKg = newWeightKg;

        PreviousMinimumWeightKg =
            previousMinimumWeightKg;

        PreviousMaximumWeightKg =
            previousMaximumWeightKg;

        NewMinimumWeightKg =
            newMinimumWeightKg;

        NewMaximumWeightKg =
            newMaximumWeightKg;

        PriceDifference = previousPrice.HasValue && newPrice.HasValue
            ? newPrice.Value - previousPrice.Value
            : null;

        AmountPaid = amountPaid;

        RemainingBalance = amountPaid.HasValue && newPrice.HasValue
            ? Math.Max(0m, newPrice.Value - amountPaid.Value)
            : null;

        OverpaymentAmount = amountPaid.HasValue && newPrice.HasValue
            ? Math.Max(0m, amountPaid.Value - newPrice.Value)
            : null;

        RequiresFinancialReview = OverpaymentAmount > 0m;

        PreviousPrice = previousPrice;
        NewPrice = newPrice;
    }
}