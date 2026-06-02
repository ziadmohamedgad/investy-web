import { Component, HostListener, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CreateAsset, Asset } from '../../../core/models/models';

export type AssetDialogMode = 'create' | 'edit';

@Component({
  selector: 'app-asset-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule
  ],
  templateUrl: './asset-dialog.component.html',
  styleUrl: './asset-dialog.component.scss'
})
export class AssetDialogComponent {
  readonly assetTypes = [
    { value: 'Stock', label: 'سهم' },
    { value: 'ETF', label: 'صندوق مؤشرات' },
    { value: 'Bond', label: 'سند' },
    { value: 'Crypto', label: 'عملة رقمية' },
    { value: 'Cash', label: 'نقد' },
    { value: 'Fund', label: 'صندوق' },
    { value: 'Gold', label: 'ذهب' },
    { value: 'Other', label: 'أخرى' }
  ];
  readonly currencies = ['EGP', 'USD', 'EUR', 'GBP'];

  form;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AssetDialogComponent, CreateAsset | undefined>,
    @Inject(MAT_DIALOG_DATA) public data: { mode: AssetDialogMode; asset?: Asset }
  ) {
    this.form = this.fb.group({
      assetCode: ['', [Validators.required, Validators.maxLength(20)]],
      assetName: ['', [Validators.required, Validators.maxLength(100)]],
      assetType: ['Stock', [Validators.required]],
      currency: ['EGP', [Validators.required]],
      externalTicker: [''],
      isActive: [true, [Validators.required]]
    });

    if (data.asset) {
      this.form.patchValue({
        assetCode: data.asset.assetCode,
        assetName: data.asset.assetName,
        assetType: data.asset.assetType,
        currency: data.asset.currency,
        externalTicker: data.asset.externalTicker ?? '',
        isActive: data.asset.isActive
      });
    }
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
      assetCode: this.toAssetCode(value.assetCode!).trim().toUpperCase(),
      assetName: this.toAscii(value.assetName!).trim(),
      assetType: value.assetType!,
      currency: value.currency!,
      externalTicker: this.toAscii(value.externalTicker ?? '').trim() || undefined,
      isActive: !!value.isActive
    });
  }

  sanitizeTextControl(controlName: 'assetCode' | 'assetName' | 'externalTicker', uppercase = false): void {
    const control = this.form.get(controlName);
    const value = String(control?.value ?? '');
    const sanitized = controlName === 'assetCode' ? this.toAssetCode(value) : this.toAscii(value);
    const next = uppercase ? sanitized.toUpperCase() : sanitized;
    if (next !== value) {
      control?.setValue(next, { emitEvent: false });
    }
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  private shouldIgnoreEnter(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement | null;
    return target?.tagName?.toLowerCase() === 'textarea';
  }

  private toAscii(value: string): string {
    return value.replace(/[^\x00-\x7F]/g, '');
  }

  private toAssetCode(value: string): string {
    return value.replace(/[^A-Za-z]/g, '');
  }
}

