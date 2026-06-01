import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDeleteDialogData {
  title: string;
  message: string;
}

@Component({
  selector: 'app-confirm-delete-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <section class="confirm-delete" dir="rtl">
      <div class="icon-shell">
        <mat-icon>delete_forever</mat-icon>
      </div>

      <h2>{{ data.title }}</h2>
      <p>{{ data.message }}</p>

      <div class="actions">
        <button mat-button class="dialog-action cancel-action" type="button" (click)="close(false)">إلغاء</button>
        <button mat-button class="dialog-action delete-action" type="button" (click)="close(true)">حذف</button>
      </div>
    </section>
  `,
  styles: [`
    .confirm-delete {
      width: min(420px, calc(100vw - 32px));
      display: grid;
      justify-items: center;
      gap: 12px;
      padding: 26px 24px 22px;
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
        linear-gradient(135deg, rgba(220, 38, 38, 0.18), rgba(245, 158, 11, 0.14)),
        var(--bg-surface-2);
      color: var(--danger);
      border: 1px solid rgba(220, 38, 38, 0.22);
      box-shadow: 0 16px 34px rgba(220, 38, 38, 0.16);
    }

    mat-icon {
      width: 34px;
      height: 34px;
      font-size: 34px;
    }

    h2 {
      margin: 4px 0 0;
      font-size: 1.28rem;
      font-weight: 800;
      font-family: 'Changa', 'Cairo', sans-serif;
    }

    p {
      margin: 0;
      max-width: 31ch;
      color: var(--text-secondary);
      line-height: 1.8;
      font-weight: 600;
    }

    .actions {
      width: 100%;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
      margin-top: 8px;
    }

    .dialog-action {
      min-height: 42px;
      border-radius: 999px;
      font-weight: 800;
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

    .delete-action {
      color: var(--danger);
      background: rgba(220, 38, 38, 0.08);
      border-color: rgba(220, 38, 38, 0.20);
    }

    .delete-action:hover {
      color: var(--danger);
      background: rgba(220, 38, 38, 0.14);
      border-color: rgba(220, 38, 38, 0.34);
    }
  `]
})
export class ConfirmDeleteDialogComponent {
  constructor(
    private dialogRef: MatDialogRef<ConfirmDeleteDialogComponent, boolean>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDeleteDialogData
  ) {}

  close(value: boolean): void {
    this.dialogRef.close(value);
  }
}
