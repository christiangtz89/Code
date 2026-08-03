namespace pcms.Application.Veterinarians.DTOs;

public class PagedVeterinariansDto
{
    public IEnumerable<VeterinarianDto> Items { get; set; }
        = new List<VeterinarianDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}