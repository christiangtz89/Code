namespace pcms.Domain.Entities;
public class ExpenseCategory { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public string? Description { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } public ICollection<Expense> Expenses { get; set; } = new List<Expense>(); }
