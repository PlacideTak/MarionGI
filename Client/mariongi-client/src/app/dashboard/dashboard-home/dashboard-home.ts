import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ChartModule } from 'primeng/chart';
import { ButtonModule } from 'primeng/button';

import { BiensService } from '../../services/biens.service';
import { RapportsService } from '../../services/rapports.service';
import { AlertItem, KpiCard, PropertyRow } from '../dashboard.models';
import { BienDto } from '../../models/gestimmo.models';

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [CommonModule, CardModule, TableModule, TagModule, ChartModule, ButtonModule],
  templateUrl: './dashboard-home.html',
  styleUrls: ['./dashboard-home.scss']
})
export class DashboardHome implements OnInit {
  readonly biensService = inject(BiensService);
  private readonly rapportsService = inject(RapportsService);

  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  // Données dynamiques
  readonly kpis = signal<KpiCard[]>([]);
  readonly alerts = signal<AlertItem[]>([]);
  readonly properties = signal<PropertyRow[]>([]);

  // Configuration Chart.js
  readonly chartLabels = signal<string[]>([]);
  readonly chartValues = signal<number[]>([]);

  readonly chartData = computed(() => ({
    labels: this.chartLabels(),
    datasets: [
      {
        label: 'Encaissements (FCFA)',
        data: this.chartValues(),
        fill: true,
        borderColor: '#2e7d5b',
        backgroundColor: 'rgba(46, 125, 91, 0.12)',
        tension: 0.4,
        pointBackgroundColor: '#2e7d5b',
        pointBorderColor: '#ffffff',
        pointRadius: 4,
        borderWidth: 2,
      },
    ],
  }));

  readonly chartOptions = {
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (context: any) => {
            const val = context.parsed.y ?? 0;
            return `Encaissements : ${val.toLocaleString('fr-FR')} FCFA`;
          }
        }
      }
    },
    scales: {
      x: {
        grid: { display: false },
        ticks: { color: '#6b7280' },
      },
      y: {
        beginAtZero: true,
        grid: { color: '#eef1ef' },
        ticks: {
          color: '#6b7280',
          callback: (value: number) => {
            if (value >= 1_000_000) {
              return `${(value / 1_000_000).toFixed(1)}M`;
            } else if (value >= 1_000) {
              return `${(value / 1_000).toFixed(0)}k`;
            }
            return value;
          },
        },
      },
    },
    maintainAspectRatio: false,
  };

  ngOnInit(): void {
    this.chargerDonneesDashboard();
  }

chargerDonneesDashboard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // 1. Récupération des KPIs et du graphe
    this.rapportsService.getDashboardKpi().subscribe({
      next: (kpiDto: any) => {
        const biensDisponibles = kpiDto.biensDisponibles ?? kpiDto.BiensDisponibles ?? 0;
        const encaissementsMois = kpiDto.encaissementsMois ?? kpiDto.EncaissementsMois ?? 0;
        const tauxOccupation = kpiDto.tauxOccupation ?? kpiDto.TauxOccupation ?? 0;

        // Mapping et normalisation sécurisée des alertes en premier
        const alertesList = kpiDto.alertes ?? kpiDto.Alertes;
        let mappedAlerts: AlertItem[] = [];
        
        if (Array.isArray(alertesList)) {
          mappedAlerts = alertesList.map((a: any) => ({
            id: a.id ?? a.Id,
            titre: a.titre ?? a.Titre ?? a.title ?? a.Title,
            message: a.message ?? a.Message ?? '',
            severite: (a.severite ?? a.Severite ?? a.type ?? a.Type ?? 'info').toLowerCase(),
          }));
          this.alerts.set(mappedAlerts);
        } else {
          this.alerts.set([]);
        }

        // Calcul dynamique du nombre d'impayés basé sur les alertes de retard de paiement
        const impayesEnCours = mappedAlerts.filter(a => 
          (a.titre || '').toLowerCase().includes('retard') || 
          (a.message || '').toLowerCase().includes('retard')
        ).length;

        this.kpis.set([
          { label: 'Biens disponibles', value: biensDisponibles },
          { label: 'Loyers encaissés (mois)', value: encaissementsMois, isCurrency: true },
          { label: 'Impayés en cours', value: impayesEnCours },
          { label: "Taux d'occupation", value: tauxOccupation, unit: '%' },
        ]);

        const graph = kpiDto.encaissementsGraph ?? kpiDto.EncaissementsGraph;
        if (graph) {
          const labels = graph.labels ?? graph.Labels ?? [];
          const datasets = graph.datasets ?? graph.Datasets ?? [];
          this.chartLabels.set(labels);
          this.chartValues.set(datasets);
        }
      },
      error: (err) => {
        console.error('Erreur API KPIs:', err);
        this.errorMessage.set('Erreur de chargement des métriques.');
      }
    });

    // 2. Récupération des derniers biens
    this.biensService.getBiens().subscribe({
      next: (data: BienDto[]) => {
        const mappedRows: PropertyRow[] = (data || []).slice(0, 5).map((b: BienDto) => ({
          reference: b.reference || 'N/A',
          type: this.biensService.getTypeLibelle(b.type),
          location: `${b.ville ?? ''} - ${b.quartier ?? ''}`.trim() || b.adresse || 'N/A',
          status: this.biensService.getStatutLibelle(b.statut),
          rent: b.loyer ?? 0,
        }));
        this.properties.set(mappedRows);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Erreur API Biens:', err);
        this.isLoading.set(false);
      }
    });
  }

  getAlertIcon(type?: string): string {
    switch ((type || '').toLowerCase()) {
      case 'danger':
      case 'error':
        return 'pi pi-exclamation-circle text-red-500';
      case 'warn':
      case 'warning':
        return 'pi pi-exclamation-triangle text-orange-500';
      case 'success':
        return 'pi pi-check-circle text-green-500';
      default:
        return 'pi pi-info-circle text-blue-500';
    }
  }
}