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



    protected override void OnModelCreating(
    ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);


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
        .HasColumnName("Apellido");

    entity.Property(x => x.Phone)
        .HasColumnName("Telefono");

    entity.Property(x => x.Email)
        .HasColumnName("CorreoElectronico");

    entity.Property(x => x.IsActive)
        .HasColumnName("Activo");

    entity.Property(x => x.CreatedAt)
        .HasColumnName("FechaCreacion");
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
}
}