import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { catchError, timeout } from 'rxjs/operators';
import { of, Subscription, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PriceFetchService } from '../../../core/services/price-fetch.service';
import { PriceFetchStatus, PriceProviderStatus } from '../../../core/models/models';
import { PriceProvidersService } from '../../../core/services/price-providers.service';
import { RefreshService } from '../../../core/services/refresh.service';
import { BalanceVisibilityService } from '../../../core/services/balance-visibility.service';
import { EodhdApiKeyDialogComponent } from '../../../shared/eodhd-api-key-dialog/eodhd-api-key-dialog.component';

const themeStorageKey = 'investment-web-theme';
const eodhdPromptDismissedKey = 'investy-eodhd-prompt-dismissed';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatButtonModule, MatDialogModule],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  priceSyncStatus: PriceFetchStatus | null = null;
  providerStatus: PriceProviderStatus | null = null;
  providerLoading = true;
  syncingPrices = false;
  syncError: string | null = null;
  readonly isProduction = environment.production;
  private refreshSub?: Subscription;
  isDarkMode = false;
  hideBalances$!: Observable<boolean>;

  constructor(
    private priceFetchService: PriceFetchService, 
    private priceProviders: PriceProvidersService,
    private refreshService: RefreshService,
    private balanceVisibilityService: BalanceVisibilityService,
    private dialog: MatDialog
  ) {
    this.isDarkMode = this.getStoredTheme() === 'dark' || document.documentElement.classList.contains('dark-theme');
    this.applyTheme(this.isDarkMode);
    this.hideBalances$ = this.balanceVisibilityService.hidden$;
  }

  get hideBalances(): boolean {
    return this.balanceVisibilityService.isHidden();
  }

  toggleTheme(): void {
    this.isDarkMode = !this.isDarkMode;
    this.applyTheme(this.isDarkMode);
  }

  toggleBalances(): void {
    this.balanceVisibilityService.toggle();
  }

  ngOnInit(): void {
    this.loadPriceStatus();
    this.loadProviderStatus();
    this.checkEodhdSetup();

    this.refreshSub = this.refreshService.onRefresh('any').subscribe((key) => {
      this.loadPriceStatus();
      if (key === 'prices:changed') {
        this.loadProviderStatus();
      }
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  syncPrices(): void {
    if (this.syncingPrices || this.isSyncDisabled) {
      return;
    }

    this.syncingPrices = true;
    this.syncError = null;

    this.priceFetchService.runFetch().pipe(
      timeout(30000),
      catchError(() => {
        this.syncError = 'تعذر مزامنة الأسعار.';
        return of(null);
      })
    ).subscribe(() => {
      this.loadPriceStatus();
      this.loadProviderStatus();
      this.refreshService.notify('prices:changed');
      this.syncingPrices = false;
    });
  }

  configureApiKey(): void {
    const dialogRef = this.dialog.open(EodhdApiKeyDialogComponent, {
      width: '500px',
      maxWidth: '92vw',
      panelClass: 'eodhd-api-key-dialog-panel',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe((saved) => {
      if (!saved) {
        return;
      }

      this.clearEodhdPromptDismissed();
      this.loadProviderStatus();
      this.refreshService.notify('prices:changed');
    });
  }

  /** Number of API calls one full sync will consume (= number of assets with a ticker). */
  get estimatedCallsPerSync(): number {
    return this.priceSyncStatus?.assetsWithTicker ?? 0;
  }

  /** Combined remaining credits across all EODHD API keys. */
  get remaining(): number {
    if (this.providerStatus?.keys?.length) {
      return this.providerStatus.keys.reduce((sum, key) => sum + key.remaining, 0);
    }
    return this.providerStatus?.remaining ?? 0;
  }

  /** How many full syncs the user can still do today. */
  get availableSyncClicks(): number {
    const callsPerSync = this.estimatedCallsPerSync;
    return callsPerSync > 0 ? Math.floor(this.remaining / callsPerSync) : 0;
  }

  /** True when the button should be disabled: not enough credits, no assets, or provider data not yet loaded. */
  get isSyncDisabled(): boolean {
    if (this.providerLoading || !this.providerStatus) return true;
    const callsPerSync = this.estimatedCallsPerSync;
    return callsPerSync <= 0 || this.remaining < callsPerSync;
  }

  private loadPriceStatus(): void {
    this.priceFetchService.getStatus().pipe(
      timeout(10000),
      catchError(() => of(null as PriceFetchStatus | null))
    ).subscribe(status => {
      this.priceSyncStatus = status;
    });
  }

  private loadProviderStatus(): void {
    this.providerLoading = true;
    this.priceProviders.getEodhdStatus().pipe(
      timeout(5000),
      catchError(() => of(null as PriceProviderStatus | null))
    ).subscribe(status => {
      this.providerStatus = status;
      this.providerLoading = false;
    });
  }

  private checkEodhdSetup(): void {
    if (this.wasEodhdPromptDismissed()) {
      return;
    }

    this.priceProviders.getEodhdConfiguration().pipe(
      timeout(5000),
      catchError(() => of({ hasApiKey: true }))
    ).subscribe((configuration) => {
      if (configuration.hasApiKey || this.dialog.openDialogs.length > 0) {
        return;
      }

      const dialogRef = this.dialog.open(EodhdApiKeyDialogComponent, {
        width: '500px',
        maxWidth: '92vw',
        panelClass: 'eodhd-api-key-dialog-panel',
        disableClose: false
      });

      dialogRef.afterClosed().subscribe((saved) => {
        if (saved) {
          this.clearEodhdPromptDismissed();
          this.loadProviderStatus();
          return;
        }

        this.dismissEodhdPrompt();
      });
    });
  }

  private getStoredTheme(): 'light' | 'dark' | null {
    try {
      const storedTheme = localStorage.getItem(themeStorageKey);
      return storedTheme === 'dark' || storedTheme === 'light' ? storedTheme : null;
    } catch {
      return null;
    }
  }

  private applyTheme(isDarkMode: boolean): void {
    const theme = isDarkMode ? 'dark' : 'light';

    document.documentElement.classList.toggle('dark-theme', isDarkMode);
    document.documentElement.style.colorScheme = theme;
    document.body.classList.toggle('dark-theme', isDarkMode);
    document.body.style.colorScheme = theme;

    try {
      localStorage.setItem(themeStorageKey, theme);
    } catch {
      // Ignore storage failures and keep the in-memory theme state.
    }
  }

  private wasEodhdPromptDismissed(): boolean {
    try {
      return localStorage.getItem(eodhdPromptDismissedKey) === 'true';
    } catch {
      return false;
    }
  }

  private dismissEodhdPrompt(): void {
    try {
      localStorage.setItem(eodhdPromptDismissedKey, 'true');
    } catch {
      // Ignore storage failures.
    }
  }

  private clearEodhdPromptDismissed(): void {
    try {
      localStorage.removeItem(eodhdPromptDismissedKey);
    } catch {
      // Ignore storage failures.
    }
  }
}
