import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
    <section class="api-key-card" dir="rtl">
      <div class="icon-wrap">
        <mat-icon>vpn_key</mat-icon>
      </div>

      <h2>إعداد مزامنة الأسعار</h2>
      <p>
        لإنڤيستي يحتاج مفتاح EODHD للبحث عن الأسهم ومزامنة الأسعار. سجّل في
        <a href="https://eodhd.com/" target="_blank" rel="noopener noreferrer">eodhd.com</a>
        ثم ضع المفتاح هنا. سيتم حفظه داخل قاعدة بياناتك المحلية فقط.
      </p>

      <label class="field-label">
        مفتاح EODHD
        <input
          [formControl]="apiKey"
          autocomplete="off"
          spellcheck="false"
          placeholder="API key" />
      </label>

      <div class="message error" *ngIf="errorMessage">{{ errorMessage }}</div>
      <div class="message success" *ngIf="successMessage">{{ successMessage }}</div>

      <div class="actions">
        <button mat-button type="button" class="secondary-action" [disabled]="saving" (click)="cancel()">إلغاء</button>
        <button mat-button type="button" class="primary-action" [disabled]="saving || apiKey.invalid" (click)="save()">
          <mat-spinner *ngIf="saving" diameter="18"></mat-spinner>
          <mat-icon *ngIf="!saving">check</mat-icon>
          <span>موافق</span>
        </button>
      </div>
    </section>
  `,
  styles: [`
    .api-key-card {
      width: min(460px, 86vw);
      padding: 28px;
      text-align: center;
      color: var(--text-primary);
    }

    .icon-wrap {
      width: 58px;
      height: 58px;
      margin: 0 auto 14px;
      display: grid;
      place-items: center;
      border-radius: 18px;
      background: linear-gradient(135deg, rgba(37, 99, 235, .16), rgba(15, 143, 111, .16));
      color: var(--primary);
      border: 1px solid rgba(37, 99, 235, .18);
    }

    .icon-wrap mat-icon {
      width: 30px;
      height: 30px;
      font-size: 30px;
    }

    h2 {
      margin: 0 0 10px;
      font-size: 1.35rem;
      font-weight: 800;
    }

    p {
      margin: 0 0 20px;
      color: var(--text-secondary);
      line-height: 1.8;
    }

    a {
      color: var(--primary);
      font-weight: 800;
      text-decoration: none;
    }

    .field-label {
      display: grid;
      gap: 8px;
      text-align: right;
      font-weight: 800;
      color: var(--text-primary);
    }

    input {
      width: 100%;
      box-sizing: border-box;
      direction: ltr;
      text-align: left;
      padding: 13px 14px;
      border-radius: 14px;
      border: 1px solid var(--border-color);
      background: color-mix(in srgb, var(--bg-surface) 82%, transparent);
      color: var(--text-primary);
      outline: none;
      font: inherit;
      font-weight: 700;
    }

    input:focus {
      border-color: color-mix(in srgb, var(--primary) 58%, var(--border-color));
      box-shadow: 0 0 0 4px color-mix(in srgb, var(--primary) 14%, transparent);
    }

    .message {
      margin-top: 14px;
      padding: 10px 12px;
      border-radius: 12px;
      font-weight: 800;
    }

    .error {
      color: var(--danger);
      background: color-mix(in srgb, var(--danger) 12%, transparent);
    }

    .success {
      color: var(--accent);
      background: color-mix(in srgb, var(--accent) 12%, transparent);
    }

    .actions {
      margin-top: 20px;
      display: flex;
      justify-content: center;
      gap: 10px;
    }

    .primary-action,
    .secondary-action {
      min-width: 112px;
      border-radius: 999px;
      font-weight: 900;
      border: 1px solid var(--border-color);
      transition: transform .18s ease, background-color .18s ease, box-shadow .18s ease;
    }

    .primary-action {
      color: #fff;
      background: linear-gradient(135deg, var(--primary), #0f8f6f);
      border-color: transparent;
      box-shadow: 0 10px 22px rgba(37, 99, 235, .18);
    }

    .secondary-action {
      background: color-mix(in srgb, var(--bg-surface) 86%, transparent);
      color: var(--text-primary);
    }

    .primary-action:hover,
    .secondary-action:hover {
      transform: translateY(-1px);
    }
  `]
})
export class EodhdApiKeyDialogComponent {
  apiKey = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] });
  saving = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  constructor(
    private dialogRef: MatDialogRef<EodhdApiKeyDialogComponent, boolean>,
    private priceProviders: PriceProvidersService
  ) {}

  save(): void {
    if (this.apiKey.invalid || this.saving) {
      return;
    }

    this.saving = true;
    this.errorMessage = null;
    this.successMessage = null;

    this.priceProviders.saveEodhdApiKey(this.apiKey.value).subscribe({
      next: () => {
        this.successMessage = 'تم حفظ المفتاح بنجاح.';
        this.saving = false;
        this.dialogRef.close(true);
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
