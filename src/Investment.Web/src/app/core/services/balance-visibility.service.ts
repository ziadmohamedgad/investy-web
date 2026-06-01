import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

const balanceVisibilityStorageKey = 'investment-web-hide-balances';

@Injectable({
  providedIn: 'root'
})
export class BalanceVisibilityService {
  private readonly hiddenSubject: BehaviorSubject<boolean>;
  readonly hidden$: Observable<boolean>;

  constructor() {
    const storedValue = this.readStoredValue();
    this.hiddenSubject = new BehaviorSubject<boolean>(storedValue);
    this.hidden$ = this.hiddenSubject.asObservable();
  }

  isHidden(): boolean {
    return this.hiddenSubject.value;
  }

  toggle(): void {
    this.setHidden(!this.hiddenSubject.value);
  }

  setHidden(hidden: boolean): void {
    this.hiddenSubject.next(hidden);
    this.applyDocumentClass(hidden);

    try {
      localStorage.setItem(balanceVisibilityStorageKey, hidden ? '1' : '0');
    } catch {
      // Ignore storage failures and keep the in-memory state.
    }
  }

  private readStoredValue(): boolean {
    try {
      const hidden = localStorage.getItem(balanceVisibilityStorageKey) === '1';
      this.applyDocumentClass(hidden);
      return hidden;
    } catch {
      return false;
    }
  }

  private applyDocumentClass(hidden: boolean): void {
    document.body.classList.toggle('hide-balances', hidden);
    document.documentElement.classList.toggle('hide-balances', hidden);
  }
}