import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

const selectionStorageKey = 'investment-web-selected-assets';

@Injectable({ providedIn: 'root' })
export class AssetSelectionService {
  private readonly selectedAssetCodesSubject = new BehaviorSubject<string[] | null>(this.readSelection());
  readonly selectedAssetCodes$ = this.selectedAssetCodesSubject.asObservable();

  get snapshot(): string[] | null {
    return this.selectedAssetCodesSubject.value;
  }

  setSelection(selectedAssetCodes: string[] | null): void {
    const normalizedSelection = this.normalizeSelection(selectedAssetCodes);
    this.selectedAssetCodesSubject.next(normalizedSelection);
    this.writeSelection(normalizedSelection);
  }

  clearSelection(): void {
    this.setSelection(null);
  }

  isSelected(assetCode: string): boolean {
    const selected = this.selectedAssetCodesSubject.value;
    return selected === null || selected.includes(assetCode);
  }

  toggleAssetCode(assetCode: string, allAssetCodes: string[]): void {
    const availableCodes = this.uniqueCodes(allAssetCodes);
    const currentSelection = this.selectedAssetCodesSubject.value;
    const effectiveSelection = currentSelection === null ? availableCodes : currentSelection;

    const nextSelection = effectiveSelection.includes(assetCode)
      ? effectiveSelection.filter(code => code !== assetCode)
      : [...effectiveSelection, assetCode];

    this.setSelection(this.collapseIfAllSelected(nextSelection, availableCodes));
  }

  syncToAvailableAssets(allAssetCodes: string[]): void {
    const availableCodes = this.uniqueCodes(allAssetCodes);
    const currentSelection = this.selectedAssetCodesSubject.value;

    if (currentSelection === null) {
      return;
    }

    const filteredSelection = currentSelection.filter(code => availableCodes.includes(code));
    this.setSelection(this.collapseIfAllSelected(filteredSelection, availableCodes));
  }

  private normalizeSelection(selectedAssetCodes: string[] | null): string[] | null {
    if (selectedAssetCodes === null) {
      return null;
    }

    return this.uniqueCodes(selectedAssetCodes);
  }

  private collapseIfAllSelected(selectedAssetCodes: string[], allAssetCodes: string[]): string[] | null {
    if (selectedAssetCodes.length === 0) {
      return [];
    }

    if (selectedAssetCodes.length === allAssetCodes.length) {
      return null;
    }

    return this.uniqueCodes(selectedAssetCodes);
  }

  private uniqueCodes(codes: string[]): string[] {
    return Array.from(new Set(codes));
  }

  private readSelection(): string[] | null {
    try {
      const stored = localStorage.getItem(selectionStorageKey);
      if (!stored) {
        return null;
      }

      const parsed = JSON.parse(stored) as unknown;
      if (!Array.isArray(parsed)) {
        return null;
      }

      return this.uniqueCodes(parsed.filter((code): code is string => typeof code === 'string'));
    } catch {
      return null;
    }
  }

  private writeSelection(selectedAssetCodes: string[] | null): void {
    try {
      if (selectedAssetCodes === null) {
        localStorage.removeItem(selectionStorageKey);
        return;
      }

      localStorage.setItem(selectionStorageKey, JSON.stringify(selectedAssetCodes));
    } catch {
      // Ignore storage failures and keep the in-memory selection state.
    }
  }
}
