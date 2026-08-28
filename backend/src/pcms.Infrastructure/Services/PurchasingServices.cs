using Microsoft.EntityFrameworkCore;
using pcms.Application.Purchasing.DTOs;
using pcms.Application.Purchasing.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;
namespace pcms.Infrastructure.Services;

public class SupplierSupplyItemService(AppDbContext db):ISupplierSupplyItemService {
 public async Task<IEnumerable<SupplierSupplyItemDto>> GetAllAsync(Guid? s,Guid? i,bool active){var q=db.SupplierSupplyItems.AsNoTracking().Include(x=>x.Supplier).Include(x=>x.SupplyItem).Where(x=>x.IsActive==active);if(s.HasValue)q=q.Where(x=>x.SupplierId==s);if(i.HasValue)q=q.Where(x=>x.SupplyItemId==i);return(await q.ToListAsync()).Select(Map);}
 public async Task<IEnumerable<CostHistoryDto>> GetHistoryAsync(Guid id)=>await db.SupplierSupplyItemCostHistories.AsNoTracking().Where(x=>x.SupplierSupplyItemId==id).OrderByDescending(x=>x.EffectiveAt).Select(x=>new CostHistoryDto(x.Id,x.UnitCost,x.Currency,x.EffectiveAt,x.EndedAt)).ToListAsync();
 public async Task<SupplierSupplyItemDto>CreateAsync(SupplierSupplyItemInput i){Validate(i);if(await db.SupplierSupplyItems.AnyAsync(x=>x.SupplierId==i.SupplierId&&x.SupplyItemId==i.SupplyItemId))throw new InvalidOperationException("La relación proveedor-insumo ya existe.");var s=await db.Suppliers.FirstOrDefaultAsync(x=>x.Id==i.SupplierId&&x.IsActive)??throw new ArgumentException("Proveedor inválido.");var item=await db.SupplyItems.FirstOrDefaultAsync(x=>x.Id==i.SupplyItemId&&x.IsActive)??throw new ArgumentException("Insumo inválido.");var n=DateTime.UtcNow;var x=new SupplierSupplyItem{Id=Guid.NewGuid(),SupplierId=i.SupplierId,SupplyItemId=i.SupplyItemId,SupplierSku=O(i.SupplierSku),CurrentUnitCost=i.CurrentUnitCost,PurchaseUnit=i.PurchaseUnit.Trim(),InventoryUnitsPerPurchaseUnit=i.InventoryUnitsPerPurchaseUnit,Currency=i.Currency.Trim().ToUpperInvariant(),IsPreferred=i.IsPreferred,CreatedAt=n};if(x.IsPreferred)await db.SupplierSupplyItems.Where(y=>y.SupplyItemId==x.SupplyItemId).ExecuteUpdateAsync(a=>a.SetProperty(y=>y.IsPreferred,false));db.Add(x);db.Add(new SupplierSupplyItemCostHistory{Id=Guid.NewGuid(),SupplierSupplyItemId=x.Id,UnitCost=x.CurrentUnitCost,Currency=x.Currency,EffectiveAt=n,CreatedAt=n});await db.SaveChangesAsync();x.Supplier=s;x.SupplyItem=item;return Map(x);}
 public async Task<SupplierSupplyItemDto?>UpdateAsync(Guid id,SupplierSupplyItemInput i){Validate(i);var x=await db.SupplierSupplyItems.Include(x=>x.Supplier).Include(x=>x.SupplyItem).FirstOrDefaultAsync(x=>x.Id==id);if(x is null)return null;var c=i.Currency.Trim().ToUpperInvariant();var n=DateTime.UtcNow;if(x.CurrentUnitCost!=i.CurrentUnitCost||x.Currency!=c){var h=await db.SupplierSupplyItemCostHistories.FirstOrDefaultAsync(y=>y.SupplierSupplyItemId==id&&y.EndedAt==null);if(h is not null)h.EndedAt=n;db.Add(new SupplierSupplyItemCostHistory{Id=Guid.NewGuid(),SupplierSupplyItemId=id,UnitCost=i.CurrentUnitCost,Currency=c,EffectiveAt=n,CreatedAt=n});}x.SupplierSku=O(i.SupplierSku);x.CurrentUnitCost=i.CurrentUnitCost;x.PurchaseUnit=i.PurchaseUnit.Trim();x.InventoryUnitsPerPurchaseUnit=i.InventoryUnitsPerPurchaseUnit;x.Currency=c;x.IsPreferred=i.IsPreferred;x.UpdatedAt=n;await db.SaveChangesAsync();return Map(x);}
 public async Task<bool>DeactivateAsync(Guid id){var x=await db.SupplierSupplyItems.FirstOrDefaultAsync(x=>x.Id==id&&x.IsActive);if(x is null)return false;x.IsActive=false;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();return true;} static void Validate(SupplierSupplyItemInput i){if(i.CurrentUnitCost<0||i.InventoryUnitsPerPurchaseUnit<=0||string.IsNullOrWhiteSpace(i.PurchaseUnit)||i.Currency.Trim().Length!=3)throw new ArgumentException("Datos de compra inválidos.");}static string?O(string?x)=>string.IsNullOrWhiteSpace(x)?null:x.Trim();static SupplierSupplyItemDto Map(SupplierSupplyItem x)=>new(x.Id,x.SupplierId,x.Supplier.Name,x.SupplyItemId,x.SupplyItem.Name,x.SupplierSku,x.CurrentUnitCost,x.PurchaseUnit,x.InventoryUnitsPerPurchaseUnit,x.Currency,x.IsPreferred,x.IsActive,x.CreatedAt,x.UpdatedAt);
}

