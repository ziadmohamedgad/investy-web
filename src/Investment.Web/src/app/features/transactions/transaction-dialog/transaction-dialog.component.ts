import { Component, HostListener, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CreateTransactionDraft, ExternalAssetSearchResult, Transaction } from '../../../core/models/models';
import { AssetService } from '../../../core/services/asset.service';
import { BalanceVisibilityService } from '../../../core/services/balance-visibility.service';
import { Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';

export type TransactionDialogMode = 'create' | 'edit';

export interface TransactionDialogData {
  mode: TransactionDialogMode;
  transaction?: Transaction;
  /** Pre-select an asset when opening the stock transaction dialog after manual asset creation. */
  prefilledAsset?: ExternalAssetSearchResult;
}

@Component({
  selector: 'app-transaction-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './transaction-dialog.component.html',
  styleUrl: './transaction-dialog.component.scss'
})
export class TransactionDialogComponent {
  readonly transactionTypes = [
    { value: 'Buy', label: 'شراء' },
    { value: 'Sell', label: 'بيع' }
  ];

  form;
  searchResults: ExternalAssetSearchResult[] = [];
  searchLoading = false;
  selectedAsset: ExternalAssetSearchResult | null = null;
  hideBalances$!: Observable<boolean>;

  constructor(
    private fb: FormBuilder,
    private assetService: AssetService,
    private balanceVisibilityService: BalanceVisibilityService,
    private dialogRef: MatDialogRef<TransactionDialogComponent, CreateTransactionDraft | undefined>,
    @Inject(MAT_DIALOG_DATA) public data: TransactionDialogData
  ) {
    this.hideBalances$ = this.balanceVisibilityService.hidden$;
    this.form = this.fb.group({
      assetQuery: ['', [Validators.required]],
      transactionType: ['Buy', [Validators.required]],
      transactionDate: [this.todayIso(), [Validators.required]],
      quantity: [null as unknown as number, [Validators.required, Validators.min(0.00000001)]],
      pricePerUnit: [null as unknown as number, [Validators.required, Validators.min(0)]],
      manufacturingFeePerGram: [null as unknown as number, [Validators.min(0)]],
      fees: [null as unknown as number, [Validators.min(0)]],
      notes: ['']
    });

    if (this.data.mode === 'edit' && this.data.transaction) {
      const existingAsset: ExternalAssetSearchResult = {
        assetCode: this.data.transaction.assetCode,
        assetName: this.data.transaction.assetName,
        assetType: this.data.transaction.assetType,
        currency: 'EGP',
        externalTicker: this.data.transaction.assetCode,
        isDailyAccrualFund: this.data.transaction.isDailyAccrualFund
      };
      this.applySelectedAsset(existingAsset, {
        transactionType: this.data.transaction.transactionType,
        transactionDate: this.formatDateForInput(this.data.transaction.transactionDate),
        quantity: this.data.transaction.quantity,
        pricePerUnit: this.data.transaction.pricePerUnit,
        manufacturingFeePerGram: this.data.transaction.manufacturingFeePerGram ?? 0,
        fees: this.data.transaction.fees ?? 0,
        notes: this.data.transaction.notes ?? ''
      });
      this.form.get('assetQuery')!.disable({ emitEvent: false });
    } else if (this.data.prefilledAsset) {
      this.applySelectedAsset(this.data.prefilledAsset);
    }

    const assetQueryControl = this.form.get('assetQuery')!;
    (assetQueryControl.valueChanges as any).pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap((value: string | ExternalAssetSearchResult | null) => {
        this.searchLoading = true;
        if (value && typeof value !== 'string') {
          this.searchLoading = false;
          return of([] as ExternalAssetSearchResult[]);
        }

        const normalized = this.toAscii(value?.trim() ?? '');
        if (normalized.length < 1) {
          this.selectedAsset = null;
          this.searchLoading = false;
          return of([] as ExternalAssetSearchResult[]);
        }
        return this.assetService.searchExternal(normalized).pipe(
          catchError(() => of([] as ExternalAssetSearchResult[]))
        );
      })
    ).subscribe((results: ExternalAssetSearchResult[]) => {
      this.searchResults = results;
      this.searchLoading = false;
    });
  }

  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardShortcut(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.cancel();
      return;
    }

    if (event.key === 'Enter' && !this.shouldIgnoreEnter(event)) {
      event.preventDefault();
      this.submit();
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.selectedAsset) {
      this.form.get('assetQuery')!.setErrors({ required: true });
      return;
    }

    const value = this.form.getRawValue();
    const quantity = Number(value.quantity);

    this.dialogRef.close({
      asset: this.selectedAsset,
      transactionType: value.transactionType!,
      transactionDate: new Date(value.transactionDate!),
      quantity,
      pricePerUnit: this.selectedAsset?.isDailyAccrualFund ? 1 : Number(value.pricePerUnit),
      fees: Number(value.fees ?? 0),
      manufacturingFeePerGram: Number(value.manufacturingFeePerGram ?? 0),
      notes: value.notes?.trim() || undefined
    });
  }

  onAssetSelected(asset: ExternalAssetSearchResult): void {
    if (this.isEditMode) {
      return;
    }

    this.applySelectedAsset(asset);
    this.searchLoading = false;
    this.searchResults = [];
  }

  sanitizeAssetQuery(): void {
    if (this.isEditMode) {
      return;
    }

    const control = this.form.get('assetQuery');
    const value = control?.value;
    if (typeof value !== 'string') {
      return;
    }

    const next = this.toAscii(value).toUpperCase();
    if (next !== value) {
      control?.setValue(next, { emitEvent: true });
    }
  }

  private applySelectedAsset(
    asset: ExternalAssetSearchResult,
    extra?: Partial<{
      transactionType: string;
      transactionDate: string;
      quantity: number;
      pricePerUnit: number;
      manufacturingFeePerGram: number;
      fees: number;
      notes: string;
    }>
  ): void {
    this.selectedAsset = asset;
    this.form.patchValue({
      assetQuery: this.displayAsset(asset),
      ...extra
    }, { emitEvent: false });
    this.syncDailyAccrualMode(asset.isDailyAccrualFund);
    this.syncGoldMode(asset.assetType === 'Gold');
  }

  displayAsset(asset?: ExternalAssetSearchResult | null): string {
    if (!asset) return '';
    return `${asset.assetCode} — ${asset.assetName}`;
  }

  private syncDailyAccrualMode(enabled: boolean): void {
    const priceControl = this.form.get('pricePerUnit');
    if (!priceControl) {
      return;
    }

    if (enabled) {
      priceControl.clearValidators();
      priceControl.setValue(1, { emitEvent: false });
    } else {
      priceControl.setValidators([Validators.required, Validators.min(0)]);
      if (!priceControl.value || Number(priceControl.value) <= 0) {
        priceControl.setValue(null, { emitEvent: false });
      }
    }

    priceControl.updateValueAndValidity({ emitEvent: false });
  }

  get isDailyAccrualFundSelected(): boolean {
    return this.selectedAsset?.isDailyAccrualFund ?? false;
  }

  get isGoldSelected(): boolean {
    return this.selectedAsset?.assetType === 'Gold';
  }

  get isGoldBuySelected(): boolean {
    return this.isGoldSelected && this.form.get('transactionType')?.value === 'Buy';
  }

  get quantityLabel(): string {
    if (this.isDailyAccrualFundSelected) return 'المبلغ';
    return this.isGoldSelected ? 'عدد الجرامات' : 'الكمية';
  }

  get priceLabel(): string {
    return this.isGoldSelected ? 'السعر للجرام' : 'السعر للوحدة';
  }

  get goldAdjustmentLabel(): string {
    return this.isGoldBuySelected ? 'المصنعية للجرام' : 'الكاش باك للجرام';
  }

  private syncGoldMode(enabled: boolean): void {
    const manufacturingFeeControl = this.form.get('manufacturingFeePerGram');
    if (!manufacturingFeeControl) {
      return;
    }

    if (!enabled) {
      manufacturingFeeControl.setValue(null, { emitEvent: false });
    }

    manufacturingFeeControl.updateValueAndValidity({ emitEvent: false });
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  private shouldIgnoreEnter(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement | null;
    const tagName = target?.tagName?.toLowerCase();
    return tagName === 'textarea' || !!document.querySelector('.mat-mdc-autocomplete-panel');
  }

  private todayIso(): string {
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  private formatDateForInput(date: Date | string): string {
    const d = new Date(date);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  private toAscii(value: string): string {
    return value.replace(/[^\x00-\x7F]/g, '');
  }
}

