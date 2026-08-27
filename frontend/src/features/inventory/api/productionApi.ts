import { apiClient } from "../../../services/apiClient";
export interface ProductionUsage { id:string; supplyItemId:string; supplyItemName:string; expectedQuantity:number; actualQuantity:number; wasteQuantity:number|null; unitOfMeasure:string; costPerUnitSnapshot:number|null; totalMaterialCostSnapshot:number|null; }
export interface Production { id:string; urnId:string; urnName:string; urnBillOfMaterialsId:string; bomVersion:number; quantityProduced:number; producedAt:string; notes:string|null; usages:ProductionUsage[]; totalMaterialCost:number|null; costComplete:boolean; }
export const getProductions=async()=> (await apiClient.get<Production[]>("/urn-productions")).data;
export const getProductionCosts=async()=> (await apiClient.get<Production[]>("/urn-productions/costs")).data;
export const createProduction=async(payload:unknown)=> (await apiClient.post<Production>("/urn-productions",payload)).data;
