namespace pcms.Application.Collections.DTOs;

public class PagedCollectionsDto
{
    public IEnumerable<CollectionDto> Items { get; set; }
        = new List<CollectionDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}