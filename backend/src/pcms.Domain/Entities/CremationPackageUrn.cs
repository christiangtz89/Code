namespace pcms.Domain.Entities;

public class CremationPackageUrn
{
    public Guid Id { get; set; }

    public Guid CremationPackageId { get; set; }

    public Guid UrnId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public CremationPackage CremationPackage { get; set; } = null!;

    public Urn Urn { get; set; } = null!;
}
