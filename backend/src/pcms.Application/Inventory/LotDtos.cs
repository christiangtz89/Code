namespace pcms.Application.Inventory;
public record LotInput(Guid SupplyItemId,string ScanCode,string? ManufacturerLotNumber,decimal InitialQuantity,DateTime? ReceivedAt,string? Notes);
public record LotDto(Guid Id,Guid SupplyItemId,string SupplyItemName,string ScanCode,string? ManufacturerLotNumber,decimal InitialQuantity,decimal RemainingQuantity,string UnitOfMeasure,DateTime ReceivedAt,bool IsActive);
public interface IInventoryLotService { Task<IEnumerable<LotDto>> GetAsync(string? search); Task<LotDto?> GetByScanAsync(string code); Task<LotDto> CreateAsync(LotInput input); }
