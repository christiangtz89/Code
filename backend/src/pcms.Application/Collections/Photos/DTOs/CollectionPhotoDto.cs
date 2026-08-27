using pcms.Domain.Enums;

namespace pcms.Application.Collections.Photos.DTOs;

public class CollectionPhotoDto
{
    public Guid Id { get; set; }

    public Guid CollectionId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string UploadedByUserName { get; set; }
        = string.Empty;

    public CollectionPhotoType PhotoType { get; set; }

    public string OriginalFileName { get; set; }
        = string.Empty;

    public string ContentType { get; set; }
        = string.Empty;

    public string FileUrl { get; set; }
        = string.Empty;

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime UploadedAt { get; set; }
}