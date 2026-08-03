using pcms.Domain.Enums;

namespace pcms.Application.Receptions.Photos.DTOs;

public class ReceptionPhotoDto
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string UploadedByUserName { get; set; }
        = string.Empty;

    public ReceptionPhotoType PhotoType { get; set; }

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