import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Price, CreatePrice, BulkPriceItem } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PriceService {
  private apiUrl = `${environment.apiUrl}/prices`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Price[]> {
    return this.http.get<Price[]>(this.apiUrl);
  }

  getByAsset(assetId: number): Observable<Price[]> {
    return this.http.get<Price[]>(`${this.apiUrl}/asset/${assetId}`);
  }

  create(price: CreatePrice): Observable<Price> {
    return this.http.post<Price>(this.apiUrl, price);
  }

  bulkCreate(prices: BulkPriceItem[]): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/bulk`, { prices });
  }
}
