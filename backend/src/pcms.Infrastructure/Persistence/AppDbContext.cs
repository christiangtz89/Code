using Microsoft.EntityFrameworkCore;
using pcms.Domain.Entities;

namespace pcms.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }


    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Pet> Pets { get; set; }

    public DbSet<Collection> Collections { get; set; }

    public DbSet<CollectionPhoto> CollectionPhotos { get; set; }

    public DbSet<Reception> Receptions { get; set; }

    public DbSet<ReceptionPhoto> ReceptionPhotos { get; set; }

    public DbSet<Cremation> Cremations { get; set; }

    public DbSet<CremationPackage> CremationPackages { get; set; }

    public DbSet<Urn> Urns { get; set; }

    public DbSet<CremationPackageUrn> CremationPackageUrns { get; set; }

    public DbSet<CremationPricingConfiguration> CremationPricingConfigurations =>
        Set<CremationPricingConfiguration>();

    public DbSet<CremationPrice> CremationPrices =>
        Set<CremationPrice>();

    public DbSet<PaymentAccount> PaymentAccounts { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<VeterinaryClinic> VeterinaryClinics { get; set; }

    public DbSet<Veterinarian> Veterinarians { get; set; }

    public DbSet<VeterinaryRequest> VeterinaryRequests { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<SupplyItem> SupplyItems { get; set; }
    public DbSet<SupplierSupplyItem> SupplierSupplyItems { get; set; }
    public DbSet<SupplierSupplyItemCostHistory> SupplierSupplyItemCostHistories { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseItem> PurchaseItems { get; set; }
    public DbSet<PurchaseReceipt> PurchaseReceipts { get; set; }
    public DbSet<PurchaseReceiptItem> PurchaseReceiptItems { get; set; }
    public DbSet<SupplyInventoryMovement> SupplyInventoryMovements { get; set; }
    public DbSet<FilamentSpecification> FilamentSpecifications { get; set; }
    public DbSet<UrnBillOfMaterials> UrnBillOfMaterials { get; set; }
    public DbSet<UrnBillOfMaterialsItem> UrnBillOfMaterialsItems { get; set; }
    public DbSet<ManufacturedUrnProduction> ManufacturedUrnProductions { get; set; }
    public DbSet<ManufacturingMaterialUsage> ManufacturingMaterialUsages { get; set; }
    public DbSet<UrnSupplyItem> UrnSupplyItems { get; set; }
    public DbSet<SupplyInventoryLot> SupplyInventoryLots { get; set; }
    public DbSet<CremationUrnReservation> CremationUrnReservations { get; set; }
    public DbSet<CremationInventoryFulfillment> CremationInventoryFulfillments { get; set; }
    public DbSet<CremationInventoryMaterial> CremationInventoryMaterials { get; set; }
    public DbSet<InventoryStockCount> InventoryStockCounts { get; set; }


    protected override void OnModelCreating(
     ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Supplier>(e => { e.ToTable("Proveedores"); e.HasKey(x => x.Id); e.Property(x => x.Name).HasColumnName("Nombre").HasMaxLength(200).IsRequired(); e.Property(x => x.LegalName).HasColumnName("RazonSocial").HasMaxLength(250); e.Property(x => x.TaxId).HasColumnName("RFC").HasMaxLength(30); e.HasIndex(x => x.TaxId); });
        modelBuilder.Entity<ExpenseCategory>(e => { e.ToTable("CategoriasGasto"); e.HasKey(x => x.Id); e.Property(x => x.Name).HasColumnName("Nombre").HasMaxLength(150).IsRequired(); e.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(500); e.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Expense>(e => { e.ToTable("Gastos"); e.HasKey(x => x.Id); e.Property(x => x.Subtotal).HasColumnType("numeric(14,2)"); e.Property(x => x.Tax).HasColumnType("numeric(14,2)"); e.Property(x => x.Total).HasColumnType("numeric(14,2)"); e.Property(x => x.Currency).HasMaxLength(3).IsRequired(); e.HasOne(x => x.ExpenseCategory).WithMany(x => x.Expenses).HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Supplier).WithMany(x => x.Expenses).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<SupplyItem>(e => { e.ToTable("Insumos"); e.HasKey(x => x.Id); e.Property(x => x.MinimumQuantity).HasColumnType("numeric(14,3)"); e.Property(x => x.ScanCode).HasMaxLength(150).IsRequired(); e.HasIndex(x => x.ScanCode).IsUnique(); e.HasIndex(x => x.InternalSku); });
        modelBuilder.Entity<SupplierSupplyItem>(e => { e.ToTable("ProveedoresInsumos"); e.HasKey(x=>x.Id); e.Property(x=>x.CurrentUnitCost).HasColumnType("numeric(14,4)"); e.Property(x=>x.InventoryUnitsPerPurchaseUnit).HasColumnType("numeric(14,6)"); e.HasIndex(x=>new{x.SupplierId,x.SupplyItemId}).IsUnique(); e.HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<SupplierSupplyItemCostHistory>(e => { e.ToTable("HistorialCostosProveedorInsumo"); e.HasKey(x=>x.Id); e.Property(x=>x.UnitCost).HasColumnType("numeric(14,4)"); e.HasOne(x=>x.SupplierSupplyItem).WithMany(x=>x.CostHistory).HasForeignKey(x=>x.SupplierSupplyItemId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x=>new{x.SupplierSupplyItemId,x.EffectiveAt}); });
        modelBuilder.Entity<Purchase>(e => { e.ToTable("Compras"); e.HasKey(x=>x.Id); e.Property(x=>x.Status).HasConversion<int>().IsRequired(); e.Property(x=>x.Subtotal).HasColumnType("numeric(14,2)"); e.Property(x=>x.Tax).HasColumnType("numeric(14,2)"); e.Property(x=>x.Total).HasColumnType("numeric(14,2)"); e.HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x=>x.RecordedByUser).WithMany().HasForeignKey(x=>x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<PurchaseItem>(e => { e.ToTable("PartidasCompra"); e.HasKey(x=>x.Id); e.Property(x=>x.Quantity).HasColumnType("numeric(14,3)"); e.Property(x=>x.UnitCost).HasColumnType("numeric(14,4)"); e.Property(x=>x.LineSubtotal).HasColumnType("numeric(14,2)"); e.Property(x=>x.NormalizedReceivedQuantity).HasColumnType("numeric(14,3)"); e.HasOne(x=>x.Purchase).WithMany(x=>x.Items).HasForeignKey(x=>x.PurchaseId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x=>x.SupplierSupplyItem).WithMany().HasForeignKey(x=>x.SupplierSupplyItemId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<PurchaseReceipt>(e=>{e.ToTable("RecepcionesCompra");e.HasKey(x=>x.Id);e.HasOne(x=>x.Purchase).WithMany(x=>x.Receipts).HasForeignKey(x=>x.PurchaseId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.ReceivedByUser).WithMany().HasForeignKey(x=>x.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);e.HasIndex(x=>new{x.PurchaseId,x.ReceivedAt});});
        modelBuilder.Entity<PurchaseReceiptItem>(e=>{e.ToTable("PartidasRecepcionCompra");e.HasKey(x=>x.Id);e.Property(x=>x.QuantityReceived).HasColumnType("numeric(14,3)");e.Property(x=>x.NormalizedReceivedQuantity).HasColumnType("numeric(14,3)");e.Property(x=>x.UnitCostSnapshot).HasColumnType("numeric(14,4)");e.HasOne(x=>x.PurchaseReceipt).WithMany(x=>x.Items).HasForeignKey(x=>x.PurchaseReceiptId).OnDelete(DeleteBehavior.Cascade);e.HasOne(x=>x.PurchaseItem).WithMany(x=>x.ReceiptItems).HasForeignKey(x=>x.PurchaseItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.InventoryMovement).WithOne(x=>x.PurchaseReceiptItem).HasForeignKey<PurchaseReceiptItem>(x=>x.InventoryMovementId).OnDelete(DeleteBehavior.Restrict);e.HasIndex(x=>x.InventoryMovementId).IsUnique();});
        modelBuilder.Entity<SupplyInventoryMovement>(e => { e.ToTable("MovimientosInventarioInsumos"); e.HasKey(x=>x.Id); e.Property(x=>x.MovementType).HasConversion<int>().IsRequired(); e.Property(x=>x.Origin).HasConversion<int>().IsRequired(); e.Property(x=>x.ReasonCode).HasConversion<int>(); e.Property(x=>x.ScannedCode).HasMaxLength(150); e.Property(x=>x.SupplyItemNameSnapshot).HasMaxLength(250).IsRequired(); e.Property(x=>x.RecordedByDisplayNameSnapshot).HasMaxLength(250); e.Property(x=>x.Quantity).HasColumnType("numeric(14,3)"); e.HasIndex(x=>new{x.SupplyItemId,x.OccurredAt}); e.HasIndex(x=>new{x.RecordedByUserId,x.OccurredAt}); e.HasIndex(x=>new{x.Origin,x.OccurredAt}); e.HasIndex(x=>x.CremationId); e.HasIndex(x=>x.ClientOperationId).IsUnique().HasFilter("\"ClientOperationId\" IS NOT NULL"); e.HasIndex(x=>x.PurchaseItemId).IsUnique().HasFilter("\"PurchaseItemId\" IS NOT NULL"); e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x=>x.Cremation).WithMany().HasForeignKey(x=>x.CremationId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x=>x.PurchaseItem).WithMany().HasForeignKey(x=>x.PurchaseItemId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x=>x.RecordedByUser).WithMany().HasForeignKey(x=>x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<FilamentSpecification>(e => { e.ToTable("EspecificacionesFilamento"); e.HasKey(x=>x.Id); e.Property(x=>x.MaterialType).HasMaxLength(80).IsRequired(); e.Property(x=>x.Brand).HasMaxLength(120); e.Property(x=>x.Color).HasMaxLength(120); e.Property(x=>x.NetUsableWeightGrams).HasColumnType("numeric(14,3)"); e.Property(x=>x.ManufacturerProductCode).HasMaxLength(150); e.Property(x=>x.ProductData).HasMaxLength(2000); e.HasIndex(x=>x.SupplyItemId).IsUnique(); e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<UrnBillOfMaterials>(e=>{e.ToTable("UrnasListaMateriales");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UrnId,x.Version}).IsUnique();e.HasIndex(x=>x.UrnId).IsUnique().HasFilter("\"IsActive\" = true");e.HasOne(x=>x.Urn).WithMany().HasForeignKey(x=>x.UrnId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<UrnBillOfMaterialsItem>(e=>{e.ToTable("UrnasListaMaterialesPartidas");e.HasKey(x=>x.Id);e.Property(x=>x.RequiredQuantity).HasColumnType("numeric(14,3)");e.HasOne(x=>x.BillOfMaterials).WithMany(x=>x.Items).HasForeignKey(x=>x.UrnBillOfMaterialsId).OnDelete(DeleteBehavior.Cascade);e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<ManufacturedUrnProduction>(e=>{e.ToTable("ProduccionesUrna");e.HasKey(x=>x.Id);e.Property(x=>x.QuantityProduced).HasColumnType("numeric(14,3)");e.HasIndex(x=>x.FinishedGoodsReceiptMovementId).IsUnique().HasFilter("\"FinishedGoodsReceiptMovementId\" IS NOT NULL");e.HasOne(x=>x.FinishedGoodsReceiptMovement).WithMany().HasForeignKey(x=>x.FinishedGoodsReceiptMovementId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Urn).WithMany().HasForeignKey(x=>x.UrnId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.UrnBillOfMaterials).WithMany().HasForeignKey(x=>x.UrnBillOfMaterialsId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.RecordedByUser).WithMany().HasForeignKey(x=>x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<ManufacturingMaterialUsage>(e=>{e.ToTable("ConsumosMaterialProduccion");e.HasKey(x=>x.Id);e.Property(x=>x.SupplyItemNameSnapshot).HasMaxLength(250).IsRequired();e.Property(x=>x.ExpectedQuantity).HasColumnType("numeric(14,3)");e.Property(x=>x.ActualQuantity).HasColumnType("numeric(14,3)");e.Property(x=>x.WasteQuantity).HasColumnType("numeric(14,3)");e.Property(x=>x.CostPerUnitSnapshot).HasColumnType("numeric(14,8)");e.Property(x=>x.TotalMaterialCostSnapshot).HasColumnType("numeric(14,4)");e.HasOne(x=>x.Production).WithMany(x=>x.MaterialUsages).HasForeignKey(x=>x.ProductionId).OnDelete(DeleteBehavior.Cascade);e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.InventoryMovement).WithMany().HasForeignKey(x=>x.InventoryMovementId).OnDelete(DeleteBehavior.Restrict);e.HasIndex(x=>x.InventoryMovementId).IsUnique();});
        modelBuilder.Entity<UrnSupplyItem>(e=>{e.ToTable("UrnasInsumosTerminados");e.HasKey(x=>x.Id);e.HasIndex(x=>x.UrnId).IsUnique().HasFilter("\"IsActive\" = true");e.HasIndex(x=>x.SupplyItemId).IsUnique();e.HasOne(x=>x.Urn).WithMany().HasForeignKey(x=>x.UrnId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<SupplyInventoryLot>(e=>{e.ToTable("LotesInventarioInsumos");e.HasKey(x=>x.Id);e.Property(x=>x.ScanCode).HasMaxLength(150).IsRequired();e.Property(x=>x.InitialQuantity).HasColumnType("numeric(14,3)");e.HasIndex(x=>x.ScanCode).IsUnique();e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.PurchaseItem).WithMany().HasForeignKey(x=>x.PurchaseItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.PurchaseReceiptItem).WithMany().HasForeignKey(x=>x.PurchaseReceiptItemId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<SupplyInventoryMovement>().HasIndex(x=>x.SupplyInventoryLotId);
        modelBuilder.Entity<SupplyInventoryMovement>().HasOne(x=>x.SupplyInventoryLot).WithMany().HasForeignKey(x=>x.SupplyInventoryLotId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CremationUrnReservation>(e=>{e.ToTable("ReservasUrnaCremacion");e.HasKey(x=>x.Id);e.Property(x=>x.Status).HasConversion<int>();e.Property(x=>x.UrnNameSnapshot).HasMaxLength(250).IsRequired();e.Property(x=>x.SupplyItemNameSnapshot).HasMaxLength(250).IsRequired();e.Property(x=>x.SupplyItemScanCodeSnapshot).HasMaxLength(150).IsRequired();e.HasIndex(x=>new{x.CremationId,x.Status}).HasFilter("\"Status\" = 1").IsUnique();e.HasOne(x=>x.Cremation).WithMany().HasForeignKey(x=>x.CremationId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Urn).WithMany().HasForeignKey(x=>x.UrnId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Fulfillment).WithOne(x=>x.UrnReservation).HasForeignKey<CremationUrnReservation>(x=>x.FulfillmentId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<CremationInventoryFulfillment>(e=>{e.ToTable("EntregasInventarioCremacion");e.HasKey(x=>x.Id);e.HasIndex(x=>x.CremationId).IsUnique();e.HasOne(x=>x.Cremation).WithMany().HasForeignKey(x=>x.CremationId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.UrnMovement).WithMany().HasForeignKey(x=>x.UrnMovementId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.FulfilledByUser).WithMany().HasForeignKey(x=>x.FulfilledByUserId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<CremationInventoryMaterial>(e=>{e.ToTable("MaterialesEntregaCremacion");e.HasKey(x=>x.Id);e.Property(x=>x.Quantity).HasColumnType("numeric(14,3)");e.Property(x=>x.SupplyItemNameSnapshot).HasMaxLength(250).IsRequired();e.Property(x=>x.UnitOfMeasureSnapshot).HasMaxLength(50).IsRequired();e.HasOne(x=>x.Fulfillment).WithMany(x=>x.Materials).HasForeignKey(x=>x.FulfillmentId).OnDelete(DeleteBehavior.Cascade);e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Lot).WithMany().HasForeignKey(x=>x.LotId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.InventoryMovement).WithMany().HasForeignKey(x=>x.InventoryMovementId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<InventoryStockCount>(e=>{e.ToTable("ConteosInventario");e.HasKey(x=>x.Id);e.Property(x=>x.SystemQuantity).HasColumnType("numeric(14,3)");e.Property(x=>x.CountedQuantity).HasColumnType("numeric(14,3)");e.Property(x=>x.Variance).HasColumnType("numeric(14,3)");e.HasIndex(x=>new{x.SupplyItemId,x.CountedAt});e.HasOne(x=>x.SupplyItem).WithMany().HasForeignKey(x=>x.SupplyItemId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.SupplyInventoryLot).WithMany().HasForeignKey(x=>x.SupplyInventoryLotId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.InventoryMovement).WithMany().HasForeignKey(x=>x.InventoryMovementId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.CountedByUser).WithMany().HasForeignKey(x=>x.CountedByUserId).OnDelete(DeleteBehavior.Restrict);});

        // Reception photo table configuration
        modelBuilder.Entity<ReceptionPhoto>(entity =>
        {
            entity.ToTable("FotosRecepcion");

            entity.HasKey(rp => rp.Id);

            entity.Property(rp => rp.Id)
                .HasColumnName("Id");

            entity.Property(rp => rp.ReceptionId)
                .HasColumnName("RecepcionId")
                .IsRequired();

            entity.Property(rp => rp.UploadedByUserId)
                .HasColumnName("SubidoPorUsuarioId")
                .IsRequired();

            entity.Property(rp => rp.PhotoType)
                .HasColumnName("TipoFoto")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(rp => rp.OriginalFileName)
                .HasColumnName("NombreArchivoOriginal")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(rp => rp.StoredFileName)
                .HasColumnName("NombreArchivoGuardado")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(rp => rp.StoragePath)
                .HasColumnName("RutaAlmacenamiento")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(rp => rp.ContentType)
                .HasColumnName("TipoContenido")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(rp => rp.Notes)
                .HasColumnName("Notas")
                .HasMaxLength(1000);

            entity.Property(rp => rp.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(rp => rp.UploadedAt)
                .HasColumnName("FechaSubida")
                .IsRequired();

            entity.HasIndex(rp => rp.ReceptionId);

            entity.HasIndex(rp => rp.StoredFileName)
                .IsUnique();

            entity.HasOne(rp => rp.Reception)
                .WithMany(r => r.Photos)
                .HasForeignKey(rp => rp.ReceptionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rp => rp.UploadedByUser)
                .WithMany()
                .HasForeignKey(rp => rp.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Collection photo table configuration
        modelBuilder.Entity<CollectionPhoto>(entity =>
        {
            entity.ToTable("FotosRecoleccion");

            entity.HasKey(cp => cp.Id);

            entity.Property(cp => cp.Id)
                .HasColumnName("Id");

            entity.Property(cp => cp.CollectionId)
                .HasColumnName("RecoleccionId")
                .IsRequired();

            entity.Property(cp => cp.UploadedByUserId)
                .HasColumnName("SubidoPorUsuarioId")
                .IsRequired();

            entity.Property(cp => cp.PhotoType)
                .HasColumnName("TipoFoto")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(cp => cp.OriginalFileName)
                .HasColumnName("NombreArchivoOriginal")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(cp => cp.StoredFileName)
                .HasColumnName("NombreArchivoGuardado")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(cp => cp.StoragePath)
                .HasColumnName("RutaAlmacenamiento")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(cp => cp.ContentType)
                .HasColumnName("TipoContenido")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(cp => cp.Notes)
                .HasColumnName("Notas")
                .HasMaxLength(1000);

            entity.Property(cp => cp.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(cp => cp.UploadedAt)
                .HasColumnName("FechaSubida")
                .IsRequired();

            entity.HasIndex(cp => cp.CollectionId);

            entity.HasIndex(cp => cp.StoredFileName)
                .IsUnique();

            entity.HasOne(cp => cp.Collection)
                .WithMany(c => c.Photos)
                .HasForeignKey(cp => cp.CollectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cp => cp.UploadedByUser)
                .WithMany()
                .HasForeignKey(cp => cp.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Collection table configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("Recolecciones");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("Id");

            entity.Property(c => c.PetId)
                .HasColumnName("MascotaId")
                .IsRequired();

            entity.Property(c => c.CollectedByUserId)
                .HasColumnName("RecolectadoPorUsuarioId")
                .IsRequired();

            entity.Property(c => c.LocationType)
                .HasColumnName("TipoUbicacion")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(c => c.VeterinaryClinicId)
                .HasColumnName("VeterinariaId");

            entity.Property(c => c.ReferringVeterinarianId)
                .HasColumnName("VeterinarioReferenteId");

            entity.Property(c => c.Status)
                .HasColumnName("Estado")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(c => c.QrCode)
                .HasColumnName("CodigoQr")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.PickupAddress)
                .HasColumnName("DireccionRecoleccion")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(c => c.PickupContactName)
                .HasColumnName("NombreContactoRecoleccion")
                .HasMaxLength(150);

            entity.Property(c => c.PickupContactPhone)
                .HasColumnName("TelefonoContactoRecoleccion")
                .HasMaxLength(30);

            entity.Property(c => c.ApproximateWeightKg)
                .HasColumnName("PesoAproximadoKg")
                .HasPrecision(10, 2);

            entity.Property(c => c.HasPersonalBelongings)
                .HasColumnName("TieneObjetosPersonales")
                .IsRequired();

            entity.Property(c => c.PersonalBelongingsDescription)
                .HasColumnName("DescripcionObjetosPersonales")
                .HasMaxLength(500);

            entity.Property(c => c.Notes)
                .HasColumnName("Notas")
                .HasMaxLength(1000);

            entity.Property(c => c.CollectedAt)
                .HasColumnName("FechaRecoleccion")
                .IsRequired();

            entity.Property(c => c.ReceivedAt)
                .HasColumnName("FechaRecepcionInstalaciones");

            entity.Property(c => c.CancelledAt)
                .HasColumnName("FechaCancelacion");

            entity.Property(c => c.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.HasIndex(c => c.QrCode)
                .IsUnique();

            entity.HasIndex(c => c.PetId);

            entity.HasIndex(c => c.CollectedByUserId);

            entity.HasIndex(c => c.VeterinaryClinicId);

            entity.HasIndex(c => c.ReferringVeterinarianId);

            entity.HasIndex(c => c.Status);

            entity.HasIndex(c => c.CollectedAt);

            entity.HasOne(c => c.Pet)
                .WithMany(p => p.Collections)
                .HasForeignKey(c => c.PetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CollectedByUser)
                .WithMany()
                .HasForeignKey(c => c.CollectedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.VeterinaryClinic)
                .WithMany()
                .HasForeignKey(c => c.VeterinaryClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ReferringVeterinarian)
                .WithMany()
                .HasForeignKey(c => c.ReferringVeterinarianId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Reception table configuration
        modelBuilder.Entity<Reception>(entity =>
    {
        entity.ToTable("Recepciones");

        entity.HasKey(r => r.Id);

        entity.Property(r => r.Id)
            .HasColumnName("Id");

        entity.Property(r => r.PetId)
            .HasColumnName("MascotaId")
            .IsRequired();

        entity.Property(r => r.PetNameSnapshot)
            .HasColumnName("NombreMascotaSnapshot")
            .IsRequired();

        entity.Property(r => r.CustomerNameSnapshot)
            .HasColumnName("NombreClienteSnapshot")
            .IsRequired();

        entity.Property(r => r.ReceivedByUserId)
            .HasColumnName("RecibidoPorUsuarioId")
            .IsRequired();

        entity.Property(r => r.CollectionId)
    .HasColumnName("RecoleccionId");

        entity.Property(r => r.VeterinaryClinicId)
            .HasColumnName("VeterinariaId");

        entity.Property(r => r.ReferringVeterinarianId)
            .HasColumnName("VeterinarioReferenteId");

        entity.Property(r => r.ReceivedAt)
            .HasColumnName("FechaRecepcion")
            .IsRequired();

        entity.Property(r => r.QrCode)
            .HasColumnName("CodigoQr")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(r => r.VerifiedWeightKg)
            .HasColumnName("PesoVerificadoKg")
            .HasPrecision(10, 2)
            .IsRequired();

        entity.Property(r => r.HasPersonalBelongings)
            .HasColumnName("TieneObjetosPersonales")
            .IsRequired();

        entity.Property(r => r.PersonalBelongingsDescription)
            .HasColumnName("DescripcionObjetosPersonales")
            .HasMaxLength(500);

        entity.Property(r => r.Notes)
            .HasColumnName("Notas")
            .HasMaxLength(1000);

        entity.Property(r => r.ReferralNotes)
            .HasColumnName("NotasReferencia")
            .HasMaxLength(1000);

        entity.Property(r => r.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        entity.Property(r => r.CreatedAt)
            .HasColumnName("FechaCreacion")
            .IsRequired();

        entity.HasIndex(r => r.QrCode)
        .IsUnique();

        entity.HasIndex(r => r.VeterinaryClinicId);

        entity.HasIndex(r => r.ReferringVeterinarianId);

        entity.HasOne(r => r.Pet)
    .WithOne(p => p.Reception)
    .HasForeignKey<Reception>(
        r => r.PetId)
    .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(r => r.Collection)
            .WithOne(c => c.Reception)
            .HasForeignKey<Reception>(
                r => r.CollectionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(r => r.ReceivedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(r => r.CollectionId)
    .IsUnique();

        entity.HasOne(r => r.VeterinaryClinic)
            .WithMany(v => v.ReferredReceptions)
            .HasForeignKey(r => r.VeterinaryClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(r => r.ReferringVeterinarian)
            .WithMany(v => v.ReferredReceptions)
            .HasForeignKey(r => r.ReferringVeterinarianId)
            .OnDelete(DeleteBehavior.Restrict);
    });


        // User table localization
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Usuarios");

            entity.Property(x => x.FirstName)
                .HasColumnName("Nombre");

            entity.Property(x => x.LastName)
                .HasColumnName("Apellido");

            entity.Property(x => x.Email)
                .HasColumnName("CorreoElectronico");

            entity.Property(x => x.PasswordHash)
                .HasColumnName("HashContrasena");

            entity.Property(x => x.IsActive)
                .HasColumnName("Activo");

            entity.Property(x => x.IsOwner)
                .HasColumnName("EsOwner")
                .HasDefaultValue(false);

            entity.HasIndex(x => x.IsOwner)
                .HasFilter("\"EsOwner\" = true")
                .IsUnique();

            entity.Property(x => x.CreatedAt)
                .HasColumnName("FechaCreacion");
        });

        // Customer table localization
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Clientes");

            entity.Property(x => x.FirstName)
                .HasColumnName("Nombre");

            entity.Property(x => x.LastName)
                .HasColumnName("ApellidoPaterno");

            entity.Property(x => x.SecondLastName)
                .HasColumnName("ApellidoMaterno")
                .IsRequired(false);

            entity.Property(x => x.Phone)
                .HasColumnName("Telefono");

            entity.Property(x => x.Email)
                .HasColumnName("CorreoElectronico");

            entity.Property(x => x.IsActive)
                .HasColumnName("Activo");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("FechaCreacion");
        });

        // Pet table localization
        modelBuilder.Entity<Pet>(entity =>
    {
        entity.ToTable("Mascotas");

        entity.Property(x => x.Name)
            .HasColumnName("Nombre");

        entity.Property(x => x.Species)
            .HasColumnName("Especie");

        entity.Property(x => x.Breed)
            .HasColumnName("Raza");

        entity.Property(x => x.Sex)
            .HasColumnName("Sexo");

        entity.Property(x => x.Color)
            .HasColumnName("Color");

        entity.Property(x => x.WeightKg)
            .HasColumnName("PesoKg");

        entity.Property(x => x.AgeYears)
            .HasColumnName("EdadAnios");

        entity.Property(x => x.DateOfDeath)
            .HasColumnName("FechaFallecimiento")
            .HasColumnType("date");

        entity.Property(x => x.IsActive)
            .HasColumnName("Activo");

        entity.Property(x => x.CreatedAt)
            .HasColumnName("FechaCreacion");

        entity.HasOne(x => x.Customer)
            .WithMany(x => x.Pets)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    });

        // UserRole relationship table localization
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UsuarioRoles");

            entity.HasKey(x => new
            {
                x.UserId,
                x.RoleId
            });


            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);


            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Permission>(entity => { entity.ToTable("Permisos"); entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(100).IsRequired(); entity.Property(x => x.Name).HasMaxLength(150).IsRequired(); entity.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<RolePermission>(entity => { entity.ToTable("RolPermisos"); entity.HasKey(x => new { x.RoleId, x.PermissionId }); entity.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade); });


        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.NormalizedName).IsUnique();
        });

        // Roles seed
        modelBuilder.Entity<Role>()
            .HasData(
                new Role
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new Role
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Usuario",
                    NormalizedName = "USUARIO"
                },
                new Role
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Finanzas",
                    NormalizedName = "FINANZAS"
                },
                new Role
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Ventas",
                    NormalizedName = "VENTAS"
                }
            );

        modelBuilder.Entity<VeterinaryClinic>(entity =>
    {
        entity.ToTable("Veterinarias");

        entity.HasKey(v => v.Id);

        entity.Property(v => v.Id)
            .HasColumnName("Id");

        entity.Property(v => v.Name)
            .HasColumnName("Nombre")
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(v => v.Phone)
            .HasColumnName("Telefono")
            .HasMaxLength(30);

        entity.Property(v => v.Email)
            .HasColumnName("CorreoElectronico")
            .HasMaxLength(150);

        entity.Property(v => v.Address)
            .HasColumnName("Direccion")
            .HasMaxLength(300);

        entity.Property(v => v.PrimaryContactName)
            .HasColumnName("NombreContactoPrincipal")
            .HasMaxLength(150);

        entity.Property(v => v.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        entity.Property(v => v.CreatedAt)
            .HasColumnName("FechaCreacion")
            .IsRequired();

        entity.HasIndex(v => v.Name);

        entity.HasIndex(v => v.Email);
                });

        var permissionDefinitions = new[]
        {
            ("Permissions.Manage", "Administrar permisos"), ("Suppliers.View", "Consultar proveedores"), ("Suppliers.Manage", "Administrar proveedores"),
            ("Finance.View", "Consultar finanzas"), ("Finance.Manage", "Administrar finanzas"), ("Inventory.View", "Consultar inventario"),
            ("Inventory.Manage", "Administrar inventario"), ("Purchasing.View", "Consultar compras"), ("Purchasing.Manage", "Administrar compras"),
            ("Customers.View", "Consultar clientes"), ("Customers.Manage", "Administrar clientes"),
            ("Pets.View", "Consultar mascotas"), ("Pets.Manage", "Administrar mascotas"),
            ("VeterinaryClinics.View", "Consultar veterinarias"), ("VeterinaryClinics.Manage", "Administrar veterinarias"),
            ("Veterinarians.View", "Consultar veterinarios"), ("Veterinarians.Manage", "Administrar veterinarios"),
            ("VeterinaryRequests.View", "Consultar solicitudes veterinarias"), ("VeterinaryRequests.Manage", "Administrar solicitudes veterinarias"),
            ("Collections.View", "Consultar recolecciones"), ("Collections.Manage", "Administrar recolecciones"),
            ("Receptions.View", "Consultar recepciones"), ("Receptions.Manage", "Administrar recepciones"),
            ("Cremations.View", "Consultar cremaciones"), ("Cremations.Manage", "Administrar cremaciones"),
            ("Payments.View", "Consultar pagos"), ("Payments.Manage", "Administrar pagos"),
            ("CremationPackages.View", "Consultar paquetes de cremación"), ("CremationPackages.Manage", "Administrar paquetes de cremación"),
            ("Urns.View", "Consultar urnas"), ("Urns.Manage", "Administrar urnas"),
            ("CremationPricing.View", "Consultar precios de cremación"), ("CremationPricing.Manage", "Administrar precios de cremación"),
            ("Inventory.ScanOutgoing", "Registrar salidas por escaneo")
        };
        var permissionSeed = permissionDefinitions.Select((x, i) => new Permission { Id = Guid.Parse($"{i + 1:00000000}-0000-0000-0000-000000000001"), Code = x.Item1, Name = x.Item2 }).ToArray();
        modelBuilder.Entity<Permission>().HasData(permissionSeed);
        modelBuilder.Entity<RolePermission>().HasData(permissionSeed.Select(x => new RolePermission { RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = x.Id }));

        modelBuilder.Entity<Veterinarian>(entity =>
    {
        entity.ToTable("Veterinarios");

        entity.HasKey(v => v.Id);

        entity.Property(v => v.Id)
            .HasColumnName("Id");

        entity.Property(v => v.VeterinaryClinicId)
            .HasColumnName("VeterinariaId")
            .IsRequired(false);

        entity.Property(v => v.FirstName)
            .HasColumnName("Nombre")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(v => v.LastName)
            .HasColumnName("ApellidoPaterno")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.SecondLastName)
            .HasColumnName("ApellidoMaterno")
            .IsRequired(false);

        entity.Property(v => v.Phone)
            .HasColumnName("Telefono")
            .HasMaxLength(30);

        entity.Property(v => v.Email)
            .HasColumnName("CorreoElectronico")
            .HasMaxLength(150);

        entity.Property(v => v.ProfessionalLicenseNumber)
            .HasColumnName("CedulaProfesional")
            .HasMaxLength(50);

        entity.Property(v => v.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        entity.Property(v => v.CreatedAt)
            .HasColumnName("FechaCreacion")
            .IsRequired();

        entity.HasOne(x => x.VeterinaryClinic)
            .WithMany(x => x.Veterinarians)
            .HasForeignKey(x => x.VeterinaryClinicId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(v => v.VeterinaryClinicId);

        entity.HasIndex(v => v.Email);

        entity.HasIndex(v => v.ProfessionalLicenseNumber);
    });

        // Veterinary request table configuration
        modelBuilder.Entity<VeterinaryRequest>(entity =>
        {
            entity.ToTable("SolicitudesVeterinarias");

            entity.HasKey(vr => vr.Id);

            entity.Property(vr => vr.Id)
                .HasColumnName("Id");

            entity.Property(vr => vr.VeterinaryClinicId)
                .HasColumnName("VeterinariaId");

            entity.Property(vr => vr.ReferringVeterinarianId)
                .HasColumnName("VeterinarioReferenteId");

            entity.Property(vr => vr.SubmittedByUserId)
                .HasColumnName("RegistradoPorUsuarioId")
                .IsRequired();

            entity.Property(vr => vr.ReviewedByUserId)
                .HasColumnName("RevisadoPorUsuarioId");

            entity.Property(vr => vr.ReceptionId)
                .HasColumnName("RecepcionId");

            entity.Property(vr => vr.Status)
                .HasColumnName("Estado")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(vr => vr.OwnerFirstName)
                .HasColumnName("NombrePropietario")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(vr => vr.OwnerLastName)
                .HasColumnName("ApellidoPaternoPropietario")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(vr => vr.OwnerSecondLastName)
                .HasColumnName("ApellidoMaternoPropietario")
                .HasMaxLength(100);

            entity.Property(vr => vr.OwnerPhone)
                .HasColumnName("TelefonoPropietario")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(vr => vr.OwnerEmail)
                .HasColumnName("CorreoPropietario")
                .HasMaxLength(150);

            entity.Property(vr => vr.PetName)
                .HasColumnName("NombreMascota")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(vr => vr.Species)
                .HasColumnName("Especie")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(vr => vr.Breed)
                .HasColumnName("Raza")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(vr => vr.Sex)
                .HasColumnName("Sexo")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(vr => vr.Color)
                .HasColumnName("Color")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(vr => vr.ApproximateWeightKg)
                .HasColumnName("PesoAproximadoKg")
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(vr => vr.AgeYears)
                .HasColumnName("EdadAnios");

            entity.Property(vr => vr.DateOfDeath)
                .HasColumnName("FechaFallecimiento")
                .IsRequired();

            entity.Property(vr => vr.RequestedCremationType)
                .HasColumnName("TipoCremacionSolicitado")
                .HasConversion<int>();

            entity.Property(vr => vr.RequestedPackageName)
                .HasColumnName("NombrePaqueteSolicitado")
                .HasMaxLength(150);

            entity.Property(vr => vr.RequestNotes)
                .HasColumnName("NotasSolicitud")
                .HasMaxLength(1000);

            entity.Property(vr => vr.InternalNotes)
                .HasColumnName("NotasInternas")
                .HasMaxLength(1000);

            entity.Property(vr => vr.RejectionReason)
                .HasColumnName("MotivoRechazo")
                .HasMaxLength(1000);

            entity.Property(vr => vr.SubmittedAt)
                .HasColumnName("FechaSolicitud")
                .IsRequired();

            entity.Property(vr => vr.ReviewedAt)
                .HasColumnName("FechaRevision");

            entity.Property(vr => vr.ConvertedAt)
                .HasColumnName("FechaConversion");

            entity.Property(vr => vr.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.HasIndex(vr => vr.Status);

            entity.HasIndex(vr => vr.VeterinaryClinicId);

            entity.HasIndex(vr => vr.ReferringVeterinarianId);

            entity.HasIndex(vr => vr.SubmittedByUserId);

            entity.HasIndex(vr => vr.ReviewedByUserId);

            entity.HasIndex(vr => vr.SubmittedAt);

            entity.HasIndex(vr => vr.ReceptionId)
                .IsUnique();

            entity.HasOne(vr => vr.VeterinaryClinic)
                .WithMany()
                .HasForeignKey(vr => vr.VeterinaryClinicId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(vr => vr.ReferringVeterinarian)
                .WithMany()
                .HasForeignKey(vr => vr.ReferringVeterinarianId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(vr => vr.SubmittedByUser)
                .WithMany()
                .HasForeignKey(vr => vr.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(vr => vr.ReviewedByUser)
                .WithMany()
                .HasForeignKey(vr => vr.ReviewedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(vr => vr.Reception)
                .WithOne()
                .HasForeignKey<VeterinaryRequest>(
                    vr => vr.ReceptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Cremation package catalog configuration
        modelBuilder.Entity<CremationPackage>(entity =>
        {
            entity.ToTable("PaquetesCremacion");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("Id");

            entity.Property(p => p.Name)
                .HasColumnName("Nombre")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(p => p.ShortDescription)
                .HasColumnName("DescripcionCorta")
                .HasMaxLength(250);

            entity.Property(p => p.Description)
                .HasColumnName("Descripcion")
                .HasMaxLength(1000);

            entity.Property(p => p.IncludesUrn)
                .HasColumnName("IncluyeUrna")
                .IsRequired();

            entity.Property(p => p.IncludesPawPrint)
                .HasColumnName("IncluyeHuella")
                .IsRequired();

            entity.Property(p => p.AccessoryDescription)
                .HasColumnName("DescripcionAccesorio")
                .HasMaxLength(500);

            entity.Property(p => p.IncludesCertificate)
                .HasColumnName("IncluyeCertificado")
                .IsRequired();

            entity.Property(p => p.ImageUrl)
                .HasColumnName("UrlImagen")
                .HasMaxLength(500);

            entity.Property(p => p.IsPublic)
                .HasColumnName("EsPublico")
                .IsRequired();

            entity.Property(p => p.DisplayOrder)
                .HasColumnName("OrdenVisualizacion")
                .IsRequired();

            entity.Property(p => p.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .HasColumnName("FechaActualizacion");

            entity.Property(e => e.PackageType)
            .HasColumnName("TipoPaquete")
            .HasConversion<int>();

            entity.Property(e => e.Tier)
        .HasColumnName("Nivel");

            entity.HasIndex(p => p.Name);

            entity.HasIndex(p => p.IsActive);

            entity.HasIndex(p => p.IsPublic);

            entity.HasIndex(p => p.DisplayOrder);
        });

        // Cremation Pricing configuration
        modelBuilder.Entity<CremationPricingConfiguration>(entity =>
        {
            entity.ToTable("ConfiguracionPreciosCremacion");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.WeightInterval)
                .HasColumnName("IntervaloPesoKg")
                .HasConversion<int>();

            entity.Property(e => e.AllowIndividualNoAshes)
                .HasColumnName("PermitirIndividualSinCenizas");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("FechaCreacion");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("FechaActualizacion");
        });

        // Cremation Price
        modelBuilder.Entity<CremationPrice>(entity =>
        {
            entity.ToTable("PreciosCremacion");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.CremationPackageId)
                .HasColumnName("PaqueteCremacionId");

            entity.Property(e => e.CremationType)
                .HasColumnName("TipoCremacion")
                .HasConversion<int>();

            entity.Property(e => e.MinimumWeightKg)
                .HasColumnName("PesoMinimoKg")
                .HasPrecision(10, 2);

            entity.Property(e => e.MaximumWeightKg)
                .HasColumnName("PesoMaximoKg")
                .HasPrecision(10, 2);

            entity.Property(e => e.Price)
                .HasColumnName("Precio")
                .HasPrecision(12, 2);

            entity.Property(x => x.IsPublic)
            .HasColumnName("EsPublico")
            .HasDefaultValue(true);

            entity.Property(e => e.IsActive)
                .HasColumnName("Activo");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("FechaCreacion");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("FechaActualizacion");

            entity.HasOne(e => e.CremationPackage)
                .WithMany(e => e.Prices)
                .HasForeignKey(e => e.CremationPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new
            {
                e.CremationPackageId,
                e.CremationType,
                e.MinimumWeightKg,
                e.MaximumWeightKg
            })
                .IsUnique();

            entity.HasIndex(e => e.IsActive);
        });

        // Urn catalog configuration
        modelBuilder.Entity<Urn>(entity =>
        {
            entity.ToTable("Urnas");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                .HasColumnName("Id");

            entity.Property(u => u.Name)
                .HasColumnName("Nombre")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.Description)
                .HasColumnName("Descripcion")
                .HasMaxLength(1000);

            entity.Property(u => u.Material)
                .HasColumnName("Material")
                .HasMaxLength(100);

            entity.Property(u => u.Color)
                .HasColumnName("Color")
                .HasMaxLength(100);

            entity.Property(u => u.ImageUrl)
                .HasColumnName("UrlImagen")
                .HasMaxLength(500);

            entity.Property(u => u.IsPublic)
                .HasColumnName("Publico")
                .IsRequired();

            entity.Property(u => u.DisplayOrder)
                .HasColumnName("OrdenVisualizacion")
                .IsRequired();

            entity.Property(u => u.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .HasColumnName("FechaActualizacion");

            entity.HasIndex(u => u.Name);

            entity.HasIndex(u => u.IsActive);

            entity.HasIndex(u => u.IsPublic);

            entity.HasIndex(u => u.DisplayOrder);
        });

        // Allowed urns per cremation package
        modelBuilder.Entity<CremationPackageUrn>(entity =>
        {
            entity.ToTable("PaquetesCremacionUrnas");

            entity.HasKey(option => option.Id);

            entity.Property(option => option.Id)
                .HasColumnName("Id");

            entity.Property(option => option.CremationPackageId)
                .HasColumnName("PaqueteCremacionId")
                .IsRequired();

            entity.Property(option => option.UrnId)
                .HasColumnName("UrnaId")
                .IsRequired();

            entity.Property(option => option.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(option => option.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.Property(option => option.UpdatedAt)
                .HasColumnName("FechaActualizacion");

            entity.HasOne(option => option.CremationPackage)
                .WithMany(package => package.UrnOptions)
                .HasForeignKey(option => option.CremationPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(option => option.Urn)
                .WithMany(urn => urn.PackageOptions)
                .HasForeignKey(option => option.UrnId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(option => new
            {
                option.CremationPackageId,
                option.UrnId
            })
                .IsUnique();

            entity.HasIndex(option => option.IsActive);
        });

        // Cremation table configuration
        modelBuilder.Entity<Cremation>(entity =>
        {
            entity.ToTable("Cremaciones");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("Id");

            entity.Property(c => c.ReceptionId)
                .HasColumnName("RecepcionId")
                .IsRequired();

            entity.Property(c => c.AssignedToUserId)
                .HasColumnName("AsignadoAUsuarioId");

            entity.Property(c => c.CremationPackageId)
    .HasColumnName("PaqueteCremacionId");

            entity.Property(c => c.UrnId)
                .HasColumnName("UrnaId");

            entity.Property(c => c.CremationType)
                .HasColumnName("TipoCremacion")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(c => c.Status)
                .HasColumnName("Estado")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(c => c.PackageName)
                .HasColumnName("NombrePaquete")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(c => c.IncludesUrn)
                .HasColumnName("IncluyeUrna")
                .IsRequired();

            entity.Property(c => c.UrnDescription)
                .HasColumnName("DescripcionUrna")
                .HasMaxLength(500);

            entity.Property(c => c.IncludesPawPrint)
                .HasColumnName("IncluyeHuella")
                .IsRequired();

            entity.Property(c => c.AccessoryDescription)
    .HasColumnName("DescripcionAccesorio")
    .HasMaxLength(500);

            entity.Property(c => c.IncludesCertificate)
                .HasColumnName("IncluyeCertificado")
                .IsRequired();

            entity.Property(x => x.QuotedPrice)
    .HasColumnName("PrecioCotizado")
    .HasColumnType("numeric(12,2)");

            entity.Property(x => x.QuotedWeightKg)
                .HasColumnName("PesoCotizadoKg")
                .HasColumnType("numeric(10,2)");

            entity.Property(x => x.QuotedMinimumWeightKg)
                .HasColumnName("PesoMinimoCotizadoKg")
                .HasColumnType("numeric(10,2)");

            entity.Property(x => x.QuotedMaximumWeightKg)
                .HasColumnName("PesoMaximoCotizadoKg")
                .HasColumnType("numeric(10,2)");

            entity.Property(c => c.ScheduledAt)
                .HasColumnName("FechaProgramada");

            entity.Property(c => c.StartedAt)
                .HasColumnName("FechaInicio");

            entity.Property(c => c.CompletedAt)
                .HasColumnName("FechaFinalizacion");

            entity.Property(c => c.ReadyForDeliveryAt)
                .HasColumnName("FechaListaParaEntrega");

            entity.Property(c => c.DeliveredAt)
                .HasColumnName("FechaEntrega");

            entity.Property(c => c.SpecialInstructions)
                .HasColumnName("InstruccionesEspeciales")
                .HasMaxLength(1000);

            entity.Property(c => c.Notes)
                .HasColumnName("Notas")
                .HasMaxLength(1000);

            entity.Property(c => c.IsActive)
                .HasColumnName("Activo")
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.HasIndex(c => c.ReceptionId)
                .IsUnique();

            entity.HasIndex(c => c.AssignedToUserId);

            entity.HasIndex(c => c.Status);

            entity.HasIndex(c => c.ScheduledAt);

            entity.HasIndex(c => c.CremationPackageId);

            entity.HasIndex(c => c.UrnId);

            entity.HasOne(c => c.Reception)
                .WithOne(r => r.Cremation)
                .HasForeignKey<Cremation>(c => c.ReceptionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.AssignedToUser)
                .WithMany()
                .HasForeignKey(c => c.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CremationPackage)
    .WithMany(p => p.Cremations)
    .HasForeignKey(c => c.CremationPackageId)
    .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Urn)
                .WithMany(u => u.Cremations)
                .HasForeignKey(c => c.UrnId)
                .OnDelete(DeleteBehavior.Restrict);

        });

        // Payment account table configuration
        modelBuilder.Entity<PaymentAccount>(entity =>
        {
            entity.ToTable("CuentasPago");

            entity.HasKey(pa => pa.Id);

            entity.Property(pa => pa.Id)
                .HasColumnName("Id");

            entity.Property(pa => pa.CremationId)
                .HasColumnName("CremacionId")
                .IsRequired();

            entity.Property(pa => pa.ServiceTotal)
                .HasColumnName("TotalServicio")
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(pa => pa.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.Property(pa => pa.UpdatedAt)
                .HasColumnName("FechaActualizacion");

            entity.HasIndex(pa => pa.CremationId)
                .IsUnique();

            entity.HasOne(pa => pa.Cremation)
                .WithOne(c => c.PaymentAccount)
                .HasForeignKey<PaymentAccount>(
                    pa => pa.CremationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment transaction table configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Pagos");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("Id");

            entity.Property(p => p.PaymentAccountId)
                .HasColumnName("CuentaPagoId")
                .IsRequired();

            entity.Property(p => p.RecordedByUserId)
                .HasColumnName("RegistradoPorUsuarioId")
                .IsRequired();

            entity.Property(p => p.Amount)
                .HasColumnName("Monto")
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(p => p.Method)
                .HasColumnName("MetodoPago")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(p => p.PaidAt)
                .HasColumnName("FechaPago")
                .IsRequired();

            entity.Property(p => p.Reference)
                .HasColumnName("Referencia")
                .HasMaxLength(150);

            entity.Property(p => p.Notes)
                .HasColumnName("Notas")
                .HasMaxLength(1000);

            entity.Property(p => p.CreatedAt)
                .HasColumnName("FechaCreacion")
                .IsRequired();

            entity.HasIndex(p => p.PaymentAccountId);

            entity.HasIndex(p => p.RecordedByUserId);

            entity.HasIndex(p => p.PaidAt);

            entity.HasOne(p => p.PaymentAccount)
                .WithMany(pa => pa.Payments)
                .HasForeignKey(p => p.PaymentAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.RecordedByUser)
                .WithMany()
                .HasForeignKey(p => p.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
