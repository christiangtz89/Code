namespace pcms.Application.Supplies.DTOs;
public record ExpenseCategoryDto(Guid Id,string Name,string? Description,bool IsActive,DateTime CreatedAt,DateTime? UpdatedAt);
public record ExpenseCategoryInput(string Name,string? Description);
public record ExpenseDto(Guid Id,Guid ExpenseCategoryId,string CategoryName,Guid? SupplierId,string? SupplierName,DateTime ExpenseDate,string Description,decimal Subtotal,decimal Tax,decimal Total,string Currency,string? InvoiceReference,string? Notes,Guid? RecordedByUserId,bool IsActive,DateTime CreatedAt,DateTime? UpdatedAt);
public record ExpenseInput(Guid ExpenseCategoryId,Guid? SupplierId,DateTime ExpenseDate,string Description,decimal Subtotal,decimal Tax,string Currency,string? InvoiceReference,string? Notes);
public record PagedExpensesDto(IEnumerable<ExpenseDto> Items,int Page,int PageSize,int TotalItems,int TotalPages);
