import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, AfterViewInit } from '@angular/core';
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
import { Transaction, CreateTransactionDraft, ExternalAssetSearchResult, CreateManualAssetDraft } from '../../core/models/models';
import { TransactionDialogComponent, TransactionDialogData } from './transaction-dialog/transaction-dialog.component';
import { ManualAssetDialogComponent } from '../assets/manual-asset-dialog/manual-asset-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { BalanceVisibilityService } from '../../core/services/balance-visibility.service';
import { Observable } from 'rxjs';
import { catchError, finalize, switchMap, timeout } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule, MatProgressSpinnerModule, MatDialogModule, MatPaginatorModule],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionsComponent implements OnInit, AfterViewInit {
  transactions: Transaction[] = [];
  dataSource = new MatTableDataSource<Transaction>([]);
  pageSize = 7;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns: string[] = ['date', 'asset', 'type', 'quantity', 'price', 'total', 'edit', 'actions'];
  loading = true;
  error: string | null = null;
  successMessage: string | null = null;
  hideBalances$!: Observable<boolean>;

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
    this.error = null;
    this.transactionService.getAll().pipe(
      timeout(10000),
      catchError(() => {
        this.error = 'تعذر تحميل المعاملات.';
        return of([] as Transaction[]);
      }),
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe((data) => {
      this.transactions = data;
      this.syncPaginator(data.length);
      this.dataSource.data = data;
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
    const dialogData: TransactionDialogData = { mode: 'create', prefilledAsset };
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
      maxWidth: '95vw'
    });

    dialogRef.afterClosed().subscribe((result: CreateManualAssetDraft | undefined) => {
      if (!result) return;

      this.assetService.createManual({
        assetCode: result.assetCode,
        assetName: result.assetName,
        assetType: result.assetType,
        currency: result.currency,
        notes: result.notes,
        initialPrice: result.pricePerUnit,
        isDailyAccrualFund: result.isDailyAccrualFund,
        dailyAccrualAnnualRatePercent: result.isDailyAccrualFund ? 16 : 0
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
            notes: result.notes
          })
        )
      ).subscribe({
        next: () => {
          this.error = null;
          this.successMessage = `تمت إضافة الأصل ${result.assetCode} والمعاملة بنجاح.`;
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
            this.error = 'تعذر الاتصال بالخادم. تأكد أن واجهة الـ API تعمل على http://localhost:5091';
          } else if (serverMessage) {
            this.error = `تعذر الحفظ: ${serverMessage}`;
          } else {
            this.error = `تعذر إضافة الأصل اليدوي (رمز الخطأ: ${err?.status ?? 'غير معروف'}).`;
          }
          this.cdr.markForCheck();
        }
      });
    });
  }

  openEditTransactionDialog(transaction: Transaction): void {
    const dialogData: TransactionDialogData = { mode: 'edit', transaction };
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
          notes: result.notes
        };

        const request = mode === 'edit' && transactionId != null
          ? this.transactionService.update(transactionId, payload)
          : this.transactionService.create(payload);

        request.subscribe({
          next: () => {
            this.loadTransactions(false);
            this.refreshService.notify('transactions:changed');
          },
          error: () => {
            this.error = mode === 'edit' ? 'تعذر تعديل المعاملة.' : 'تعذر إضافة المعاملة.';
            this.cdr.markForCheck();
          }
        });
      },
      error: (err) => {
        const serverMessage = err?.error?.message || err?.error?.Message || err?.error?.detailed || err?.error?.Detailed;
        this.error = serverMessage
          ? `تعذر إضافة الأصل من المصدر الخارجي: ${serverMessage}`
          : 'تعذر إضافة الأصل من المصدر الخارجي.';
        this.cdr.markForCheck();
      }
    });
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
          this.loadTransactions(false);
          this.refreshService.notify('transactions:changed');
        },
        error: () => {
          this.error = 'تعذر حذف المعاملة.';
          this.cdr.markForCheck();
        }
      });
    });
  }


  trackByTransactionId = (_: number, item: Transaction) => item.transactionId;

  transactionTypeLabel(type: string): string {
    return type === 'Buy' ? 'شراء' : type === 'Sell' ? 'بيع' : type;
  }
  getAssetCodeClass(assetType: string): string {
    if (assetType === 'Gold') {
      return 'asset-code asset-code-gold';
    }

    return assetType === 'Stock'
      ? 'asset-code asset-code-stock'
      : 'asset-code asset-code-non-stock';
  }
}
