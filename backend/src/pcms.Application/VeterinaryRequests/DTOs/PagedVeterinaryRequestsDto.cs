namespace pcms.Application.VeterinaryRequests.DTOs;

public class PagedVeterinaryRequestsDto
{
    public IEnumerable<VeterinaryRequestListItemDto> Items { get; set; }
        = new List<VeterinaryRequestListItemDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
