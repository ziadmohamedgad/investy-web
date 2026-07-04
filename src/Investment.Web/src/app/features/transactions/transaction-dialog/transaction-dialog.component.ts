import { Component, HostListener, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Asset, AssetSummary, CreateTransactionDraft, ExternalAssetSearchResult, Transaction } from '../../../core/models/models';
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
  knownAssets?: Asset[];
  assetSummaries?: AssetSummary[];
  assetIdsWithBuy?: number[];
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

  readonly dailyAccrualTransactionTypes = [
    { value: 'Buy', label: 'إيداع' },
    { value: 'Sell', label: 'سحب' }
  ];

  readonly today = this.todayIso();
  form;
  searchResults: ExternalAssetSearchResult[] = [];
  searchLoading = false;
  selectedAsset: ExternalAssetSearchResult | null = null;
  sellAllowed = false;
  sellAvailabilityLoading = false;
  availableSellAmount = 0;
  availableSellMarketValue = 0;
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
      transactionDate: [this.today, [Validators.required, this.notFutureDateValidator]],
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

    this.form.get('transactionDate')!.valueChanges.subscribe(() => this.refreshSellAvailability());
    this.form.get('transactionType')!.valueChanges.subscribe(() => this.refreshSellAvailability());

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

        const normalized = this.toAssetCode(value?.trim() ?? '');
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

  get currentTransactionTypes(): { value: string; label: string }[] {
    return this.isDailyAccrualFundSelected ? this.dailyAccrualTransactionTypes : this.transactionTypes;
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
    if (this.form.invalid || this.sellBlocked || this.sellExceedsAvailable || this.sellNetInvalid || this.sellExceedsMarketValue) {
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

    const next = this.toAssetCode(value).toUpperCase();
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
    this.refreshSellAvailability(asset);
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

  get sellBlockedHint(): string | null {
    if (!this.selectedAsset || this.sellAllowed || this.sellAvailabilityLoading) {
      return null;
    }

    return this.isDailyAccrualFundSelected
      ? 'السحب متاح فقط بعد وجود إيداع سابق لهذا الأصل.'
      : 'البيع متاح فقط بعد وجود عملية شراء سابقة لهذا الأصل.';
  }

  get sellBlocked(): boolean {
    return this.form.get('transactionType')?.value === 'Sell' && !this.sellAllowed;
  }

  get sellExceedsAvailable(): boolean {
    if (this.form.get('transactionType')?.value !== 'Sell') {
      return false;
    }

    const quantity = Number(this.form.get('quantity')?.value ?? 0);
    return quantity > 0 && this.availableSellAmount > 0 && quantity > this.availableSellAmount + 0.0000001;
  }

  get sellNetInvalid(): boolean {
    const quantity = Number(this.form.get('quantity')?.value ?? 0);
    return this.form.get('transactionType')?.value === 'Sell' && quantity > 0 && this.transactionNetAmount < 0;
  }

  get sellExceedsMarketValue(): boolean {
    const quantity = Number(this.form.get('quantity')?.value ?? 0);
    return this.form.get('transactionType')?.value === 'Sell'
      && quantity > 0
      && this.transactionNetAmount > this.availableSellMarketValue + 0.01;
  }

  get transactionNetAmount(): number {
    const quantity = Number(this.form.get('quantity')?.value ?? 0);
    const price = this.isDailyAccrualFundSelected ? 1 : Number(this.form.get('pricePerUnit')?.value ?? 0);
    const fees = this.isDailyAccrualFundSelected ? 0 : Number(this.form.get('fees')?.value ?? 0);
    const goldAdjustment = this.isGoldSelected ? quantity * Number(this.form.get('manufacturingFeePerGram')?.value ?? 0) : 0;
    const gross = this.isDailyAccrualFundSelected ? quantity : quantity * price + goldAdjustment;

    return this.form.get('transactionType')?.value === 'Buy'
      ? gross + fees
      : gross - fees;
  }

  get transactionTotalLabel(): string {
    return this.transactionNetAmount.toLocaleString('ar-EG', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  get submitLabel(): string {
    const isBuy = this.form.get('transactionType')?.value === 'Buy';
    if (this.isDailyAccrualFundSelected) {
      return isBuy ? 'إيداع' : 'سحب';
    }

    return isBuy ? 'شراء' : 'بيع';
  }

  get availableSellLabel(): string {
    return this.availableSellAmount.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: this.isDailyAccrualFundSelected ? 2 : 5
    });
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

  private notFutureDateValidator = (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }

    return new Date(value).setHours(0, 0, 0, 0) > new Date(this.today).setHours(0, 0, 0, 0)
      ? { futureDate: true }
      : null;
  };

  private refreshSellAvailability(asset = this.selectedAsset): void {
    if (!asset) {
      this.sellAllowed = false;
      this.availableSellAmount = 0;
      this.availableSellMarketValue = 0;
      this.ensureTransactionTypeAllowed();
      return;
    }

    const normalizedCode = asset.assetCode.trim().toUpperCase();
    const existingAsset = this.data.transaction
      ? this.data.knownAssets?.find((item) => item.assetId === this.data.transaction!.assetId)
      : this.data.knownAssets?.find((item) => item.assetCode.trim().toUpperCase() === normalizedCode);

    if (!existingAsset) {
      this.sellAllowed = false;
      this.availableSellAmount = 0;
      this.availableSellMarketValue = 0;
      this.ensureTransactionTypeAllowed();
      return;
    }

    this.sellAllowed = (this.data.assetIdsWithBuy ?? []).includes(existingAsset.assetId);
    this.calculateAvailableSellValues(existingAsset);
    this.ensureTransactionTypeAllowed();
  }

  private ensureTransactionTypeAllowed(): void {
    if (!this.sellAllowed && this.form.get('transactionType')?.value === 'Sell') {
      this.form.get('transactionType')?.setValue('Buy', { emitEvent: false });
    }
  }

  private calculateAvailableSellValues(asset: Asset): void {
    const summary = this.data.assetSummaries?.find((item) => item.assetId === asset.assetId);
    const editedSell = this.data.transaction?.transactionType === 'Sell' ? this.data.transaction : null;

    if (!asset.isDailyAccrualFund) {
      const editedQuantity = editedSell?.assetId === asset.assetId ? editedSell.quantity : 0;
      this.availableSellAmount = Math.max(0, (summary?.totalUnitsHeld ?? 0) + editedQuantity);
    } else {
      const editedAmount = editedSell?.assetId === asset.assetId ? editedSell.totalAmount : 0;
      this.availableSellAmount = Math.max(0, (summary?.currentValue ?? 0) + editedAmount);
    }

    const editedNet = editedSell?.assetId === asset.assetId ? editedSell.netAmount : 0;
    this.availableSellMarketValue = Math.max(0, (summary?.currentValue ?? 0) + editedNet);
  }

  private toAscii(value: string): string {
    return value.replace(/[^\x00-\x7F]/g, '');
  }

  private toAssetCode(value: string): string {
    return value.replace(/[^A-Za-z]/g, '');
  }
}

