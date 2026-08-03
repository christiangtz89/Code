namespace pcms.Application.Pets.DTOs;

public class PetDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Species { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public string Sex { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }

    public int? AgeYears { get; set; }

    public DateTime DateOfDeath { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}