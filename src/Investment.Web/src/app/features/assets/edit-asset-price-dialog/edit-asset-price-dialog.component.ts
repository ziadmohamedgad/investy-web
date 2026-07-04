import { Component, HostListener, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AssetSummary } from '../../../core/models/models';
import { BalanceVisibilityService } from '../../../core/services/balance-visibility.service';
import { Observable } from 'rxjs';

export interface EditAssetPriceDialogResult {
  price?: number;
  goldCashbackPerGram?: number;
  dailyAccrualAnnualRatePercent?: number;
}

@Component({
  selector: 'app-edit-asset-price-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  templateUrl: './edit-asset-price-dialog.component.html',
  styleUrl: './edit-asset-price-dialog.component.scss'
})
export class EditAssetPriceDialogComponent {
  form;
  hideBalances$!: Observable<boolean>;

  constructor(
    private fb: FormBuilder,
    private balanceVisibilityService: BalanceVisibilityService,
    private dialogRef: MatDialogRef<EditAssetPriceDialogComponent, EditAssetPriceDialogResult | undefined>,
    @Inject(MAT_DIALOG_DATA) public data: { asset: AssetSummary }
  ) {
    this.hideBalances$ = this.balanceVisibilityService.hidden$;
    this.form = this.fb.group({
      price: [null],
      goldCashbackPerGram: [data.asset.goldCashbackPerGram ?? 28.5, [Validators.min(0)]],
      dailyAccrualAnnualRatePercent: [data.asset.dailyAccrualAnnualRatePercent || 16, [Validators.min(0.00000001)]]
    });

    if (this.showPriceInput) {
      this.form.get('price')?.setValidators([Validators.required, Validators.min(0)]);
    }
    this.form.get('price')?.updateValueAndValidity({ emitEvent: false });
  }

  get isGoldOrSilver(): boolean {
    return this.data.asset.assetType === 'Gold' || this.data.asset.assetType === 'Silver';
  }

  get isDailyAccrualFund(): boolean {
    return this.data.asset.isDailyAccrualFund;
  }

  get showPriceInput(): boolean {
    return !this.isDailyAccrualFund;
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
    this.dialogRef.close({
      price: this.showPriceInput ? Number(value.price) : undefined,
      goldCashbackPerGram: this.isGoldOrSilver ? Number(value.goldCashbackPerGram ?? 28.5) : undefined,
      dailyAccrualAnnualRatePercent: this.isDailyAccrualFund ? Number(value.dailyAccrualAnnualRatePercent ?? 16) : undefined
    });
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  private shouldIgnoreEnter(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement | null;
    return target?.tagName?.toLowerCase() === 'textarea';
  }
}
