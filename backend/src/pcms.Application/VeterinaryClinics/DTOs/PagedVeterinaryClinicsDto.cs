namespace pcms.Application.VeterinaryClinics.DTOs;

public class PagedVeterinaryClinicsDto
{
    public IEnumerable<VeterinaryClinicDto> Items { get; set; }
        = new List<VeterinaryClinicDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}