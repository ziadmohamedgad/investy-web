import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Asset, CreateAsset, AssetSummary, ExternalAssetSearchResult, EnsureAssetRequest, CreateManualAsset, SetAssetCurrentPrice, AssetFinancialSettings } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AssetService {
  private apiUrl = `${environment.apiUrl}/assets`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Asset[]> {
    return this.http.get<Asset[]>(this.apiUrl);
  }

  getById(id: number): Observable<Asset> {
    return this.http.get<Asset>(`${this.apiUrl}/${id}`);
  }

  getSummary(id: number): Observable<AssetSummary> {
    return this.http.get<AssetSummary>(`${this.apiUrl}/${id}/summary`);
  }

  create(asset: CreateAsset): Observable<Asset> {
    return this.http.post<Asset>(this.apiUrl, asset);
  }

  createManual(asset: CreateManualAsset): Observable<Asset> {
    return this.http.post<Asset>(`${this.apiUrl}/manual`, asset);
  }

  setCurrentPrice(assetId: number, request: SetAssetCurrentPrice): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${assetId}/current-price`, request);
  }

  syncCurrentPrice(assetId: number): Observable<{ price: number; date: Date }> {
    return this.http.post<{ price: number; date: Date }>(`${this.apiUrl}/${assetId}/sync-price`, {});
  }

  updateFinancialSettings(assetId: number, request: AssetFinancialSettings): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${assetId}/financial-settings`, request);
  }

  update(id: number, asset: CreateAsset): Observable<Asset> {
    return this.http.put<Asset>(`${this.apiUrl}/${id}`, asset);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  searchExternal(query: string): Observable<ExternalAssetSearchResult[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<ExternalAssetSearchResult[]>(`${this.apiUrl}/search`, { params });
  }

  getNonStockSuggestions(query: string = '', assetType: string = ''): Observable<ExternalAssetSearchResult[]> {
    let params = new HttpParams().set('query', query);
    if (assetType) {
      params = params.set('assetType', assetType);
    }
    return this.http.get<ExternalAssetSearchResult[]>(`${this.apiUrl}/non-stock-suggestions`, { params });
  }

  ensureExternalAsset(request: EnsureAssetRequest): Observable<Asset> {
    return this.http.post<Asset>(`${this.apiUrl}/ensure`, request);
  }

  compareAsset(assetId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${assetId}/compare`);
  }

  getAllSummaries(): Observable<AssetSummary[]> {
    return this.http.get<AssetSummary[]>(`${this.apiUrl}/summaries`);
  }
}
