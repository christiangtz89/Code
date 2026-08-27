import { apiClient } from "../../../services/apiClient";
export interface InventoryLot { id:string; supplyItemId:string; supplyItemName:string; scanCode:string; manufacturerLotNumber:string|null; initialQuantity:number; remainingQuantity:number; unitOfMeasure:string; receivedAt:string; isActive:boolean; }
export const getLots=async(search="")=>(await apiClient.get<InventoryLot[]>("/inventory-lots",{params:{search}})).data;
export const getLotByScan=async(code:string)=>(await apiClient.get<InventoryLot>(`/inventory-lots/scan/${encodeURIComponent(code)}`)).data;
export const createLot=async(payload:unknown)=>(await apiClient.post<InventoryLot>("/inventory-lots",payload)).data;
