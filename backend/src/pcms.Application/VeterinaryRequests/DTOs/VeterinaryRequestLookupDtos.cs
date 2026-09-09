namespace pcms.Application.VeterinaryRequests.DTOs;

public class VeterinaryRequestClinicOptionDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}

public class VeterinaryRequestVeterinarianOptionDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public Guid? VeterinaryClinicId { get; set; }
}

public class VeterinaryRequestCustomerOptionDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}

public class VeterinaryRequestPetOptionDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
