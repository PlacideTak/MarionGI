import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import { CommonModule } from '@angular/common';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ChartModule } from 'primeng/chart';
import { ButtonModule } from 'primeng/button';

import { RapportsService } from '../../services/rapports.service';
import { AuthService } from '../../login/auth.service';

import {
  AlertItem,
  KpiCard
} from '../dashboard.models';

import {
  ROLES,
  TypeUniteLocative,
  StatutBien
} from '../../models/gestimmo.models';


// ============================================================
// TYPES
// ============================================================

/**
 * Valeurs acceptées par PrimeNG pour p-tag.
 *
 * PrimeNG utilise "warn" et non "warning".
 */
type AlertSeverity =
  | 'danger'
  | 'warn'
  | 'info'
  | 'success'
  | 'secondary'
  | 'contrast';


/**
 * Ligne affichée dans le tableau
 * des dernières unités locatives.
 */
interface UniteDashboardRow {
  reference: string;
  bienReference: string;
  bienNom: string;
  type: string;
  loyer: number;
  statut: string;
}


/**
 * Structure interne d'une alerte provenant de l'API.
 */
interface DashboardAlertDto {
  id?: string;
  titre?: string;
  message?: string;
  severite?: string;
  type?: string;
}


// ============================================================
// COMPOSANT
// ============================================================

@Component({
  selector: 'app-dashboard-home',
  standalone: true,

  imports: [
    CommonModule,
    CardModule,
    TableModule,
    TagModule,
    ChartModule,
    ButtonModule
  ],

  templateUrl: './dashboard-home.html',
  styleUrls: ['./dashboard-home.scss']
})
export class DashboardHome implements OnInit {

  // ============================================================
  // SERVICES
  // ============================================================

  readonly rapportsService = inject(RapportsService);

  private readonly authService = inject(AuthService);


  // ============================================================
  // ÉTAT
  // ============================================================

  readonly isLoading = signal<boolean>(true);

  readonly errorMessage =
    signal<string | null>(null);

  readonly kpis =
    signal<KpiCard[]>([]);

  readonly alerts =
    signal<AlertItem[]>([]);

  readonly properties =
    signal<UniteDashboardRow[]>([]);


  // ============================================================
  // DROITS
  // ============================================================

  readonly canViewStats = computed(() => {

    return this.authService.hasRole([
      ROLES.Administrateur,
      ROLES.Gestionnaire
    ]);

  });


  // ============================================================
  // DONNÉES DU GRAPHIQUE
  // ============================================================

  readonly chartLabels =
    signal<string[]>([]);

  readonly chartValues =
    signal<number[]>([]);


  readonly chartData = computed(() => ({

    labels: this.chartLabels(),

    datasets: [

      {
        label: 'Encaissements (FCFA)',

        data: this.chartValues(),

        fill: true,

        borderColor: '#2e7d5b',

        backgroundColor:
          'rgba(46, 125, 91, 0.12)',

        tension: 0.4,

        pointBackgroundColor:
          '#2e7d5b',

        pointBorderColor:
          '#ffffff',

        pointRadius: 4,

        borderWidth: 2
      }

    ]

  }));


