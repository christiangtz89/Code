namespace pcms.Application.Inventory;
public record BomItemInput(Guid SupplyItemId,decimal RequiredQuantity,string UnitOfMeasure);
public record BomInput(Guid UrnId,IReadOnlyCollection<BomItemInput> Items);
public record BomItemDto(Guid Id,Guid SupplyItemId,string SupplyItemName,decimal RequiredQuantity,string UnitOfMeasure,decimal? EstimatedCost,string? Currency,bool CostAvailable);
public record BomDto(Guid Id,Guid UrnId,string UrnName,int Version,bool IsActive,IReadOnlyCollection<BomItemDto> Items,decimal? EstimatedMaterialCost,bool CostComplete,string? Currency);
public interface IUrnBomService { Task<IEnumerable<BomDto>> GetAsync(Guid urnId,bool includeCost); Task<BomDto> SaveAsync(BomInput input); }
