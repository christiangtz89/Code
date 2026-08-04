using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Customers;

public class UpdateCustomerDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; }
        = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; }
        = string.Empty;

    [StringLength(100)]
    public string? SecondLastName { get; set; }

    [Required]
    [StringLength(25)]
    public string Phone { get; set; }
        = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; }
        = string.Empty;
}