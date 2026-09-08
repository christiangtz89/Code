using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace pcms.Infrastructure.Services;

internal static class CustomerPetInputRules
{
    private static readonly EmailAddressAttribute EmailValidator = new();
    private static readonly Regex PhoneValidator = new(
        @"^[0-9+\-\s()]+$",
        RegexOptions.CultureInvariant);

    public static NormalizedCustomerInput NormalizeCustomer(
        string? firstName,
        string? lastName,
        string? secondLastName,
        string? phone,
        string? email)
    {
        var normalizedPhone = NormalizeRequired(
            phone,
            "El teléfono",
            7,
            25);

        if (!PhoneValidator.IsMatch(normalizedPhone))
        {
            throw new ArgumentException(
                "El teléfono contiene caracteres no válidos.");
        }

        var normalizedEmail = NormalizeRequired(
            email,
            "El correo electrónico",
            1,
            200);

        if (!EmailValidator.IsValid(normalizedEmail))
        {
            throw new ArgumentException(
                "El correo electrónico no es válido.");
        }

        return new NormalizedCustomerInput(
            NormalizeRequired(firstName, "El nombre", 2, 100),
            NormalizeRequired(lastName, "El apellido", 2, 100),
            NormalizeOptional(secondLastName, "El apellido materno", 100),
            normalizedPhone,
            normalizedEmail);
    }

    public static NormalizedPetInput NormalizePet(
        string? name,
        string? species,
        string? breed,
        string? sex,
        string? color) =>
        new(
            NormalizeRequired(name, "El nombre de la mascota", 2, 100),
            NormalizeRequired(species, "La especie", 1, 50),
            NormalizeRequired(breed, "La raza", 1, 100),
            NormalizeRequired(sex, "El sexo", 1, 20),
            NormalizeRequired(color, "El color", 1, 100));

    private static string NormalizeRequired(
        string? value,
        string fieldName,
        int minimumLength,
        int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (normalized.Length < minimumLength)
        {
            throw new ArgumentException(
                minimumLength == 1
                    ? $"{fieldName} es obligatorio."
                    : $"{fieldName} debe tener al menos {minimumLength} caracteres.");
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{fieldName} no puede exceder {maximumLength} caracteres.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        string fieldName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{fieldName} no puede exceder {maximumLength} caracteres.");
        }

        return normalized;
    }
}

internal readonly record struct NormalizedCustomerInput(
    string FirstName,
    string LastName,
    string? SecondLastName,
    string Phone,
    string Email);

internal readonly record struct NormalizedPetInput(
    string Name,
    string Species,
    string Breed,
    string Sex,
    string Color);
