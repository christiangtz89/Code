namespace pcms.Application.Inventory;
public record ProductionUsageInput(Guid SupplyItemId,decimal ActualQuantity,decimal? WasteQuantity);
public record ProductionInput(Guid UrnId,Guid UrnBillOfMaterialsId,decimal QuantityProduced,DateTime? ProducedAt,string? Notes,IReadOnlyCollection<ProductionUsageInput> Usages);
public record ProductionUsageDto(Guid Id,Guid SupplyItemId,string SupplyItemName,decimal ExpectedQuantity,decimal ActualQuantity,decimal? WasteQuantity,string UnitOfMeasure,decimal? CostPerUnitSnapshot,decimal? TotalMaterialCostSnapshot);
public record ProductionDto(Guid Id,Guid UrnId,string UrnName,Guid UrnBillOfMaterialsId,int BomVersion,decimal QuantityProduced,DateTime ProducedAt,string? Notes,IReadOnlyCollection<ProductionUsageDto> Usages,decimal? TotalMaterialCost,bool CostComplete);
public interface IManufacturedUrnProductionService { Task<ProductionDto> CreateAsync(ProductionInput input,Guid? userId); Task<IEnumerable<ProductionDto>> GetAllAsync(Guid? urnId,bool includeCost); }
