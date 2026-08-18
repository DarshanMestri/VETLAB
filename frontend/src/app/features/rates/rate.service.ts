import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin } from 'rxjs';
import { map, switchMap, catchError } from 'rxjs/operators';

export interface RateClient {
  id: number;
  code?: string | null;
  name: string;
  isActive: boolean;
}

export interface RateItem {
  id: number;
  code?: string | null;
  name: string;
}

export interface ClientRate {
  id: number;
  clientId: number;
  itemId: number;
  itemType: number;
  rate: number;
  vatRate: number;
  currency?: string | null;
  isActive: boolean;
}

export interface BulkRateItem {
  itemType: number;
  itemId: number;
  rate: number | null;
  vatRate: number;
  currency?: string;
  isActive: boolean;
}

export interface BulkRateResult {
  created: number;
  updated: number;
  removed: number;
}

interface Paged<T> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class RateService {
  private http = inject(HttpClient);

  getClients(): Observable<RateClient[]> {
    return this.http.get<RateClient[]>('/api/clients').pipe(catchError(() => of([])));
  }

  getTests(): Observable<RateItem[]> {
    return this.fetchAllPages('/api/tests', 1000);
  }

  getProfiles(): Observable<RateItem[]> {
    return this.fetchAllPages('/api/profiles', 200);
  }

  getPackages(): Observable<RateItem[]> {
    return this.fetchAllPages('/api/packages', 200);
  }

  getMedicines(): Observable<RateItem[]> {
    return this.fetchAllPages('/api/mastercatalog/medicines', 100);
  }

  listRates(clientId: number): Observable<ClientRate[]> {
    return this.http
      .get<ClientRate[]>('/api/clientrate', { params: { clientId: clientId.toString() } })
      .pipe(catchError(() => of([])));
  }

  bulkUpsert(clientId: number, items: BulkRateItem[]): Observable<BulkRateResult> {
    return this.http.put<BulkRateResult>('/api/clientrate/bulk', { clientId, items });
  }

  private toItem = (x: any): RateItem => ({ id: x.id, code: x.code, name: x.name });

  private fetchAllPages(url: string, pageSize: number, maxPages = 25): Observable<RateItem[]> {
    return this.http
      .get<Paged<any>>(url, { params: { page: '1', pageSize: pageSize.toString() } })
      .pipe(
        switchMap((res) => {
          const total = res?.total ?? 0;
          const pages = Math.min(Math.ceil(total / pageSize), maxPages);
          const firstItems = (res?.items ?? []).map(this.toItem);
          if (pages <= 1) return of(firstItems);

          const calls: Observable<Paged<any>>[] = [];
          for (let p = 2; p <= pages; p++) {
            calls.push(
              this.http.get<Paged<any>>(url, {
                params: { page: p.toString(), pageSize: pageSize.toString() },
              })
            );
          }
          return forkJoin(calls).pipe(
            map((others) => firstItems.concat(...others.flatMap((o) => o?.items ?? []).map(this.toItem))),
            catchError(() => of(firstItems))
          );
        }),
        catchError(() => of([]))
      );
  }
}
