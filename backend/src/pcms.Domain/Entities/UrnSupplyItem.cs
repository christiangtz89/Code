namespace pcms.Domain.Entities;
public class UrnSupplyItem { public Guid Id { get; set; } public Guid UrnId { get; set; } public Urn Urn { get; set; } = null!; public Guid SupplyItemId { get; set; } public SupplyItem SupplyItem { get; set; } = null!; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } }
