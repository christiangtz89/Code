using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Collections.DTOs;

public class AssignCollectionDto
{
    [Required(ErrorMessage = "Debe seleccionar un conductor.")]
    public Guid DriverUserId { get; set; }
}
