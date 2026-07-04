import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PriceProvidersService } from '../../core/services/price-providers.service';
import { PriceFetchService } from '../../core/services/price-fetch.service';
import { ExportService } from '../../core/services/export.service';
import { PriceProviderStatus } from '../../core/models/models';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit, OnDestroy {
  status: PriceProviderStatus | null = null;
  loadingStatus = false;
  newApiKey = '';
  saving = false;
  syncingPrices = false;
  message = '';
  
  private sub?: Subscription;

  constructor(
    private priceProviders: PriceProvidersService,
    private priceFetch: PriceFetchService,
    private exportService: ExportService
  ) {}

  ngOnInit(): void {
    this.loadStatus();
  }
  
  ngOnDestroy(): void {
    if (this.sub) this.sub.unsubscribe();
  }

  loadStatus(): void {
    this.loadingStatus = true;
    this.sub = this.priceProviders.getEodhdStatus().subscribe({
      next: (res) => {
        this.status = res;
        this.loadingStatus = false;
      },
      error: () => {
        this.status = null;
        this.loadingStatus = false;
      }
    });
  }

  exportWorkbook(): void {
    this.exportService.exportWorkbook();
  }

  syncPrices(): void {
    if (this.syncingPrices) return;
    this.syncingPrices = true;
    this.message = 'جاري مزامنة الأسعار...';
    
    this.priceFetch.runFetch().subscribe({
      next: () => {
        this.syncingPrices = false;
        this.message = 'تم تحديث الأسعار بنجاح.';
        this.loadStatus(); // تحديث بيانات المفتاح بعد המزامنة
        setTimeout(() => this.message = '', 3000);
      },
      error: () => {
        this.syncingPrices = false;
        this.message = 'حدث خطأ أثناء مزامنة الأسعار.';
        setTimeout(() => this.message = '', 3000);
      }
    });
  }

  saveKey(): void {
    if (!this.newApiKey || !this.newApiKey.trim()) return;
    this.saving = true;
    this.message = 'جاري التحقق من المفتاح...';
    
    this.priceProviders.saveEodhdApiKey(this.newApiKey.trim()).subscribe({
      next: (res) => {
        this.saving = false;
        this.status = res;
        this.newApiKey = '';
        this.message = 'تم حفظ المفتاح بنجاح.';
        setTimeout(() => this.message = '', 3000);
      },
      error: () => {
        this.saving = false;
        this.message = 'مفتاح غير صالح أو الخدمة غير متوفرة.';
        setTimeout(() => this.message = '', 3000);
      }
    });
  }

  clearKey(): void {
    if (this.saving) return;
    this.saving = true;
    this.priceProviders.clearEodhdApiKey().subscribe({
      next: () => {
        window.location.href = '/';
      },
      error: () => {
        this.message = 'حدث خطأ أثناء مسح المفتاح';
        this.saving = false;
      }
    });
  }
}
