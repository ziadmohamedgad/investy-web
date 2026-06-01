import { Component, HostListener, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { Asset, Portfolio } from '../../../core/models/models';

@Component({
  selector: 'app-portfolio-assets-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatListModule],
  templateUrl: './portfolio-assets-dialog.component.html',
  styleUrls: ['./portfolio-assets-dialog.component.scss']
})
export class PortfolioAssetsDialogComponent {
  readonly selectedIds = new Set<number>();

  constructor(
    private dialogRef: MatDialogRef<PortfolioAssetsDialogComponent, number[] | undefined>,
    @Inject(MAT_DIALOG_DATA) public data: { portfolio: Portfolio; assets: Asset[] }
  ) {
    for (const asset of data.portfolio.assets || []) {
      this.selectedIds.add(asset.assetId);
    }
  }

  toggle(assetId: number, checked: boolean): void {
    if (checked) {
      this.selectedIds.add(assetId);
    } else {
      this.selectedIds.delete(assetId);
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
    this.dialogRef.close(Array.from(this.selectedIds));
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  private shouldIgnoreEnter(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement | null;
    return target?.tagName?.toLowerCase() === 'textarea';
  }
}
