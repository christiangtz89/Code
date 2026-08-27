import { apiClient } from "../../../services/apiClient";
export interface Supplier {
  id: string;
  name: string;
  legalName: string | null;
  taxId: string | null;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  address: string | null;
  notes: string | null;
  isActive: boolean;
}
export interface SupplyItem {
  id: string;
  name: string;
  description: string | null;
  internalSku: string | null;
  category: string;
  unitOfMeasure: string;
  trackInventory: boolean;
  minimumQuantity: number;
  scanCode: string;
  isActive: boolean;
}
export interface Expense {
  id: string;
  categoryName: string;
  supplierName: string | null;
  expenseDate: string;
  description: string;
  subtotal: number;
  tax: number;
  total: number;
  currency: string;
  isActive: boolean;
}
export interface ExpenseCategory {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
export const getSuppliers = async (search = "", isActive = true) =>
  (
    await apiClient.get<Page<Supplier>>("/Suppliers", {
      params: { search, isActive, page: 1, pageSize: 100 },
    })
  ).data;
export const createSupplier = async (payload: Record<string, unknown>) =>
  (await apiClient.post<Supplier>("/Suppliers", payload)).data;
export const updateSupplier = async (
  id: string,
  payload: Record<string, unknown>,
) => (await apiClient.put<Supplier>(`/Suppliers/${id}`, payload)).data;
export const deactivateSupplier = async (id: string) =>
  apiClient.delete(`/Suppliers/${id}`);
export const restoreSupplier = async (id: string) =>
  apiClient.patch(`/Suppliers/${id}/restore`);
export const getSupplyItems = async (search = "") =>
  (
    await apiClient.get<Page<SupplyItem>>("/SupplyItems", {
      params: { search, isActive: true, page: 1, pageSize: 100 },
    })
  ).data;
export const getSupplyItemByScan = async (code: string) =>
  (
    await apiClient.get<SupplyItem>(
      `/SupplyItems/scan/${encodeURIComponent(code)}`,
    )
  ).data;
export const createSupplyItem = async (payload: Record<string, unknown>) =>
  (await apiClient.post<SupplyItem>("/SupplyItems", payload)).data;
export const getExpenses = async () =>
  (
    await apiClient.get<Page<Expense>>("/Expenses", {
      params: { isActive: true, page: 1, pageSize: 100 },
    })
  ).data;
export const getExpenseCategories = async () =>
  (await apiClient.get<ExpenseCategory[]>("/Expenses/categories")).data;
export const createExpense = async (payload: Record<string, unknown>) =>
  (await apiClient.post<Expense>("/Expenses", payload)).data;
export interface SpendingSummary { currency: string; operatingExpenses: number; purchases: number; total: number; expenseCount: number; purchaseCount: number; }
export interface ExpenseCategorySpending { categoryId: string; categoryName: string; currency: string; total: number; expenseCount: number; percentage: number; }
export interface SupplierSpending { supplierId: string | null; supplierName: string; currency: string; total: number; purchaseCount: number; percentage: number; }
export interface MonthlySpending { month: string; currency: string; operatingExpenses: number; purchases: number; total: number; }
export interface SpendingReport { startDate: string; endDate: string; includesOperatingExpenses: boolean; includesPurchases: boolean; purchaseAmountBasis: string; summary: SpendingSummary[]; expenseCategories: ExpenseCategorySpending[]; purchaseSuppliers: SupplierSpending[]; expenseSuppliers: SupplierSpending[]; monthlyTrend: MonthlySpending[]; }
export const getSpendingReport = async (params: { startDate?: string; endDate?: string }) => (await apiClient.get<SpendingReport>("/reports/spending", { params })).data;
export interface InventoryReport { stock:{supplyItemId:string;name:string;category:string;unitOfMeasure:string;scanCode:string;currentQuantity:number;minimumQuantity:number;isLowStock:boolean}[]; movements:{movementType:string;quantity:number;count:number;isInbound:boolean}[]; lots:{id:string;supplyItemName:string;scanCode:string;manufacturerLotNumber:string|null;initialQuantity:number;remainingQuantity:number;unitOfMeasure:string;receivedAt:string;isActive:boolean;isEmpty:boolean;isLow:boolean}[]; productions:{id:string;urnName:string;bomVersion:number;quantityProduced:number;producedAt:string;materials:{supplyItemName:string;expectedQuantity:number;actualQuantity:number;wasteQuantity:number|null;unitOfMeasure:string;variance:number}[]}[]; finishedUrns:{urnName:string;supplyItemName:string;currentQuantity:number;minimumQuantity:number;isLowStock:boolean;purchasedReceipts:number;manufacturedReceipts:number}[]; }
export const getInventoryReport = async (params:{startDate?:string;endDate?:string;search?:string}) => (await apiClient.get<InventoryReport>("/reports/inventory",{params})).data;
export interface CostAnalytics { startDate:string; endDate:string; supplies:Array<{supplyItemId:string;name:string;unitOfMeasure:string;currentQuantity:number;currentUnitCost:number|null;normalizedCost:number|null;currency:string|null;estimatedReplacementValue:number|null;costAvailable:boolean}>; suppliers:Array<{supplyItemName:string;supplierName:string;unitCost:number;normalizedCost:number;purchaseUnit:string;currency:string;isPreferred:boolean}>; purchaseHistory:Array<{period:string;supplyItemName:string;quantity:number;amount:number;currency:string}>; boms:Array<{urnName:string;version:number;estimatedMaterialCost:number|null;currency:string|null;costComplete:boolean;items:Array<{supplyItemName:string;requiredQuantity:number;unitOfMeasure:string;unitCost:number|null;estimatedCost:number|null;currency:string|null;costAvailable:boolean}>}>; productions:Array<{urnName:string;producedAt:string;quantityProduced:number;actualMaterialCost:number|null;costPerUrn:number|null;expectedMaterialCost:number|null;quantityVariance:number|null;costVariance:number|null;costComplete:boolean}>; urns:Array<{urnName:string;costBasis:string;costPerUnit:number|null;currency:string|null;quantityInStock:number|null;estimatedReplacementValue:number|null;costAvailable:boolean}>; }
export const getCostAnalytics = async (params:{startDate?:string;endDate?:string}) => (await apiClient.get<CostAnalytics>("/reports/cost-analytics",{params})).data;
export interface PurchaseSource {
  id: string;
  supplierId: string;
  supplierName: string;
  supplyItemId: string;
  supplyItemName: string;
  supplierSku: string | null;
  currentUnitCost: number;
  purchaseUnit: string;
  inventoryUnitsPerPurchaseUnit: number;
  currency: string;
  isPreferred: boolean;
  isActive: boolean;
}
export interface PurchaseItem {
  id: string;
  supplyItemId: string;
  supplyItemName: string;
  descriptionSnapshot: string;
  supplierSkuSnapshot: string | null;
  purchaseUnitSnapshot: string;
  quantity: number;
  receivedQuantity: number;
  remainingQuantity: number;
  unitCost: number;
  lineSubtotal: number;
  normalizedReceivedQuantity: number;
  currency: string;
}
export interface Purchase {
  id: string;
  supplierId: string;
  supplierName: string;
  status: "Draft" | "Ordered" | "PartiallyReceived" | "Received" | "Cancelled";
  purchaseDate: string;
  invoiceReference: string | null;
  subtotal: number;
  tax: number;
  total: number;
  currency: string;
  notes: string | null;
  items: PurchaseItem[];
  receipts: { id: string; receivedAt: string; receivedByUserName: string | null; reference: string | null; notes: string | null; items: { id: string; purchaseItemId: string; supplyItemName: string; quantityReceived: number; normalizedReceivedQuantity: number }[] }[];
}
export const getPurchaseSources = async (supplierId: string) =>
  (
    await apiClient.get<PurchaseSource[]>("/purchasing/sources", {
      params: { supplierId, isActive: true },
    })
  ).data;
export const getPurchases = async () =>
  (
    await apiClient.get<Page<Purchase>>("/purchasing/purchases", {
      params: { page: 1, pageSize: 100 },
    })
  ).data;
export const createPurchase = async (payload: Record<string, unknown>) =>
  (await apiClient.post<Purchase>("/purchasing/purchases", payload)).data;
export const updatePurchase = async (id: string, payload: Record<string, unknown>) => (await apiClient.put<Purchase>(`/purchasing/purchases/${id}`, payload)).data;
export const changePurchaseStatus = async (id: string, status: number) => (await apiClient.post<Purchase>(`/purchasing/purchases/${id}/status`, status)).data;
export const receivePurchase = async (id: string, payload: Record<string, unknown>) => (await apiClient.post<Purchase>(`/purchasing/purchases/${id}/receipts`, payload)).data;
export interface InventoryItem { supplyItemId: string; name: string; unitOfMeasure: string; minimumQuantity: number; currentQuantity: number; isLowStock: boolean; trackInventory: boolean; }
export interface InventoryMovement { id: string; supplyItemId: string; supplyItemName: string; movementType: string; quantity: number; unitOfMeasure: string; occurredAt: string; reference: string | null; notes: string | null; recordedByUserId: string | null; }
export const getCurrentInventory = async (search = "") => (await apiClient.get<InventoryItem[]>("/inventory/current", { params: { search } })).data;
export const getInventoryMovements = async (id: string) => (await apiClient.get<InventoryMovement[]>(`/inventory/items/${id}/movements`)).data;
export const recordInventoryMovement = async (payload: Record<string, unknown>) => (await apiClient.post<InventoryMovement>("/inventory/movements", payload)).data;
export interface StockCount { id:string; supplyItemId:string; lotId:string|null; systemQuantity:number; countedQuantity:number; variance:number; countedAt:string; countedByUserId:string|null; inventoryMovementId:string|null; }
export const recordStockCount = async (payload: Record<string, unknown>) => (await apiClient.post<StockCount>("/inventory/stock-counts", payload)).data;
export const getStockCounts = async (id:string) => (await apiClient.get<StockCount[]>(`/inventory/stock-counts/${id}`)).data;
export interface FilamentSpecification { id: string; supplyItemId: string; materialType: string; brand: string | null; color: string | null; netUsableWeightGrams: number; manufacturerProductCode: string | null; productData: string | null; costPerKilogram: number | null; costPerGram: number | null; currency: string | null; }
export const getFilament = async (id: string) => (await apiClient.get<FilamentSpecification>(`/inventory/filament/${id}`)).data;
export const getFilamentCost = async (id: string) => (await apiClient.get<FilamentSpecification>(`/inventory/filament/${id}/cost`)).data;
export const saveFilament = async (payload: Record<string, unknown>) => (await apiClient.put<FilamentSpecification>("/inventory/filament", payload)).data;
