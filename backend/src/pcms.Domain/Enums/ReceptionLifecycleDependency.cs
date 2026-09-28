namespace pcms.Domain.Enums;

[Flags]
public enum ReceptionLifecycleDependency
{
    None = 0,
    Collection = 1,
    ConvertedVeterinaryRequest = 2,
    Cremation = 4,
    PaymentAccount = 8,
    PaymentHistory = 16,
    DirectReceptionEvidence = 32,
    CollectionEvidence = 64
}
