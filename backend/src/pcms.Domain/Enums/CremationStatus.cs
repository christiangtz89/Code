namespace pcms.Domain.Enums;

public enum CremationStatus
{
    Pending = 1,
    Scheduled = 2,
    InProgress = 3,
    Cooling = 4,
    ProcessingRemains = 5,
    Completed = 6,
    ReadyForDelivery = 7,
    Delivered = 8,
    Cancelled = 9
}