using pcms.Domain.Entities;

namespace pcms.Infrastructure.Services;

internal static class CustomerPetWorkflowRules
{
    private static readonly TimeZoneInfo BusinessTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    public static Pet RequireEligiblePet(Pet? pet)
    {
        if (pet is null)
            throw new ArgumentException("La mascota no existe.");
        if (!pet.IsActive)
            throw new InvalidOperationException("La mascota está inactiva.");

        EnsureCustomerIsActive(pet.Customer);
        return pet;
    }

    public static void EnsureCustomerIsActive(Customer customer)
    {
        if (!customer.IsActive)
            throw new InvalidOperationException(
                "La mascota no es elegible para esta operación porque el cliente propietario está inactivo.");
    }

    public static ReceptionIdentitySnapshot CaptureReceptionIdentity(Pet pet) =>
        CaptureReceptionIdentity(pet, pet.Customer);

    public static ReceptionIdentitySnapshot CaptureReceptionIdentity(
        Pet pet,
        Customer customer) =>
        new(
            pet.Name,
            string.Join(
                " ",
                new[]
                {
                    customer.FirstName,
                    customer.LastName,
                    customer.SecondLastName
                }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())));

    public static DateOnly CurrentBusinessDate()
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            BusinessTimeZone);
        return DateOnly.FromDateTime(localNow);
    }
}

internal readonly record struct ReceptionIdentitySnapshot(
    string PetName,
    string CustomerName);
