namespace pcms.Application.Inventory;
public record FilamentSpecificationDto(Guid Id,Guid SupplyItemId,string MaterialType,string? Brand,string? Color,decimal NetUsableWeightGrams,string? ManufacturerProductCode,string? ProductData,decimal? CostPerKilogram,decimal? CostPerGram,string? Currency);
public record FilamentSpecificationInput(Guid SupplyItemId,string MaterialType,string? Brand,string? Color,decimal NetUsableWeightGrams,string? ManufacturerProductCode,string? ProductData);
public interface IFilamentService { Task<FilamentSpecificationDto?> GetAsync(Guid supplyItemId,bool includeCost); Task<FilamentSpecificationDto> SaveAsync(FilamentSpecificationInput input); }
