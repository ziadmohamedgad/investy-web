import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { CreateManualAssetDraft, ExternalAssetSearchResult } from '../../../core/models/models';
import { AssetService } from '../../../core/services/asset.service';
import { BalanceVisibilityService } from '../../../core/services/balance-visibility.service';
import { Observable, combineLatest, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';

@Component({
  selector: 'app-manual-asset-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatAutocompleteModule
  ],
  templateUrl: './manual-asset-dialog.component.html',
  styleUrl: './manual-asset-dialog.component.scss'
})
export class ManualAssetDialogComponent implements OnInit {
  readonly assetTypes = [
    { value: 'DailyAccrualFund', label: 'صندوق ثاندر كلاود لحظي' },
    { value: 'Gold', label: 'ذهب' },
    { value: 'Fund', label: 'صندوق / مؤشر' },
    { value: 'Other', label: 'أخرى (عقار، ممتلكات، إلخ)' }
  ];

  readonly transactionTypes = [
    { value: 'Buy', label: 'شراء' },
    { value: 'Sell', label: 'بيع' }
  ];

  readonly currencies = ['EGP', 'USD', 'EUR'];

  form;
  assetCodeSuggestions$!: Observable<ExternalAssetSearchResult[]>;
  hideBalances$!: Observable<boolean>;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<ManualAssetDialogComponent, CreateManualAssetDraft | undefined>,
    private assetService: AssetService,
    private balanceVisibilityService: BalanceVisibilityService
  ) {
    this.hideBalances$ = this.balanceVisibilityService.hidden$;
    this.form = this.fb.group({
      assetCode: ['', [Validators.required, Validators.maxLength(50)]],
      assetName: ['', [Validators.required, Validators.maxLength(200)]],
      assetType: ['Fund', [Validators.required]],
      currency: ['EGP', [Validators.required]],
      transactionType: ['Buy', [Validators.required]],
      transactionDate: [this.todayIso(), [Validators.required]],
      quantity: [null as unknown as number, [Validators.required, Validators.min(0.00000001)]],
      pricePerUnit: [null as unknown as number, [Validators.required, Validators.min(0.00000001)]],
      manufacturingFeePerGram: [null as unknown as number, [Validators.min(0)]],
      fees: [null as unknown as number, [Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    this.form.get('assetType')!.valueChanges.subscribe((value) => {
      const isDailyAccrualFund = value === 'DailyAccrualFund';
      if (isDailyAccrualFund) {
        this.form.patchValue({
          assetCode: 'TCD',
          assetName: 'Thndr Cloud Daily'
        }, { emitEvent: false });
      } else {
        this.clearDailyAccrualDefaults();
      }
      this.syncDailyAccrualMode(isDailyAccrualFund);
    });
    this.syncDailyAccrualMode(this.form.get('assetType')!.value === 'DailyAccrualFund');

    const assetCode$ = this.form.get('assetCode')!.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged()
    );

    const assetType$ = this.form.get('assetType')!.valueChanges.pipe(
      startWith('Fund'),
      distinctUntilChanged()
    );

    this.assetCodeSuggestions$ = combineLatest([assetCode$, assetType$]).pipe(
      switchMap(([query, type]) => {
        if (!query || query.trim().length === 0) {
          return of([] as ExternalAssetSearchResult[]);
        }
        return this.assetService.getNonStockSuggestions(query || '', type || '');
      })
    );
  }

  onSuggestionSelected(suggestion: ExternalAssetSearchResult): void {
    this.form.patchValue({
      assetCode: suggestion.assetCode,
      assetName: suggestion.assetName,
      assetType: this.normalizeAssetType(suggestion.assetType),
      currency: suggestion.currency
    });
  }

  private normalizeAssetType(type: string): string {
    const typeMap: { [key: string]: string } = {
      'Gold': 'Gold',
      'Fund': 'Fund',
      'DailyAccrualFund': 'DailyAccrualFund',
      'Other': 'Other',
      'RealEstate': 'Other',
      'Crypto': 'Other'
    };
    return typeMap[type] || 'Other';
  }

  private syncDailyAccrualMode(enabled: boolean): void {
    const priceControl = this.form.get('pricePerUnit');
    if (!priceControl) {
      return;
    }

    if (enabled) {
      priceControl.clearValidators();
      priceControl.setValue(null, { emitEvent: false });
    } else {
      priceControl.setValidators([Validators.required, Validators.min(0.00000001)]);
      if (!priceControl.value || Number(priceControl.value) <= 0) {
        priceControl.setValue(null, { emitEvent: false });
      }
    }

    priceControl.updateValueAndValidity({ emitEvent: false });
  }

  sanitizeTextControl(controlName: 'assetCode' | 'assetName', uppercase = false): void {
    const control = this.form.get(controlName);
    const value = String(control?.value ?? '');
    const sanitized = this.toAscii(value);
    const next = uppercase ? sanitized.toUpperCase() : sanitized;
    if (next !== value) {
      control?.setValue(next, { emitEvent: false });
    }
  }

  private clearDailyAccrualDefaults(): void {
    const assetCode = this.form.get('assetCode')?.value?.trim().toUpperCase();
    const assetName = this.form.get('assetName')?.value?.trim();

    this.form.patchValue({
      assetCode: assetCode === 'TCD' ? '' : this.form.get('assetCode')?.value,
      assetName: assetName === 'Thndr Cloud Daily' ? '' : this.form.get('assetName')?.value
    }, { emitEvent: false });
  }

  get isDailyAccrualFundSelected(): boolean {
    return this.form.get('assetType')?.value === 'DailyAccrualFund';
  }

  get isGoldSelected(): boolean {
    return this.form.get('assetType')?.value === 'Gold';
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

    const value = this.form.getRawValue();
    const quantity = Number(value.quantity);
    const baseFees = Number(value.fees ?? 0);
    const manufacturingFeePerGram = Number(value.manufacturingFeePerGram ?? 0);
    const isDailyAccrualFund = value.assetType === 'DailyAccrualFund';

    const finalAssetType = isDailyAccrualFund ? 'Fund' : value.assetType!;

    this.dialogRef.close({
      assetCode: (isDailyAccrualFund ? 'TCD' : this.toAscii(value.assetCode!)).trim().toUpperCase(),
      assetName: (isDailyAccrualFund ? 'Thndr Cloud Daily' : this.toAscii(value.assetName!)).trim(),
      assetType: finalAssetType,

      currency: value.currency!,
      isDailyAccrualFund,
      transactionType: value.transactionType!,
      transactionDate: new Date(value.transactionDate!),
      quantity,
      pricePerUnit: isDailyAccrualFund ? 1 : Number(value.pricePerUnit),
      fees: baseFees,
      manufacturingFeePerGram
    });
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

  private toAscii(value: string): string {
    return value.replace(/[^\x00-\x7F]/g, '');
  }
}
