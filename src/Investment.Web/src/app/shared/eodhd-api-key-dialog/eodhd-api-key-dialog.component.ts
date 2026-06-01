import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PriceProviderStatus } from '../../core/models/models';
import { PriceProvidersService } from '../../core/services/price-providers.service';

@Component({
  selector: 'app-eodhd-api-key-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <section class="api-key-dialog" dir="rtl">
      <div class="icon-shell">
        <mat-icon>vpn_key</mat-icon>
      </div>

      <h2>تفعيل مزامنة الأسعار</h2>
      <p>
        سجّل في
        <a href="https://eodhd.com/" target="_blank" rel="noopener noreferrer">eodhd.com</a>
        ثم ضع مفتاح EODHD هنا. سيتم حفظ المفتاح داخل قاعدة بياناتك المحلية فقط.
      </p>

      <label class="field-label">
        مفتاح EODHD
        <input
          [formControl]="apiKey"
          autocomplete="off"
          spellcheck="false"
          placeholder="API key" />
      </label>

      <div class="message loading" *ngIf="saving">
        <mat-spinner diameter="18"></mat-spinner>
        <span>جاري التحقق من المفتاح...</span>
      </div>
      <div class="message error" *ngIf="errorMessage">{{ errorMessage }}</div>

      <div class="actions">
        <button mat-button class="dialog-action cancel-action" type="button" [disabled]="saving" (click)="cancel()">إلغاء</button>
        <button mat-button class="dialog-action save-action" type="button" [disabled]="saving || apiKey.invalid" (click)="save()">
          <mat-icon *ngIf="!saving">check</mat-icon>
          <span>حفظ المفتاح</span>
        </button>
      </div>
    </section>
  `,
  styles: [`
    .api-key-dialog {
      width: min(460px, calc(100vw - 32px));
      display: grid;
      justify-items: center;
      gap: 12px;
      padding: 28px 24px 22px;
      text-align: center;
      color: var(--text-primary);
    }

    .icon-shell {
      width: 64px;
      height: 64px;
      display: grid;
      place-items: center;
      border-radius: 22px;
      background:
        linear-gradient(135deg, rgba(37, 99, 235, 0.18), rgba(15, 143, 111, 0.16)),
        var(--bg-surface-2);
      color: var(--primary);
      border: 1px solid rgba(37, 99, 235, 0.22);
      box-shadow: 0 16px 34px rgba(37, 99, 235, 0.14);
    }

    .icon-shell mat-icon {
      width: 34px;
      height: 34px;
      font-size: 34px;
    }

    h2 {
      margin: 4px 0 0;
      font-size: 1.32rem;
      font-weight: 900;
      font-family: 'Changa', 'Cairo', sans-serif;
    }

    p {
      margin: 0;
      max-width: 36ch;
      color: var(--text-secondary);
      line-height: 1.85;
      font-weight: 600;
    }

    a {
      color: var(--primary);
      font-weight: 900;
      text-decoration: none;
    }

    .field-label {
      width: 100%;
      display: grid;
      gap: 8px;
      margin-top: 2px;
      text-align: right;
      font-weight: 900;
      color: var(--text-primary);
    }

    input {
      width: 100%;
      box-sizing: border-box;
      direction: ltr;
      text-align: left;
      padding: 13px 14px;
      border-radius: 16px;
      border: 1px solid var(--border-color);
      background: color-mix(in srgb, var(--bg-surface) 86%, transparent);
      color: var(--text-primary);
      outline: none;
      font: inherit;
      font-weight: 800;
      box-shadow: var(--shadow-sm);
    }

    input:focus {
      border-color: color-mix(in srgb, var(--primary) 58%, var(--border-color));
      box-shadow: 0 0 0 4px color-mix(in srgb, var(--primary) 14%, transparent);
    }

    .message {
      width: 100%;
      box-sizing: border-box;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 10px 12px;
      border-radius: 14px;
      font-weight: 800;
    }

    .loading {
      color: var(--primary);
      background: color-mix(in srgb, var(--primary) 10%, transparent);
    }

    .error {
      color: var(--danger);
      background: color-mix(in srgb, var(--danger) 12%, transparent);
    }

    .actions {
      width: 100%;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
      margin-top: 6px;
    }

    .dialog-action {
      min-height: 42px;
      border-radius: 999px;
      font-weight: 900;
      border: 1px solid var(--border-color);
      background: var(--bg-surface);
      color: var(--text-primary);
      box-shadow: var(--shadow-sm);
      transition:
        transform var(--transition-fast),
        box-shadow var(--transition-fast),
        background-color var(--transition-fast),
        border-color var(--transition-fast),
        color var(--transition-fast);
    }

    .dialog-action:hover {
      transform: translateY(-1px);
      background: var(--primary-soft);
      color: var(--primary);
      border-color: rgba(37, 99, 235, 0.28);
      box-shadow: var(--shadow-md);
    }

    .dialog-action:active {
      transform: translateY(0);
      box-shadow: var(--shadow-sm);
    }

    .save-action {
      color: #fff;
      background: linear-gradient(135deg, var(--primary), #0f8f6f);
      border-color: transparent;
    }

    .save-action:hover {
      color: #fff;
      background: linear-gradient(135deg, var(--primary), #0f8f6f);
      border-color: transparent;
    }

    .save-action mat-icon {
      margin-inline-start: 4px;
    }
  `]
})
export class EodhdApiKeyDialogComponent {
  apiKey = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] });
  saving = false;
  errorMessage: string | null = null;

  constructor(
    private dialogRef: MatDialogRef<EodhdApiKeyDialogComponent, PriceProviderStatus | false>,
    private priceProviders: PriceProvidersService
  ) {}

  save(): void {
    if (this.apiKey.invalid || this.saving) {
      return;
    }

    this.saving = true;
    this.errorMessage = null;

    this.priceProviders.saveEodhdApiKey(this.apiKey.value).subscribe({
      next: (status) => {
        this.saving = false;
        this.dialogRef.close(status);
      },
      error: () => {
        this.errorMessage = 'تعذر التحقق من المفتاح. تأكد من صحته ومن اتصال الإنترنت.';
        this.saving = false;
      }
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
