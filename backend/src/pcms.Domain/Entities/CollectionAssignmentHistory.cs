namespace pcms.Domain.Entities;

public class CollectionAssignmentHistory
{
    public Guid Id { get; set; }

    public Guid CollectionId { get; set; }

    public Guid AssignedDriverId { get; set; }

    public string AssignedDriverNameSnapshot { get; set; } = string.Empty;

    public Guid AssignedByUserId { get; set; }

    public string AssignedByUserNameSnapshot { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public string? AcceptedByUserNameSnapshot { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public Guid? EndedByUserId { get; set; }

    public string? EndedByUserNameSnapshot { get; set; }

    public DateTime? EndedAt { get; set; }

    public Collection Collection { get; set; } = null!;

    public User AssignedDriver { get; set; } = null!;

    public User AssignedByUser { get; set; } = null!;

    public User? AcceptedByUser { get; set; }

    public User? EndedByUser { get; set; }
}
