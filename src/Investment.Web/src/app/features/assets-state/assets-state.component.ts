import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AssetService } from '../../core/services/asset.service';
import { EditAssetPriceDialogComponent } from '../assets/edit-asset-price-dialog/edit-asset-price-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { RefreshService } from '../../core/services/refresh.service';
import { AssetSummary } from '../../core/models/models';
import { BalanceVisibilityService } from '../../core/services/balance-visibility.service';
import { PriceProvidersService } from '../../core/services/price-providers.service';
import { EodhdApiKeyDialogComponent } from '../../shared/eodhd-api-key-dialog/eodhd-api-key-dialog.component';
import { Observable } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { forkJoin, of } from 'rxjs';
import { ShortenNamePipe } from '../../shared/pipes/shorten-name.pipe';

@Component({
  selector: 'app-assets-state',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatProgressSpinnerModule, MatSortModule, MatPaginatorModule, MatIconModule, MatButtonModule, MatTooltipModule, MatDialogModule, ShortenNamePipe],
  templateUrl: './assets-state.component.html',
  styleUrls: ['./assets-state.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetsStateComponent implements OnInit, AfterViewInit {
  summaries: AssetSummary[] = [];
  displayedColumns = ['assetCode', 'totalUnitsHeld', 'averageBuyPrice', 'currentPrice', 'totalPaidIncludingFees', 'unrealizedPnL', 'realizedPnL', 'currentValue', 'actions'];
  loading = true;
  error: string | null = null;
  syncingAssetId: number | null = null;
  pageSize = 6;
  hideBalances$!: Observable<boolean>;

  dataSource = new MatTableDataSource<AssetSummary>([]);

  @ViewChild(MatSort) sort?: MatSort;
  @ViewChild(MatPaginator) paginator?: MatPaginator;

  constructor(
    private assetService: AssetService,
    private cdr: ChangeDetectorRef,
    private refresh: RefreshService,
    private dialog: MatDialog,
    private balanceVisibilityService: BalanceVisibilityService,
    private priceProviders: PriceProvidersService
  ) {}

  ngOnInit(): void {
    this.hideBalances$ = this.balanceVisibilityService.hidden$;
    this.loadSummaries();
    // subscribe to cross-component refresh events
    this.refresh.onRefresh('transactions:changed').subscribe(() => this.loadSummaries(false));
    this.refresh.onRefresh('prices:changed').subscribe(() => this.loadSummaries(false));
  }

  ngAfterViewInit(): void {
    // assign after view init
    if (this.sort) {
      this.dataSource.sort = this.sort;
    }
    if (this.paginator) {
      this.dataSource.paginator = this.paginator;
      this.paginator.pageSize = this.pageSize;
      this.paginator.pageIndex = 0;
    }
    (this.dataSource as any)._updateChangeSubscription?.();
  }

  get totalItems(): number {
    return this.dataSource.filteredData.length || this.summaries.length;
  }

  get currentPage(): number {
    const total = this.totalPages;
    const index = this.paginator?.pageIndex ?? 0;
    return Math.min(index + 1, total);
  }

  get totalPages(): number {
    if (this.totalItems === 0) {
      return 1;
    }
    return Math.max(1, Math.ceil(this.totalItems / this.pageSize));
  }

  getAssetCodeClass(assetType: string, isDailyAccrualFund?: boolean): string {
    if (isDailyAccrualFund) {
      return 'type-cloud';
    }
    const map: Record<string, string> = {
      'Stock': 'type-stock',
      'ETF': 'type-etf',
      'Crypto': 'type-crypto',
      'Gold': 'type-metal-gold',
      'Silver': 'type-metal-silver',
      'Fund': 'type-fund'
    };
    return map[assetType] || 'type-other';
  }

  getAssetTypeName(assetType: string, isDailyAccrualFund?: boolean): string {
    if (isDailyAccrualFund) {
      return 'صندوق تراكمي';
    }
    const map: Record<string, string> = {
      'Stock': 'سهم',
      'ETF': 'وثيقة',
      'Crypto': 'كريبتو',
      'Gold': 'ذهب',
      'Silver': 'فضة',
      'Fund': 'صندوق'
    };
    return map[assetType] || 'أخرى';
  }

  get isPreviousDisabled(): boolean {
    return !this.paginator || !this.paginator.hasPreviousPage();
  }

  get isNextDisabled(): boolean {
    return !this.paginator || !this.paginator.hasNextPage();
  }

  goToPreviousPage(): void {
    if (this.paginator?.hasPreviousPage()) {
      this.paginator.previousPage();
      this.cdr.markForCheck();
    }
  }

  goToNextPage(): void {
    if (this.paginator?.hasNextPage()) {
      this.paginator.nextPage();
      this.cdr.markForCheck();
    }
  }

  loadSummaries(showLoading = true): void {
    if (showLoading) {
      this.loading = true;
    }
    this.error = null;
    this.assetService.getAllSummaries().pipe(
      timeout(10000),
      catchError(() => {
        this.error = 'تعذر تحميل حالة الأصول.';
        return of([] as AssetSummary[]);
      })
    ).subscribe((data) => {
      this.summaries = data.map((summary) => ({
        ...summary,
        goldCashbackPerGram: summary.goldCashbackPerGram ?? 28.5,
        isClosedPosition: summary.isClosedPosition ?? false,
        totalFeesPaid: summary.totalFeesPaid ?? 0,
        totalPaidIncludingFees: summary.totalPaidIncludingFees ?? (summary.totalCostBasis + (summary.totalFeesPaid ?? 0))
      }));
      // Sort by market value (highest first) like the mobile app
      this.summaries.sort((a, b) => (b.currentValue ?? 0) - (a.currentValue ?? 0));
      
      this.syncPaginator(this.summaries.length);
      this.dataSource.data = this.summaries;
      this.refreshTable();
      this.loading = false;
      this.cdr.markForCheck();
    });
  }

  private syncPaginator(itemCount = this.dataSource.data.length): void {
    if (!this.paginator) {
      return;
    }

    this.paginator.pageSize = this.pageSize;
    const lastPage = Math.max(0, Math.ceil(itemCount / this.pageSize) - 1);
    if (this.paginator.pageIndex > lastPage) {
      this.paginator.pageIndex = lastPage;
    }
    this.dataSource.paginator = this.paginator;
  }

  private refreshTable(): void {
    (this.dataSource as any)._updateChangeSubscription?.();
  }

  editCurrentPrice(asset: AssetSummary): void {
    const dialogRef = this.dialog.open(EditAssetPriceDialogComponent, {
      width: '400px',
      maxWidth: '95vw',
      data: { asset }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;

      const requests = [];
      if (result.price != null) {
        requests.push(this.assetService.setCurrentPrice(asset.assetId, { price: result.price }));
      }
      if (result.goldCashbackPerGram != null || result.dailyAccrualAnnualRatePercent != null) {
        requests.push(this.assetService.updateFinancialSettings(asset.assetId, {
          goldCashbackPerGram: result.goldCashbackPerGram,
          dailyAccrualAnnualRatePercent: result.dailyAccrualAnnualRatePercent
        }));
      }

      (requests.length ? forkJoin(requests) : of([])).subscribe({
        next: () => {
          this.loadSummaries(false);
          this.refresh.notify('transactions:changed');
        },
        error: () => {
          this.error = 'تعذر تحديث السعر الحالي.';
          this.cdr.markForCheck();
        }
      });
    });
  }

  syncAssetPrice(asset: AssetSummary): void {
    if (asset.assetType !== 'Stock' || this.syncingAssetId != null) {
      return;
    }

    this.syncingAssetId = asset.assetId;
    this.error = null;
    this.cdr.markForCheck();

    this.priceProviders.getEodhdConfiguration().pipe(
      timeout(5000),
      catchError(() => of({ hasApiKey: true }))
    ).subscribe((configuration) => {
      if (!configuration.hasApiKey) {
        this.openApiKeyDialogBeforeSync(asset);
        return;
      }

      this.runAssetPriceSync(asset);
    });
  }

  private openApiKeyDialogBeforeSync(asset: AssetSummary): void {
    const dialogRef = this.dialog.open(EodhdApiKeyDialogComponent, {
      width: '460px',
      maxWidth: '92vw',
      panelClass: 'eodhd-api-key-dialog-panel',
      backdropClass: 'confirm-delete-backdrop',
      autoFocus: '#eodhd-api-key',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe((saved) => {
      if (!saved) {
        this.syncingAssetId = null;
        this.cdr.markForCheck();
        return;
      }

      this.refresh.notify('prices:changed');
      this.runAssetPriceSync(asset);
    });
  }

  private runAssetPriceSync(asset: AssetSummary): void {
    this.assetService.syncCurrentPrice(asset.assetId).subscribe({
      next: () => {
        this.syncingAssetId = null;
        this.loadSummaries(false);
        this.refresh.notify('prices:changed');
      },
      error: () => {
        this.syncingAssetId = null;
        this.error = 'تعذر مزامنة سعر الأصل.';
        this.cdr.markForCheck();
      }
    });
  }

  deleteAsset(asset: AssetSummary): void {
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '420px',
      maxWidth: 'calc(100vw - 32px)',
      panelClass: 'confirm-delete-dialog-panel',
      backdropClass: 'confirm-delete-backdrop',
      data: {
        title: 'حذف الأصل',
        message: `هل تريد حذف ${asset.assetCode} وكل معاملاته؟`
      }
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.assetService.delete(asset.assetId).subscribe({
        next: () => {
          this.loadSummaries(false);
          this.refresh.notify('transactions:changed');
        },
        error: () => {
          this.error = 'تعذر حذف الأصل.';
          this.cdr.markForCheck();
        }
      });
    });
  }

  getPnLClass(value: number): string {
    if (value > 0) return 'profit';
    if (value < 0) return 'loss';
    return '';
  }

  getPnLPercentClass(value: number): string {
    return this.getPnLClass(value);
  }
}
