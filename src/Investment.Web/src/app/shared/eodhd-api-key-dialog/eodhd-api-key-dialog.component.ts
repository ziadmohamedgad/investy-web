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
      <header class="dialog-head">
        <div class="icon-shell">
          <mat-icon>vpn_key</mat-icon>
        </div>

        <div class="heading-copy">
          <h2>تفعيل مزامنة الأسعار</h2>
          <p>
            سجّل في
            <a href="https://eodhd.com/" target="_blank" rel="noopener noreferrer">eodhd.com</a>
            ثم ضع مفتاح EODHD هنا. سيتم حفظه داخل قاعدة بياناتك المحلية فقط.
          </p>
        </div>
      </header>

      <div class="field-group">
        <label for="eodhd-api-key">مفتاح EODHD</label>
        <input
          id="eodhd-api-key"
          [formControl]="apiKey"
          autocomplete="off"
          spellcheck="false"
          placeholder="API key" />
      </div>

      <div class="message loading" *ngIf="saving">
        <mat-spinner diameter="18"></mat-spinner>
        <span>جاري التحقق من المفتاح...</span>
      </div>
      <div class="message error" *ngIf="errorMessage">{{ errorMessage }}</div>

      <div class="actions">
        <button
          mat-button
          class="dialog-action save-action"
          type="button"
          [class.is-disabled]="saving || apiKey.invalid"
          [attr.aria-disabled]="saving || apiKey.invalid"
          (click)="save()">
          <mat-icon *ngIf="!saving">check</mat-icon>
          <span>حفظ المفتاح</span>
        <button *ngIf="!dialogRef.disableClose" mat-button class="dialog-action cancel-action" type="button" [disabled]="saving" (click)="cancel()">إلغاء</button>
      </div>
    </section>
  `,
  styles: [`
    .api-key-dialog {
      width: min(456px, calc(100vw - 32px));
      display: grid;
      gap: 18px;
      padding: 24px;
      color: var(--text-primary);
    }

    .dialog-head {
      width: 100%;
      display: grid;
      grid-template-columns: auto 1fr;
      align-items: center;
      gap: 14px;
      text-align: right;
    }

    .icon-shell {
      width: 58px;
      height: 58px;
      display: grid;
      place-items: center;
      border-radius: 18px;
      background:
        linear-gradient(145deg, color-mix(in srgb, var(--primary) 18%, transparent), color-mix(in srgb, var(--accent) 10%, transparent)),
        color-mix(in srgb, var(--bg-surface) 86%, transparent);
      color: var(--primary);
      border: 1px solid color-mix(in srgb, var(--primary) 24%, var(--border-color));
      box-shadow:
        0 14px 30px color-mix(in srgb, var(--primary) 13%, transparent),
        inset 0 1px 0 rgba(255, 255, 255, 0.42);
    }

    .icon-shell mat-icon {
      width: 32px;
      height: 32px;
      font-size: 32px;
    }

    .heading-copy {
      min-width: 0;
    }

    h2 {
      margin: 0 0 6px;
      font-family: 'Changa', 'Cairo', sans-serif;
      font-size: 1.25rem;
      font-weight: 900;
      line-height: 1.4;
      color: var(--text-primary);
    }

    p {
      margin: 0;
      color: var(--text-secondary);
      font-size: 0.96rem;
      font-weight: 650;
      line-height: 1.85;
    }

    a {
      color: var(--primary);
      font-weight: 900;
      text-decoration: none;
    }

    .field-group {
      display: grid;
      gap: 8px;
      width: 100%;
      text-align: right;
    }

    label {
      color: var(--text-primary);
      font-weight: 900;
      font-size: 0.96rem;
    }

    input {
      width: 100%;
      height: 52px;
      box-sizing: border-box;
      direction: ltr;
      text-align: left;
      padding: 0 15px;
      border-radius: 14px;
      border: 1px solid var(--border-color);
      background:
        linear-gradient(180deg, color-mix(in srgb, var(--bg-surface) 94%, transparent), color-mix(in srgb, var(--bg-surface-2) 74%, transparent)),
        var(--bg-surface);
      color: var(--text-primary);
      outline: none;
      font: inherit;
      font-weight: 800;
      box-shadow: var(--shadow-sm);
      transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
    }

    input:focus {
      border-color: color-mix(in srgb, var(--primary) 52%, var(--border-color));
      box-shadow:
        0 0 0 4px color-mix(in srgb, var(--primary) 13%, transparent),
        var(--shadow-sm);
    }

    .message {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 10px 12px;
      border-radius: 14px;
      font-weight: 800;
      text-align: center;
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
      display: grid;
      grid-template-columns: 1.05fr 0.95fr;
      gap: 10px;
      width: 100%;
    }

    .dialog-action {
      min-height: 44px;
      border-radius: 14px;
      border: 1px solid var(--border-color);
      background:
        linear-gradient(180deg, color-mix(in srgb, var(--bg-surface) 94%, transparent), color-mix(in srgb, var(--bg-surface-2) 78%, transparent)),
        var(--bg-surface);
      color: var(--text-primary);
      font-weight: 900;
      box-shadow: var(--shadow-sm);
      transition: transform var(--transition-fast), box-shadow var(--transition-fast), border-color var(--transition-fast), color var(--transition-fast);
    }

    .dialog-action:hover:not(:disabled) {
      transform: translateY(-1px);
      background: var(--primary-soft);
      color: var(--primary);
      border-color: color-mix(in srgb, var(--primary) 34%, var(--border-color));
      box-shadow: var(--shadow-md);
    }

    .dialog-action:active:not(:disabled) {
      transform: translateY(0);
      box-shadow: var(--shadow-sm);
    }

    .save-action {
      color: var(--primary);
      border-color: color-mix(in srgb, var(--primary) 32%, var(--border-color));
      background:
        linear-gradient(180deg, color-mix(in srgb, var(--primary) 15%, transparent), color-mix(in srgb, var(--primary) 9%, transparent)),
        var(--bg-surface);
    }

    .save-action:hover {
      color: var(--primary);
      background:
        linear-gradient(180deg, color-mix(in srgb, var(--primary) 22%, transparent), color-mix(in srgb, var(--accent) 12%, transparent)),
        var(--bg-surface);
      border-color: color-mix(in srgb, var(--primary) 48%, var(--border-color));
      box-shadow:
        0 14px 30px color-mix(in srgb, var(--primary) 20%, transparent),
        inset 0 1px 0 rgba(255, 255, 255, 0.36);
    }

    .save-action mat-icon {
      margin-inline-start: 4px;
    }

    .dialog-action:disabled {
      opacity: 0.72;
      cursor: not-allowed;
      transform: none;
      box-shadow: var(--shadow-sm);
    }

    .save-action.is-disabled {
      opacity: 0.72;
      cursor: not-allowed;
    }

    .save-action.is-disabled:hover {
      opacity: 0.86;
      transform: translateY(-1px);
    }

    @media (max-width: 520px) {
      .api-key-dialog {
        padding: 22px;
      }

      .dialog-head {
        grid-template-columns: 1fr;
        justify-items: center;
        text-align: center;
      }

      .actions {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class EodhdApiKeyDialogComponent {
  apiKey = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] });
  saving = false;
  errorMessage: string | null = null;

  constructor(
    public dialogRef: MatDialogRef<EodhdApiKeyDialogComponent, PriceProviderStatus | false>,
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
