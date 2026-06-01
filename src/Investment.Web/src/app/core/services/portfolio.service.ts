import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Portfolio, CreatePortfolio, PortfolioSummary } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PortfolioService {
  private apiUrl = `${environment.apiUrl}/portfolios`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Portfolio[]> {
    return this.http.get<Portfolio[]>(this.apiUrl);
  }

  getById(id: number): Observable<Portfolio> {
    return this.http.get<Portfolio>(`${this.apiUrl}/${id}`);
  }

  getSummary(id: number): Observable<PortfolioSummary> {
    return this.http.get<PortfolioSummary>(`${this.apiUrl}/${id}/summary`);
  }

  create(portfolio: CreatePortfolio): Observable<Portfolio> {
    return this.http.post<Portfolio>(this.apiUrl, portfolio);
  }

  update(id: number, portfolio: CreatePortfolio): Observable<Portfolio> {
    return this.http.put<Portfolio>(`${this.apiUrl}/${id}`, portfolio);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  addAsset(portfolioId: number, assetId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${portfolioId}/assets`, { assetId });
  }

  removeAsset(portfolioId: number, assetId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${portfolioId}/assets/${assetId}`);
  }
}
