import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Holding, Performance, PortfolioAnalyticsSummary } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getHoldings(): Observable<Holding[]> {
    return this.http.get<Holding[]>(`${this.apiUrl}/holdings`);
  }

  getPerformance(period: string = 'ALL', fromDate?: string, toDate?: string): Observable<Performance> {
    let params = new HttpParams().set('period', period);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<Performance>(`${this.apiUrl}/performance`, { params });
  }

  getSummary(): Observable<PortfolioAnalyticsSummary> {
    return this.http.get<PortfolioAnalyticsSummary>(`${this.apiUrl}/summary`);
  }
}
