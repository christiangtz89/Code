namespace pcms.Application.VeterinaryRequests.DTOs;

public class PagedVeterinaryRequestsDto
{
    public IEnumerable<VeterinaryRequestDto> Items { get; set; }
        = new List<VeterinaryRequestDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}