  readonly chartOptions = {

    plugins: {

      legend: {
        display: false
      },

      tooltip: {

        callbacks: {

          label: (context: any) => {

            const val =
              context.parsed.y ?? 0;

            return (
              `Encaissements : ` +
              `${val.toLocaleString('fr-FR')} FCFA`
            );

          }

        }

      }

    },

    scales: {

      x: {

        grid: {
          display: false
        },

        ticks: {
          color: '#6b7280'
        }

      },

      y: {

        beginAtZero: true,

        grid: {
          color: '#eef1ef'
        },

        ticks: {

          color: '#6b7280',

          callback: (value: number) => {

            if (value >= 1_000_000) {

              return (
                `${(value / 1_000_000).toFixed(1)}M`
              );

            }

            if (value >= 1_000) {

              return (
                `${(value / 1_000).toFixed(0)}k`
              );

            }

            return value;

          }

        }

      }

    },

    maintainAspectRatio: false

  };


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    if (this.canViewStats()) {

      this.chargerDonneesDashboard();

    } else {

      this.isLoading.set(false);

    }

  }


  // ============================================================
  // CHARGEMENT DU DASHBOARD
  // ============================================================

  chargerDonneesDashboard(): void {

    this.isLoading.set(true);

    this.errorMessage.set(null);


    // ==========================================================
    // KPIs + GRAPHIQUE + DERNIÈRES UNITÉS
    // ==========================================================

    this.rapportsService.getDashboardKpi().subscribe({

      next: (kpiDto: any) => {

        // ------------------------------------------------------
        // UNITÉS DISPONIBLES
        // ------------------------------------------------------

        const unitesDisponibles =
          kpiDto.unitesDisponibles ??
          kpiDto.UnitesDisponibles ??
          0;


        // ------------------------------------------------------
        // ENCAISSEMENTS DU MOIS
        // ------------------------------------------------------

        const encaissementsMois =
          kpiDto.encaissementsMois ??
          kpiDto.EncaissementsMois ??
          0;


        // ------------------------------------------------------
        // TAUX D'OCCUPATION
        // ------------------------------------------------------

        const tauxOccupation =
          kpiDto.tauxOccupation ??
          kpiDto.TauxOccupation ??
          0;


        // ------------------------------------------------------
        // ALERTES
        // ------------------------------------------------------

        const alertesList =
          kpiDto.alertes ??
          kpiDto.Alertes;

        let mappedAlerts: AlertItem[] = [];


        if (Array.isArray(alertesList)) {

          mappedAlerts =
            alertesList.map(
              (a: DashboardAlertDto): AlertItem => {

                const severiteBrute = (
                  a.severite ??
                  a.type ??
                  'info'
                ).toLowerCase();


                // ------------------------------------------------
                // NORMALISATION PRIME NG
                // ------------------------------------------------
                //
                // Le backend peut envoyer "warning".
                // PrimeNG attend "warn".
                //
                const severite: AlertSeverity =
                  severiteBrute === 'warning'
                    ? 'warn'
                    : this.normalizeAlertSeverity(
                        severiteBrute
                      );


                return {

                  id:
                    a.id ?? '',

                  titre:
                    a.titre ?? '',

                  message:
                    a.message ?? '',

                  severite

                };

              }
            );

        }


        this.alerts.set(mappedAlerts);


        // ------------------------------------------------------
        // IMPAYÉS
        // ------------------------------------------------------

        const impayesEnCours =
          mappedAlerts.filter(a =>

            (a.titre || '')
              .toLowerCase()
              .includes('retard')

            ||

            (a.message || '')
              .toLowerCase()
              .includes('retard')

          ).length;


        // ------------------------------------------------------
        // KPIs
        // ------------------------------------------------------

        this.kpis.set([

          {
            label: 'Unités disponibles',
            value: unitesDisponibles
          },

          {
            label: 'Loyers encaissés (mois)',
            value: encaissementsMois,
            isCurrency: true
          },

          {
            label: 'Impayés en cours',
            value: impayesEnCours
          },

          {
            label: "Taux d'occupation",
            value: tauxOccupation,
            unit: '%'
          }

        ]);


        // ======================================================
        // GRAPHIQUE
        // ======================================================

        const graph =
          kpiDto.encaissementsGraph ??
          kpiDto.EncaissementsGraph;


        if (graph) {

          const labels =
            graph.labels ??
            graph.Labels ??
            [];

          const datasets =
            graph.datasets ??
            graph.Datasets ??
            [];

          this.chartLabels.set(labels);

          this.chartValues.set(datasets);

        } else {

          this.chartLabels.set([]);

          this.chartValues.set([]);

        }


        // ======================================================
        // DERNIÈRES UNITÉS LOCATIVES
        // ======================================================

        const dernieresUnites =
          kpiDto.dernieresUnites ??
          kpiDto.DernieresUnites ??
          [];


        const mappedRows: UniteDashboardRow[] =
          Array.isArray(dernieresUnites)

            ? dernieresUnites.map(
                (unite: any): UniteDashboardRow => ({

                  reference:
                    unite.reference ??
                    unite.Reference ??
                    'N/A',

                    bienReference:
                      unite.bien?.reference ??
                      unite.bien?.Reference ??
                      'N/A',

                    bienNom:
                      unite.bien?.nom ??
                      unite.bien?.Nom ??
                      'N/A',

                  type:
                    this.getTypeUniteLibelle(
                      unite.type ??
                      unite.Type
                    ),

                  loyer:
                    unite.loyer ??
                    unite.Loyer ??
                    0,

                  statut:
                    this.getStatutUniteLibelle(
                      unite.statut ??
                      unite.Statut
                    )

                })
              )

            : [];


        this.properties.set(mappedRows);

        this.isLoading.set(false);

      },


      error: (err) => {

        console.error(
          'Erreur API Dashboard :',
          err
        );

        this.errorMessage.set(
          'Erreur de chargement du dashboard.'
        );

        this.properties.set([]);

        this.alerts.set([]);

        this.kpis.set([]);

        this.chartLabels.set([]);

        this.chartValues.set([]);

        this.isLoading.set(false);

      }

    });

  }


  // ============================================================
  // NORMALISATION DES SÉVÉRITÉS
  // ============================================================

  private normalizeAlertSeverity(
    severity: string
  ): AlertSeverity {

    switch (severity) {

      case 'danger':
      case 'error':
        return 'danger';

      case 'warn':
      case 'warning':
        return 'warn';

      case 'success':
        return 'success';

      case 'secondary':
        return 'secondary';

      case 'contrast':
        return 'contrast';

      case 'info':
      default:
        return 'info';

    }

  }


  // ============================================================
  // TYPE D'UNITÉ
  // ============================================================

  getTypeUniteLibelle(
    type: TypeUniteLocative | number
  ): string {

    switch (Number(type)) {

      case TypeUniteLocative.Appartement:
        return 'Appartement';

      case TypeUniteLocative.Maison:
        return 'Maison';

      case TypeUniteLocative.Villa:
        return 'Villa';

      case TypeUniteLocative.Boutique:
        return 'Boutique';

      case TypeUniteLocative.Terrain:
        return 'Terrain';

      case TypeUniteLocative.Chambre:
        return 'Chambre';

      case TypeUniteLocative.Studio:
        return 'Studio';

      case TypeUniteLocative.Magasin:
        return 'Magasin';

      case TypeUniteLocative.Bureau:
        return 'Bureau';

      case TypeUniteLocative.Duplex:
        return 'Duplex';

      case TypeUniteLocative.Bungalow:
        return 'Bungalow';

      case TypeUniteLocative.Penthouse:
        return 'Penthouse';

      case TypeUniteLocative.Entrepot:
        return 'Entrepôt';

      case TypeUniteLocative.Parking:
        return 'Parking';

      case TypeUniteLocative.Cave:
        return 'Cave';

      default:
        return 'Inconnu';

    }

  }


  // ============================================================
  // STATUT DE L'UNITÉ
  // ============================================================

  getStatutUniteLibelle(
    statut: StatutBien | number
  ): string {

    switch (Number(statut)) {

      case StatutBien.Disponible:
        return 'Disponible';

      case StatutBien.Loue:
        return 'Loué';

      case StatutBien.Reserve:
        return 'Réservé';

      case StatutBien.EnTravaux:
        return 'En travaux';

      default:
        return 'Inconnu';

    }

  }


  // ============================================================
  // ICÔNE DES ALERTES
  // ============================================================

  getAlertIcon(type?: string): string {

    switch ((type || '').toLowerCase()) {

      case 'danger':
      case 'error':
        return (
          'pi pi-exclamation-circle text-red-500'
        );

      case 'warn':
      case 'warning':
        return (
          'pi pi-exclamation-triangle text-orange-500'
        );

      case 'success':
        return (
          'pi pi-check-circle text-green-500'
        );

      default:
        return (
          'pi pi-info-circle text-blue-500'
        );

    }

  }

}