import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ExportService {
  private apiUrl = `${environment.apiUrl}/export`;

  constructor(private http: HttpClient) {}

  exportHoldings(): void {
    window.location.href = `${this.apiUrl}/holdings`;
  }

  exportTransactions(assetId?: number, type?: string, fromDate?: string, toDate?: string): void {
    let params = new HttpParams();
    if (assetId) params = params.set('assetId', assetId.toString());
    if (type) params = params.set('type', type);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    const url = `${this.apiUrl}/transactions?${params.toString()}`;
    window.location.href = url;
  }
}
