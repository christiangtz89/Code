namespace pcms.Application.Collections.Photos.DTOs;

public class CollectionPhotoFileDto
{
    public Stream Content { get; set; } = Stream.Null;

    public string ContentType { get; set; }
        = string.Empty;
}