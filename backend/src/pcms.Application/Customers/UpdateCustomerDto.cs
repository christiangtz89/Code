using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Customers;

public class UpdateCustomerDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string FirstName { get; set; }
        = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
    public string LastName { get; set; }
        = string.Empty;

    [StringLength(
        100,
        ErrorMessage = "El apellido materno no puede exceder 100 caracteres.")]
    public string? SecondLastName { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(
        25,
        MinimumLength = 7,
        ErrorMessage = "El teléfono debe tener entre 7 y 25 caracteres.")]
    [RegularExpression(
        @"^[0-9+\-\s()]+$",
        ErrorMessage = "El teléfono contiene caracteres no válidos.")]
    public string Phone { get; set; }
        = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(
        200,
        ErrorMessage = "El correo electrónico no puede exceder 200 caracteres.")]
    public string Email { get; set; }
        = string.Empty;
}
