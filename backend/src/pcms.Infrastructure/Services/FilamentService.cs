using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;
namespace pcms.Infrastructure.Services;
public class FilamentService(AppDbContext db) : IFilamentService
{
 public async Task<FilamentSpecificationDto?> GetAsync(Guid id,bool includeCost){var x=await db.FilamentSpecifications.AsNoTracking().FirstOrDefaultAsync(x=>x.SupplyItemId==id);return x is null?null:await Map(x,includeCost);}
 public async Task<FilamentSpecificationDto> SaveAsync(FilamentSpecificationInput i){if(string.IsNullOrWhiteSpace(i.MaterialType)||i.NetUsableWeightGrams<=0)throw new ArgumentException("Material y peso neto son obligatorios.");if(!await db.SupplyItems.AnyAsync(x=>x.Id==i.SupplyItemId&&x.IsActive))throw new ArgumentException("El insumo no existe o está inactivo.");var x=await db.FilamentSpecifications.FirstOrDefaultAsync(x=>x.SupplyItemId==i.SupplyItemId);if(x is null){x=new FilamentSpecification{Id=Guid.NewGuid(),SupplyItemId=i.SupplyItemId,CreatedAt=DateTime.UtcNow};db.Add(x);}x.MaterialType=i.MaterialType.Trim();x.Brand=O(i.Brand);x.Color=O(i.Color);x.NetUsableWeightGrams=decimal.Round(i.NetUsableWeightGrams,3);x.ManufacturerProductCode=O(i.ManufacturerProductCode);x.ProductData=O(i.ProductData);x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();return await Map(x,false);}
 async Task<FilamentSpecificationDto> Map(FilamentSpecification x,bool cost){decimal? kg=null,gram=null;string? currency=null;if(cost){var s=await db.SupplierSupplyItems.AsNoTracking().Where(s=>s.SupplyItemId==x.SupplyItemId&&s.IsActive&&s.IsPreferred&&s.InventoryUnitsPerPurchaseUnit>0).SingleOrDefaultAsync();if(s is not null){gram=decimal.Round(s.CurrentUnitCost/s.InventoryUnitsPerPurchaseUnit,8);kg=decimal.Round(gram.Value*1000,4);currency=s.Currency;}}return new(x.Id,x.SupplyItemId,x.MaterialType,x.Brand,x.Color,x.NetUsableWeightGrams,x.ManufacturerProductCode,x.ProductData,kg,gram,currency);}
 static string? O(string? x)=>string.IsNullOrWhiteSpace(x)?null:x.Trim();
}
