namespace pcms.Application.Reporting;
public interface IInventoryReportService { Task<InventoryReportDto> GetAsync(DateTime? startDate,DateTime? endDate,string? search,pcms.Domain.Enums.SupplyInventoryMovementType? movementType); }
