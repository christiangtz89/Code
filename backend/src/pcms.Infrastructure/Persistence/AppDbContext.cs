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

    public DbSet<Reception> Receptions { get; set; }

    public DbSet<ReceptionPhoto> ReceptionPhotos { get; set; }

    public DbSet<Cremation> Cremations { get; set; }

    public DbSet<VeterinaryClinic> VeterinaryClinics { get; set; }

    public DbSet<Veterinarian> Veterinarians { get; set; }



   protected override void OnModelCreating(
    ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

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

    entity.Property(r => r.ReceivedByUserId)
        .HasColumnName("RecibidoPorUsuarioId")
        .IsRequired();

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
        .HasForeignKey<Reception>(r => r.PetId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(r => r.ReceivedByUser)
        .WithMany()
        .HasForeignKey(r => r.ReceivedByUserId)
        .OnDelete(DeleteBehavior.Restrict);

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
        .HasColumnName("FechaFallecimiento");

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


    // Roles seed
    modelBuilder.Entity<Role>()
        .HasData(
            new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin"
            },
            new Role
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Usuario"
            },
            new Role
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Finanzas"
            },
            new Role
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Ventas"
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

    entity.Property(c => c.IncludesCertificate)
        .HasColumnName("IncluyeCertificado")
        .IsRequired();

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

    entity.HasOne(c => c.Reception)
        .WithOne(r => r.Cremation)
        .HasForeignKey<Cremation>(c => c.ReceptionId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(c => c.AssignedToUser)
        .WithMany()
        .HasForeignKey(c => c.AssignedToUserId)
        .OnDelete(DeleteBehavior.Restrict);
});

}
}