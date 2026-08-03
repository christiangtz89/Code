using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class ReceptionPhoto
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public ReceptionPhotoType PhotoType { get; set; }

    public string OriginalFileName { get; set; }
        = string.Empty;

    public string StoredFileName { get; set; }
        = string.Empty;

    public string StoragePath { get; set; }
        = string.Empty;

    public string ContentType { get; set; }
        = string.Empty;

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime UploadedAt { get; set; }
        = DateTime.UtcNow;

    public Reception Reception { get; set; } = null!;

    public User UploadedByUser { get; set; } = null!;
}