namespace pcms.Application.Collections.DTOs;

public class CollectionCustomerOptionDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}

public class CollectionPetOptionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Species { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }
}
