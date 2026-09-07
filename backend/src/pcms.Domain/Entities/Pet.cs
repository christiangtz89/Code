namespace pcms.Domain.Entities;

public class Pet
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Species { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public string Sex { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }

    public int? AgeYears { get; set; }

    public DateOnly DateOfDeath { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;

    public Reception? Reception { get; set; }

    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
}
