import { apiClient } from "../../../services/apiClient";
export interface BomItem { id:string; supplyItemId:string; supplyItemName:string; requiredQuantity:number; unitOfMeasure:string; estimatedCost:number|null; currency:string|null; costAvailable:boolean; }
export interface Bom { id:string; urnId:string; urnName:string; version:number; isActive:boolean; items:BomItem[]; estimatedMaterialCost:number|null; costComplete:boolean; currency:string|null; }
export const getBoms = async (urnId:string) => (await apiClient.get<Bom[]>(`/urn-boms/${urnId}`)).data;
export const getBomCosts = async (urnId:string) => (await apiClient.get<Bom[]>(`/urn-boms/${urnId}/cost`)).data;
export const saveBom = async (payload:unknown) => (await apiClient.put<Bom>("/urn-boms", payload)).data;
