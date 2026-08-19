export interface KpiCard {
  label: string;
  value: number | string;
  isCurrency?: boolean;
  unit?: string;
}

export interface AlertItem {
  id?: string;
  titre: string;
  message: string;
  severite: 'danger' | 'warn' | 'info' | 'success';
  icone?: string;
  lienRoute?: string;
}

export interface PropertyRow {
  id?: string;
  reference: string;
  type: string;
  location: string;
  status: string;
  rent: number;
}

export interface NavItem {
  label: string;
  icon: string;
  route: string;
}

export interface EncaissementsGraphDto {
  labels: string[];
  datasets: number[];
}

export interface DashboardKpiDto {
  totalBiens: number;
  biensDisponibles: number;
  tauxOccupation: number;
  encaissementsMois: number;
  impayesEnCours?: number;
  encaissementsGraph?: EncaissementsGraphDto;
  alertes?: any[];
}