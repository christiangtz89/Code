import { apiClient } from "../../../services/apiClient";
export interface UrnInventory { id:string; urnId:string; urnName:string; supplyItemId:string; supplyItemName:string; scanCode:string; unitOfMeasure:string; currentQuantity:number; minimumQuantity:number; isLowStock:boolean; }
export const getUrnInventory=async()=> (await apiClient.get<UrnInventory[]>("/urn-inventory")).data;
export const saveUrnInventory=async(payload:unknown)=> (await apiClient.put<UrnInventory>("/urn-inventory",payload)).data;
export interface UrnMovement { id:string; movementType:string; quantity:number; unitOfMeasure:string; occurredAt:string; reference:string|null; notes:string|null; }
export const getUrnMovements=async(id:string)=> (await apiClient.get<UrnMovement[]>(`/inventory/items/${id}/movements`)).data;
