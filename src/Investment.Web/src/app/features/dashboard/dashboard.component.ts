import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AssetSelectionService } from '../../core/services/asset-selection.service';
import { RefreshService } from '../../core/services/refresh.service';
import { AnalyticsService } from '../../core/services/analytics.service';
import { Holding } from '../../core/models/models';
import { BalanceVisibilityService } from '../../core/services/balance-visibility.service';
import { BaseChartDirective } from 'ng2-charts';
import { forkJoin, of, Subscription, Observable } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';

interface DashboardTotals {
  totalInvestedCapital: number;
  totalCurrentValue: number;
  totalUnrealizedPnL: number;
  totalUnrealizedPnLPercent: number;
  totalRealizedPnL: number;
  totalFeesPaid: number;
}

interface AssetTypeFilter {
  type: string;
  label: string;
  count: number;
  assetCodes: string[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, BaseChartDirective],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy {
  holdings: Holding[] = [];
  private allHoldings: Holding[] = [];
  summaryTotals: DashboardTotals = this.calculateTotals([]);
  loading = true;
  error: string | null = null;
  private readonly subscriptions = new Subscription();
  hideBalances$!: Observable<boolean>;
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;

  public pieChartOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    layout: {
      padding: 16
    },
    onClick: (_event: unknown, activeElements: Array<{ index?: number }> = []) => {
      const index = activeElements[0]?.index;
      if (typeof index === 'number') {
        this.toggleSelectionByIndex(index);
      }
    },
    plugins: {
      legend: {
        display: false,
        position: 'bottom',
        align: 'center',
        onClick: (_event: unknown, legendItem: { index?: number }) => {
          if (typeof legendItem.index === 'number') {
            this.toggleSelectionByIndex(legendItem.index);
          }
        },
        labels: {
          color: '#94a3b8',
          boxWidth: 14,
          padding: 12,
          font: { size: 13 }
        }
      },
      tooltip: {
        callbacks: {
          label: (context: any) => {
            const assetCode = context?.label ?? '';
            const value = typeof context?.parsed === 'number' ? context.parsed : Number(context?.parsed ?? 0);
            return this.balanceVisibilityService.isHidden()
              ? `${assetCode}: ••••••`
              : `${assetCode}: ${value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
          }
        }
      }
    }
  };
  public pieChartData: any = { labels: [], datasets: [] };

  constructor(
    private analyticsService: AnalyticsService,
    private assetSelectionService: AssetSelectionService,
    private refreshService: RefreshService,
    private cdr: ChangeDetectorRef,
    private balanceVisibilityService: BalanceVisibilityService
  ) {}

  ngOnInit(): void {
    this.hideBalances$ = this.balanceVisibilityService.hidden$;

    this.subscriptions.add(
      this.assetSelectionService.selectedAssetCodes$.subscribe(() => {
        this.updateSummaryTotals();
        this.setupChart();
        this.cdr.markForCheck();
      })
    );

    this.subscriptions.add(
      this.refreshService.onRefresh('transactions:changed').subscribe(() => {
        this.loadDashboardData(false);
      })
    );

    this.subscriptions.add(
      this.refreshService.onRefresh('prices:changed').subscribe(() => {
        this.loadDashboardData(false);
      })
    );

    this.loadDashboardData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadDashboardData(showLoading = true): void {
    if (showLoading) {
      this.loading = true;
    }

    this.error = null;
    forkJoin({
      holdings: this.analyticsService.getHoldings().pipe(
        timeout(10000),
        catchError(() => of([] as Holding[]))
      )
    }).subscribe(({ holdings }) => {
      this.allHoldings = holdings;
      this.holdings = holdings;
      this.assetSelectionService.syncToAvailableAssets(this.allHoldings.map(h => h.assetCode));
      this.updateSummaryTotals();
      this.setupChart();
      if (this.allHoldings.length === 0) {
        this.error = 'تعذر تحميل بيانات لوحة التحكم.';
      }
      this.loading = false;
      this.cdr.markForCheck();
    });
  }

  onChartClick(event: any): void {
    const index = event?.active?.[0]?.index;
    if (typeof index === 'number') {
      this.toggleSelectionByIndex(index);
    }
  }

  onChartCanvasClick(event: MouseEvent): void {
    const chart = this.chart?.chart;
    if (!chart) {
      return;
    }

    const elements = chart.getElementsAtEventForMode(event as any, 'nearest', { intersect: true }, false);
    if (elements.length > 0) {
      this.toggleSelectionByIndex(elements[0].index);
    }
  }

  resetSelection(): void {
    this.assetSelectionService.clearSelection();
  }

  hasCustomSelection(): boolean {
    const selected = this.assetSelectionService.snapshot;
    return selected !== null;
  }

  isAssetSelected(assetCode: string): boolean {
    return this.assetSelectionService.isSelected(assetCode);
  }

  toggleAssetByCode(assetCode: string): void {
    this.assetSelectionService.toggleAssetCode(assetCode, this.allHoldings.map(h => h.assetCode));
  }

  get assetTypeFilters(): AssetTypeFilter[] {
    const grouped = new Map<string, Holding[]>();
    for (const holding of this.allHoldings) {
      const key = holding.isDailyAccrualFund ? 'DailyAccrualFund' : holding.assetType;
      grouped.set(key, [...(grouped.get(key) ?? []), holding]);
    }

    return Array.from(grouped.entries())
      .sort(([left], [right]) => this.assetTypeOrder(left) - this.assetTypeOrder(right))
      .map(([type, holdings]) => ({
        type,
        label: this.assetTypeFilterLabel(type),
        count: holdings.length,
        assetCodes: holdings.map(holding => holding.assetCode)
      }));
  }

  toggleAssetType(filter: AssetTypeFilter): void {
    const allCodes = this.allHoldings.map(holding => holding.assetCode);
    const selected = this.assetSelectionService.snapshot ?? allCodes;
    const filterCodes = new Set(filter.assetCodes);
    const isExcluded = filter.assetCodes.every(code => !selected.includes(code));

    const nextSelection = isExcluded
      ? Array.from(new Set([...selected, ...filter.assetCodes]))
      : selected.filter(code => !filterCodes.has(code));

    this.assetSelectionService.setSelection(nextSelection.length === allCodes.length ? null : nextSelection);
  }

  isAssetTypeFilterActive(filter: AssetTypeFilter): boolean {
    const selected = this.assetSelectionService.snapshot;
    if (selected === null) {
      return false;
    }

    return filter.assetCodes.every(code => !selected.includes(code));
  }

  getChartColor(index: number, assetCode: string): string {
    const colors = [
      '#2563eb', '#7c3aed', '#16a34a', '#d97706', '#dc2626', '#db2777', '#0891b2'
    ];

    const baseColor = colors[index % colors.length];
    const isSelected = this.assetSelectionService.isSelected(assetCode);
    return this.withOpacity(baseColor, isSelected ? 0.92 : 0.16);
  }

  private setupChart(): void {
    const labels = this.allHoldings.map(h => h.assetCode);
    const data = this.allHoldings.map(h => h.currentValue);

    this.pieChartData = {
      labels,
      datasets: [
        {
          data,
          backgroundColor: this.allHoldings.map((holding, index) => this.getChartColor(index, holding.assetCode)),
          borderWidth: 0
        }
      ]
    };
  }

  private getSelectedHoldings(): Holding[] {
    const selectedCodes = this.assetSelectionService.snapshot;
    if (selectedCodes === null) {
      return this.allHoldings;
    }

    return this.allHoldings.filter(holding => selectedCodes.includes(holding.assetCode));
  }

  private updateSummaryTotals(): void {
    this.summaryTotals = this.calculateTotals(this.getSelectedHoldings());
  }

  private calculateTotals(holdings: Holding[]): DashboardTotals {
    const totalInvestedCapital = holdings.reduce((sum, holding) => sum + holding.totalCostBasis, 0);
    const totalCurrentValue = holdings.reduce((sum, holding) => sum + holding.currentValue, 0);
    const totalUnrealizedPnL = holdings.reduce((sum, holding) => sum + holding.unrealizedPnL, 0);
    const totalRealizedPnL = holdings.reduce((sum, holding) => sum + holding.realizedPnL, 0);
    const totalFeesPaid = holdings.reduce((sum, holding) => sum + holding.totalFeesPaid, 0);

    return {
      totalInvestedCapital,
      totalCurrentValue,
      totalUnrealizedPnL,
      totalUnrealizedPnLPercent: totalInvestedCapital !== 0 ? (totalUnrealizedPnL / totalInvestedCapital) * 100 : 0,
      totalRealizedPnL,
      totalFeesPaid
    };
  }

  private toggleSelectionByIndex(index: number): void {
    const assetCode = this.allHoldings[index]?.assetCode;
    if (!assetCode) {
      return;
    }

    this.assetSelectionService.toggleAssetCode(assetCode, this.allHoldings.map(h => h.assetCode));
  }

  private assetTypeFilterLabel(type: string): string {
    switch (type) {
      case 'Stock':
        return 'الأسهم';
      case 'Gold':
        return 'الذهب';
      case 'Fund':
        return 'الصناديق';
      case 'DailyAccrualFund':
        return 'الكلاود';
      case 'Other':
        return 'الأخرى';
      default:
        return type;
    }
  }

  private assetTypeOrder(type: string): number {
    const order: Record<string, number> = {
      Stock: 1,
      Gold: 2,
      Fund: 3,
      DailyAccrualFund: 4,
      Other: 5
    };

    return order[type] ?? 99;
  }

  private withOpacity(hexColor: string, opacity: number): string {
    const sanitized = hexColor.replace('#', '');
    const expanded = sanitized.length === 3
      ? sanitized.split('').map(character => character + character).join('')
      : sanitized;

    const red = parseInt(expanded.slice(0, 2), 16);
    const green = parseInt(expanded.slice(2, 4), 16);
    const blue = parseInt(expanded.slice(4, 6), 16);
    return `rgba(${red}, ${green}, ${blue}, ${opacity})`;
  }

}
