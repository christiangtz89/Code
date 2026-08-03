namespace pcms.Application.Pets.DTOs;

public class PagedPetsDto
{
    public IEnumerable<PetDto> Items { get; set; }
        = Enumerable.Empty<PetDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}