public class PurchaseService(AppDbContext db):IPurchaseService {
 public async Task<PurchaseDto>CreateAsync(PurchaseInput i,Guid? user){var p=await NewPurchase(i,user);db.Purchases.Add(p);await db.SaveChangesAsync();return(await GetByIdAsync(p.Id))!;}
 public async Task<PurchaseDto?>UpdateDraftAsync(Guid id,PurchaseInput i){var p=await db.Purchases.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);if(p is null)return null;if(p.Status!=PurchaseStatus.Draft)throw new InvalidOperationException("Solo se pueden editar borradores.");db.PurchaseItems.RemoveRange(p.Items);var replacement=await NewPurchase(i,p.RecordedByUserId);p.SupplierId=replacement.SupplierId;p.PurchaseDate=replacement.PurchaseDate;p.InvoiceReference=replacement.InvoiceReference;p.Currency=replacement.Currency;p.Tax=replacement.Tax;p.Notes=replacement.Notes;p.Subtotal=replacement.Subtotal;p.Total=replacement.Total;p.Items=replacement.Items;p.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();return await GetByIdAsync(id);}
 public async Task<PurchaseDto?>ChangeStatusAsync(Guid id,PurchaseStatus status){var p=await db.Purchases.Include(x=>x.Receipts).FirstOrDefaultAsync(x=>x.Id==id);if(p is null)return null;var ok=(p.Status==PurchaseStatus.Draft&&(status==PurchaseStatus.Ordered||status==PurchaseStatus.Cancelled))||(p.Status==PurchaseStatus.Ordered&&status==PurchaseStatus.Cancelled);if(!ok)throw new InvalidOperationException("Transición de estado inválida.");if(status==PurchaseStatus.Cancelled&&p.Receipts.Count>0)throw new InvalidOperationException("Una compra recibida requiere devoluciones; no puede cancelarse.");p.Status=status;p.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();return await GetByIdAsync(id);}
 public async Task<PurchaseDto>ReceiveAsync(Guid id,ReceiptInput input,Guid? user)
 {
  if(input.Items is null||input.Items.Count==0)throw new ArgumentException("La recepción requiere partidas.");
  await using var tx=await db.Database.BeginTransactionAsync();

  var requestedPurchaseItemIds=input.Items.Select(x=>x.PurchaseItemId).Distinct().ToArray();
  var affectedSupplyItemIds=await db.PurchaseItems.AsNoTracking()
   .Where(x=>x.PurchaseId==id&&requestedPurchaseItemIds.Contains(x.Id))
   .Select(x=>x.SupplyItemId)
   .Distinct()
   .ToListAsync();
  var ledger=new InventoryLedger(db);
  await ledger.AcquireSupplyItemLocksAsync(affectedSupplyItemIds);

  var p=await db.Purchases.Include(x=>x.Items).ThenInclude(x=>x.SupplyItem).Include(x=>x.Receipts).ThenInclude(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id)??throw new ArgumentException("Compra no encontrada.");
  if(p.Status is not (PurchaseStatus.Ordered or PurchaseStatus.PartiallyReceived))throw new InvalidOperationException("Solo se pueden recibir compras ordenadas.");
  var receipt=new PurchaseReceipt{Id=Guid.NewGuid(),PurchaseId=id,ReceivedAt=input.ReceivedAt==default?DateTime.UtcNow:input.ReceivedAt,ReceivedByUserId=user,Reference=O(input.Reference),Notes=O(input.Notes),CreatedAt=DateTime.UtcNow};
  foreach(var line in input.Items)
  {
   var pi=p.Items.FirstOrDefault(x=>x.Id==line.PurchaseItemId)??throw new ArgumentException("Partida inválida.");
   if(line.Quantity<=0)throw new ArgumentException("La cantidad recibida debe ser positiva.");
   var already=p.Receipts.SelectMany(x=>x.Items).Where(x=>x.PurchaseItemId==pi.Id).Sum(x=>x.QuantityReceived);
   if(already+line.Quantity>pi.Quantity)throw new ArgumentException("La recepción excede la cantidad pendiente.");
   var normalized=decimal.Round(line.Quantity*(pi.NormalizedReceivedQuantity/pi.Quantity),3);
   var movement=new SupplyInventoryMovement{Id=Guid.NewGuid(),SupplyItemId=pi.SupplyItemId,MovementType=SupplyInventoryMovementType.PurchaseReceipt,Quantity=normalized,UnitOfMeasure=pi.SupplyItem.UnitOfMeasure,OccurredAt=receipt.ReceivedAt,Reference=receipt.Reference,Notes="Recepción de compra",PurchaseItemId=pi.Id,RecordedByUserId=user,CreatedAt=DateTime.UtcNow};
   var ri=new PurchaseReceiptItem{Id=Guid.NewGuid(),PurchaseReceiptId=receipt.Id,PurchaseItemId=pi.Id,QuantityReceived=line.Quantity,NormalizedReceivedQuantity=normalized,UnitCostSnapshot=pi.UnitCost,CurrencySnapshot=pi.Currency,InventoryMovementId=movement.Id};
   movement.PurchaseReceiptItemId=ri.Id;
   receipt.Items.Add(ri);
   db.SupplyInventoryMovements.Add(movement);
   if(line.Lots is {Count:>0})
   {
    var scans=line.Lots.Select(x=>x.ScanCode.Trim()).ToList();
    if(!pi.SupplyItem.TrackInventory||scans.Any(string.IsNullOrWhiteSpace)||scans.Count!=scans.Distinct(StringComparer.OrdinalIgnoreCase).Count()||line.Lots.Any(x=>x.InitialQuantity<=0)||line.Lots.Sum(x=>x.InitialQuantity)!=normalized)throw new ArgumentException("Los lotes deben cubrir exactamente la recepción normalizada.");
    if(await db.SupplyInventoryLots.AnyAsync(x=>scans.Contains(x.ScanCode)))throw new ArgumentException("Código de lote duplicado.");
    foreach(var l in line.Lots)db.SupplyInventoryLots.Add(new SupplyInventoryLot{Id=Guid.NewGuid(),SupplyItemId=pi.SupplyItemId,PurchaseItemId=pi.Id,PurchaseReceiptItemId=ri.Id,ScanCode=l.ScanCode.Trim(),ManufacturerLotNumber=O(l.ManufacturerLotNumber),InitialQuantity=l.InitialQuantity,UnitOfMeasure=pi.SupplyItem.UnitOfMeasure,ReceivedAt=receipt.ReceivedAt,IsActive=true,CreatedAt=DateTime.UtcNow});
   }
  }
  db.PurchaseReceipts.Add(receipt);
  await db.SaveChangesAsync();
  var all=p.Receipts.SelectMany(x=>x.Items).Concat(receipt.Items).GroupBy(x=>x.PurchaseItemId).ToDictionary(x=>x.Key,x=>x.Sum(y=>y.QuantityReceived));
  p.Status=p.Items.All(x=>all.GetValueOrDefault(x.Id)>=x.Quantity)?PurchaseStatus.Received:PurchaseStatus.PartiallyReceived;
  p.UpdatedAt=DateTime.UtcNow;
  await db.SaveChangesAsync();
  await tx.CommitAsync();
  return(await GetByIdAsync(id))!;
 }
 public async Task<PurchaseDto?>GetByIdAsync(Guid id){var x=await Query().FirstOrDefaultAsync(x=>x.Id==id);return x is null?null:Map(x);} public async Task<PagedPurchasesDto>GetAllAsync(int page,int size,DateTime?from,DateTime?to,Guid?supplier){page=Math.Max(page,1);size=Math.Clamp(size,1,100);var q=Query();if(from.HasValue)q=q.Where(x=>x.PurchaseDate>=from);if(to.HasValue)q=q.Where(x=>x.PurchaseDate<=to);if(supplier.HasValue)q=q.Where(x=>x.SupplierId==supplier);var total=await q.CountAsync();var xs=await q.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();return new(xs.Select(Map),page,size,total,(int)Math.Ceiling(total/(double)size));}
 IQueryable<Purchase>Query()=>db.Purchases.AsNoTracking().Include(x=>x.Supplier).Include(x=>x.Items).ThenInclude(x=>x.SupplyItem).Include(x=>x.Receipts).ThenInclude(x=>x.ReceivedByUser).Include(x=>x.Receipts).ThenInclude(x=>x.Items).ThenInclude(x=>x.PurchaseItem).ThenInclude(x=>x.SupplyItem);
 async Task<Purchase>NewPurchase(PurchaseInput i,Guid?user){if(i.Items is null||i.Items.Count==0||i.Tax<0)throw new ArgumentException("La compra no es válida.");if(!await db.Suppliers.AnyAsync(x=>x.Id==i.SupplierId&&x.IsActive))throw new ArgumentException("Proveedor inválido.");var p=new Purchase{Id=Guid.NewGuid(),SupplierId=i.SupplierId,PurchaseDate=i.PurchaseDate==default?DateTime.UtcNow.Date:i.PurchaseDate.Date,InvoiceReference=O(i.InvoiceReference),Currency=i.Currency.Trim().ToUpperInvariant(),Tax=decimal.Round(i.Tax,2),Notes=O(i.Notes),RecordedByUserId=user,CreatedAt=DateTime.UtcNow,Status=PurchaseStatus.Draft};foreach(var l in i.Items){if(l.Quantity<=0)throw new ArgumentException("Cantidad inválida.");var source=await db.SupplierSupplyItems.FirstOrDefaultAsync(x=>x.Id==l.SupplierSupplyItemId&&x.SupplierId==i.SupplierId&&x.SupplyItemId==l.SupplyItemId&&x.IsActive)??throw new ArgumentException("Relación proveedor-insumo inválida.");var item=await db.SupplyItems.FirstAsync(x=>x.Id==l.SupplyItemId&&x.IsActive);p.Items.Add(new PurchaseItem{Id=Guid.NewGuid(),SupplyItemId=item.Id,SupplierSupplyItemId=source.Id,DescriptionSnapshot=item.Name,SupplierSkuSnapshot=source.SupplierSku,PurchaseUnitSnapshot=source.PurchaseUnit,Quantity=l.Quantity,UnitCost=source.CurrentUnitCost,LineSubtotal=decimal.Round(l.Quantity*source.CurrentUnitCost,2),NormalizedReceivedQuantity=decimal.Round(l.Quantity*source.InventoryUnitsPerPurchaseUnit,3),Currency=source.Currency});}p.Subtotal=decimal.Round(p.Items.Sum(x=>x.LineSubtotal),2);p.Total=decimal.Round(p.Subtotal+p.Tax,2);return p;}
 static PurchaseDto Map(Purchase x){var received=x.Receipts.SelectMany(r=>r.Items).GroupBy(i=>i.PurchaseItemId).ToDictionary(g=>g.Key,g=>g.Sum(v=>v.QuantityReceived));return new(x.Id,x.SupplierId,x.Supplier.Name,x.Status,x.PurchaseDate,x.InvoiceReference,x.Subtotal,x.Tax,x.Total,x.Currency,x.Notes,x.CreatedAt,x.Items.Select(i=>new PurchaseItemDto(i.Id,i.SupplyItemId,i.SupplyItem.Name,i.DescriptionSnapshot,i.SupplierSkuSnapshot,i.PurchaseUnitSnapshot,i.Quantity,received.GetValueOrDefault(i.Id),i.Quantity-received.GetValueOrDefault(i.Id),i.UnitCost,i.LineSubtotal,i.NormalizedReceivedQuantity,i.Currency)).ToList(),x.Receipts.OrderByDescending(r=>r.ReceivedAt).Select(r=>new PurchaseReceiptDto(r.Id,r.ReceivedAt,r.ReceivedByUser is null?null:$"{r.ReceivedByUser.FirstName} {r.ReceivedByUser.LastName}".Trim(),r.Reference,r.Notes,r.CreatedAt,r.Items.Select(i=>new PurchaseReceiptItemDto(i.Id,i.PurchaseItemId,i.PurchaseItem.SupplyItem.Name,i.QuantityReceived,i.NormalizedReceivedQuantity,i.UnitCostSnapshot,i.CurrencySnapshot)).ToList())).ToList());}static string?O(string?x)=>string.IsNullOrWhiteSpace(x)?null:x.Trim();
}
