namespace pcms.Application.Cremations.DTOs;

public class PagedCremationsDto
{
    public IEnumerable<CremationDto> Items { get; set; }
        = new List<CremationDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}