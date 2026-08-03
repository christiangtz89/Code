namespace pcms.Application.Receptions.DTOs;

public class PagedReceptionsDto
{
    public IEnumerable<ReceptionDto> Items { get; set; }
        = new List<ReceptionDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}