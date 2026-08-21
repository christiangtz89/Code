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

    public WeightRangeChangeConfirmationRequiredException(
        decimal previousWeightKg,
        decimal newWeightKg,
        decimal previousMinimumWeightKg,
        decimal previousMaximumWeightKg,
        decimal newMinimumWeightKg,
        decimal newMaximumWeightKg,
        decimal? previousPrice,
        decimal? newPrice)
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

        PreviousPrice = previousPrice;
        NewPrice = newPrice;
    }
}