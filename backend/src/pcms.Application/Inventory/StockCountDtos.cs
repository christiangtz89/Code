using pcms.Domain.Enums;

namespace pcms.Application.Inventory;

public record StockCountInput(Guid SupplyItemId, decimal CountedQuantity, decimal? ExpectedSystemQuantity, Guid? LotId = null);
public record StockCountDto(Guid Id, Guid SupplyItemId, Guid? LotId, decimal SystemQuantity, decimal CountedQuantity, decimal Variance, DateTime CountedAt, Guid? CountedByUserId, Guid? InventoryMovementId);
public interface IStockCountService
{
    Task<StockCountDto> RecordAsync(StockCountInput input, Guid? userId);
    Task<IEnumerable<StockCountDto>> GetAsync(Guid supplyItemId);
}
