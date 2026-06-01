import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class RefreshService {
  private refreshSubject = new Subject<string>();

  notify(key: string) {
    this.refreshSubject.next(key);
  }

  onRefresh(key: string): Observable<string> {
    return this.refreshSubject.asObservable().pipe(
      filter((refreshKey) => key === 'any' || refreshKey === key)
    );
  }
}
