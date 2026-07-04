import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TransactionService } from '../../core/services/transaction.service';
import { AssetService } from '../../core/services/asset.service';
import { RefreshService } from '../../core/services/refresh.service';
import { Asset, AssetSummary, Transaction, CreateTransactionDraft, ExternalAssetSearchResult, CreateManualAssetDraft } from '../../core/models/models';
import { TransactionDialogComponent, TransactionDialogData } from './transaction-dialog/transaction-dialog.component';
import { ManualAssetDialogComponent } from '../assets/manual-asset-dialog/manual-asset-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { BalanceVisibilityService } from '../../core/services/balance-visibility.service';
import { catchError, finalize, switchMap, timeout } from 'rxjs/operators';
import { forkJoin, Observable, of } from 'rxjs';
import { ShortenNamePipe } from '../../shared/pipes/shorten-name.pipe';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule, MatProgressSpinnerModule, MatDialogModule, MatPaginatorModule, ShortenNamePipe],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionsComponent implements OnInit, AfterViewInit, OnDestroy {
  transactions: Transaction[] = [];
  assets: Asset[] = [];
  assetSummaries: AssetSummary[] = [];
  assetIdsWithBuy: number[] = [];
  dataSource = new MatTableDataSource<Transaction>([]);
  pageSize = 6;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns: string[] = ['date', 'asset', 'type', 'quantity', 'price', 'total', 'actions'];
  loading = true;
  error: string | null = null;
  loadError: string | null = null;
  successMessage: string | null = null;
  hideBalances$!: Observable<boolean>;
  private messageTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private transactionService: TransactionService,
    private assetService: AssetService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private refreshService: RefreshService,
    private balanceVisibilityService: BalanceVisibilityService
  ) {}

  ngOnInit(): void {
    this.hideBalances$ = this.balanceVisibilityService.hidden$;
    this.loadTransactions();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  ngOnDestroy(): void {
    if (this.messageTimer) {
      clearTimeout(this.messageTimer);
    }
  }

  get totalItems(): number {
    return this.dataSource.filteredData.length || this.transactions.length;
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

  loadTransactions(showLoading = true) {
    if (showLoading) {
      this.loading = true;
    }
    this.loadError = null;
    forkJoin({
      transactions: this.transactionService.getAll(),
      assets: this.assetService.getAll(),
      summaries: this.assetService.getAllSummaries()
    }).pipe(
      timeout(10000),
      catchError(() => {
        this.loadError = 'تعذر تحميل المعاملات.';
        return of({ transactions: [] as Transaction[], assets: [] as Asset[], summaries: [] as AssetSummary[] });
      }),
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe(({ transactions, assets, summaries }) => {
      this.transactions = transactions;
      this.assets = assets;
      this.assetSummaries = summaries;
      this.assetIdsWithBuy = [...new Set(transactions
        .filter((transaction) => transaction.transactionType === 'Buy')
        .map((transaction) => transaction.assetId))];
      this.syncPaginator(transactions.length);
      this.dataSource.data = transactions;
      this.refreshTable();
      this.cdr.markForCheck();
    });
  }

  private syncPaginator(itemCount = this.dataSource.data.length): void {
    if (!this.paginator) {
      return;
    }

    const lastPage = Math.max(0, Math.ceil(itemCount / this.pageSize) - 1);
    if (this.paginator.pageIndex > lastPage) {
      this.paginator.pageIndex = lastPage;
    }
    this.dataSource.paginator = this.paginator;
  }

  private refreshTable(): void {
    (this.dataSource as any)._updateChangeSubscription?.();
  }

  openNewTransactionDialog(prefilledAsset?: ExternalAssetSearchResult): void {
    const dialogData: TransactionDialogData = {
      mode: 'create',
      prefilledAsset,
      knownAssets: this.assets,
      assetSummaries: this.assetSummaries,
      assetIdsWithBuy: this.assetIdsWithBuy
    };
    const dialogRef = this.dialog.open(TransactionDialogComponent, {
      width: '640px',
      maxWidth: '95vw',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe((result: CreateTransactionDraft | undefined) => {
      if (!result) return;
      this.saveDraft(result, 'create');
    });
  }

  openManualAssetDialog(): void {
    const dialogRef = this.dialog.open(ManualAssetDialogComponent, {
      width: '640px',
      maxWidth: '95vw',
      data: {
        knownAssets: this.assets,
        assetSummaries: this.assetSummaries,
        assetIdsWithBuy: this.assetIdsWithBuy
      }
    });

    dialogRef.afterClosed().subscribe((result: CreateManualAssetDraft | undefined) => {
      if (!result) return;

      this.canCreateInitialTransaction(result.assetCode, result.transactionType).subscribe((canProceed) => {
        if (!canProceed) {
          this.showError(this.firstSellBlockedMessage(result.isDailyAccrualFund));
          return;
        }

      this.assetService.createManual({
        assetCode: result.assetCode,
        assetName: result.assetName,
        assetType: result.assetType,
        currency: result.currency,
        notes: result.notes,
        initialPrice: result.pricePerUnit,
        isDailyAccrualFund: result.isDailyAccrualFund,
        dailyAccrualAnnualRatePercent: result.isDailyAccrualFund ? (result.dailyAccrualAnnualRatePercent ?? 0) : 0
      }).pipe(
        switchMap((asset) =>
          this.transactionService.create({
            assetId: asset.assetId,
            transactionType: result.transactionType,
            transactionDate: result.transactionDate,
            quantity: result.quantity,
            pricePerUnit: result.pricePerUnit,
            fees: result.fees,
            manufacturingFeePerGram: result.manufacturingFeePerGram,
            dividendKind: result.dividendKind,
            notes: result.notes
          })
        )
      ).subscribe({
        next: () => {
          this.showSuccess('تم إجراء العملية.');
          this.loadTransactions(false);
          this.refreshService.notify('transactions:changed');
          this.cdr.markForCheck();
        },
        error: (err) => {
          const body = err?.error;
          const serverMessage =
            (typeof body === 'string' ? body : null) ??
            body?.message ??
            body?.Message ??
            body?.detailed ??
            body?.Detailed;

          if (err?.status === 0) {
            this.showError('تعذر الاتصال بالخادم. تأكد أن واجهة الـ API تعمل على http://localhost:5091');
          } else if (serverMessage) {
            this.showError(`تعذر الحفظ: ${serverMessage}`);
          } else {
            this.showError(`تعذر إضافة الأصل اليدوي (رمز الخطأ: ${err?.status ?? 'غير معروف'}).`);
          }
        }
      });
      });
    });
  }

  openEditTransactionDialog(transaction: Transaction): void {
    const dialogData: TransactionDialogData = {
      mode: 'edit',
      transaction,
      knownAssets: this.assets,
      assetSummaries: this.assetSummaries,
      assetIdsWithBuy: this.assetIdsWithBuy
    };
    const dialogRef = this.dialog.open(TransactionDialogComponent, {
      width: '640px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe((result: CreateTransactionDraft | undefined) => {
      if (!result) return;
      this.saveDraft(result, 'edit', transaction.transactionId);
    });
  }

  private saveDraft(result: CreateTransactionDraft, mode: 'create' | 'edit', transactionId?: number): void {
    if (mode === 'create') {
      this.canCreateInitialTransaction(result.asset.assetCode, result.transactionType).subscribe((canProceed) => {
        if (!canProceed) {
          this.showError(this.firstSellBlockedMessage(result.asset.isDailyAccrualFund));
          return;
        }

        this.persistDraft(result, mode, transactionId);
      });
      return;
    }

    this.persistDraft(result, mode, transactionId);
  }

  private persistDraft(result: CreateTransactionDraft, mode: 'create' | 'edit', transactionId?: number): void {
    this.assetService.ensureExternalAsset({
      assetCode: result.asset.assetCode.trim().toUpperCase(),
      assetName: result.asset.assetName,
      assetType: result.asset.assetType,
      currency: result.asset.currency,
      externalTicker: result.asset.externalTicker
    }).subscribe({
      next: (asset) => {
        const payload = {
          assetId: asset.assetId,
          transactionType: result.transactionType,
          transactionDate: result.transactionDate,
          quantity: result.quantity,
          pricePerUnit: result.pricePerUnit,
          fees: result.fees,
          manufacturingFeePerGram: result.manufacturingFeePerGram,
          dividendKind: result.dividendKind,
          notes: result.notes
        };

        const request = mode === 'edit' && transactionId != null
          ? this.transactionService.update(transactionId, payload)
          : this.transactionService.create(payload);

        request.subscribe({
          next: () => {
            this.showSuccess(mode === 'edit' ? 'تم تعديل المعاملة.' : 'تم إجراء العملية.');
            this.loadTransactions(false);
            this.refreshService.notify('transactions:changed');
          },
          error: (err) => {
            const serverMessage = this.extractServerMessage(err);
            this.showError(serverMessage ?? (mode === 'edit' ? 'تعذر تعديل المعاملة.' : 'تعذر إضافة المعاملة.'));
          }
        });
      },
      error: (err) => {
        const serverMessage = this.extractServerMessage(err);
        this.showError(serverMessage
          ? `تعذر إضافة الأصل من المصدر الخارجي: ${serverMessage}`
          : 'تعذر إضافة الأصل من المصدر الخارجي.');
      }
    });
  }

  private canCreateInitialTransaction(assetCode: string, transactionType: string): Observable<boolean> {
    if (transactionType !== 'Sell') {
      return of(true);
    }

    const normalizedCode = assetCode.trim().toUpperCase();
    const asset = this.assets.find((item) => item.assetCode.trim().toUpperCase() === normalizedCode);
    if (!asset) {
      return of(false);
    }

    return of(this.transactions.some((transaction) =>
      transaction.assetId === asset.assetId && transaction.transactionType === 'Buy'
    ));
  }

  private firstSellBlockedMessage(isDailyAccrualFund?: boolean): string {
    return isDailyAccrualFund
      ? 'لا يمكن تسجيل سحب قبل وجود إيداع سابق لهذا الأصل.'
      : 'لا يمكن تسجيل بيع قبل وجود عملية شراء سابقة لهذا الأصل.';
  }

  deleteTransaction(transaction: Transaction): void {
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '420px',
      maxWidth: 'calc(100vw - 32px)',
      panelClass: 'confirm-delete-dialog-panel',
      backdropClass: 'confirm-delete-backdrop',
      data: {
        title: 'حذف المعاملة',
        message: 'هل تريد حذف هذه المعاملة؟'
      }
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.transactionService.delete(transaction.transactionId).subscribe({
        next: () => {
          this.showSuccess('تم حذف المعاملة.');
          this.loadTransactions(false);
          this.refreshService.notify('transactions:changed');
        },
        error: (err) => {
          this.showError(this.extractServerMessage(err) ?? 'تعذر حذف المعاملة.');
        }
      });
    });
  }

  private showSuccess(message: string): void {
    this.showTransientMessage(message, 'success');
  }

  private showError(message: string): void {
    this.showTransientMessage(message, 'error');
  }

  private showTransientMessage(message: string, type: 'success' | 'error'): void {
    if (this.messageTimer) {
      clearTimeout(this.messageTimer);
    }

    this.successMessage = type === 'success' ? message : null;
    this.error = type === 'error' ? message : null;
    this.cdr.markForCheck();

    this.messageTimer = setTimeout(() => {
      if (type === 'success' && this.successMessage === message) {
        this.successMessage = null;
      }
      if (type === 'error' && this.error === message) {
        this.error = null;
      }
      this.cdr.markForCheck();
    }, 3000);
  }

  private extractServerMessage(err: any): string | null {
    const body = err?.error;
    return (typeof body === 'string' ? body : null) ??
      body?.message ??
      body?.Message ??
      body?.detailed ??
      body?.Detailed ??
      null;
  }


  trackByTransactionId = (_: number, item: Transaction) => item.transactionId;

  transactionTypeLabel(transaction: Transaction): string {
    if (transaction.isDailyAccrualFund) {
      return transaction.transactionType === 'Buy' ? 'إيداع' : transaction.transactionType === 'Sell' ? 'سحب' : transaction.transactionType;
    }

    if (transaction.transactionType === 'Dividend') {
      return transaction.dividendKind === 'Stock' ? 'أرباح (أسهم)' : 'أرباح (نقدي)';
    }

    return transaction.transactionType === 'Buy' ? 'شراء' : transaction.transactionType === 'Sell' ? 'بيع' : transaction.transactionType;
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
}
