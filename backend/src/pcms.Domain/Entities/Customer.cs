namespace pcms.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }

    public string FirstName { get; set; }
        = string.Empty;

    // Apellido paterno — obligatorio
    public string LastName { get; set; }
        = string.Empty;

    // Apellido materno — opcional
    public string? SecondLastName { get; set; }

    public string Phone { get; set; }
        = string.Empty;

    public string Email { get; set; }
        = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Pet> Pets { get; set; }
        = new List<Pet>();
}