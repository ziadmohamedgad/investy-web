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

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatButtonModule, MatDialogModule],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  readonly isProduction = environment.production;
  private refreshSub?: Subscription;
  isDarkMode = false;
  hideBalances$!: Observable<boolean>;
  private eodhdPromptDismissedForSession = false;

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
    this.checkEodhdSetup();

    this.refreshSub = this.refreshService.onRefresh('any').subscribe((key) => {
      // Keep refresh sub for future use
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }



  configureApiKey(): void {
    const dialogRef = this.dialog.open(EodhdApiKeyDialogComponent, {
      width: '460px',
      maxWidth: '92vw',
      panelClass: 'eodhd-api-key-dialog-panel',
      backdropClass: 'confirm-delete-backdrop',
      autoFocus: '#eodhd-api-key',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe((saved) => {
      if (!saved) {
        return;
      }

      this.eodhdPromptDismissedForSession = false;
      this.applyProviderStatus(saved);
      this.refreshService.notify('prices:changed');
    });
  }



  private checkEodhdSetup(): void {
    if (this.eodhdPromptDismissedForSession) {
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
        width: '460px',
        maxWidth: '92vw',
        panelClass: 'eodhd-api-key-dialog-panel',
        backdropClass: 'api-key-blocking-backdrop',
        autoFocus: '#eodhd-api-key',
        disableClose: true
      });

      dialogRef.afterClosed().subscribe((saved) => {
        if (saved) {
          this.applyProviderStatus(saved);
          this.refreshService.notify('prices:changed');
        }
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

  private applyProviderStatus(status: PriceProviderStatus | null): void {
    // Only used to clear loading state or update if we had it, but now unused in template
  }

  private dismissEodhdPrompt(): void {
    this.eodhdPromptDismissedForSession = true;
  }
}
