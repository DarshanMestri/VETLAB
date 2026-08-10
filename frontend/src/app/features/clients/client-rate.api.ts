import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API = 'http://localhost:5035/api/clientrate';

export enum RateItemType {
  Test = 1,
  Profile = 2,
  Package = 3,
}
export interface ClientRate {
  id: number;
  clientId: number;
  itemId: number;
  itemType: RateItemType;
  rate: number;
  currency?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class ClientRateApi {
  private http = inject(HttpClient);

  list(clientId: number, itemType?: RateItemType): Observable<ClientRate[]> {
    let params = new HttpParams().set('clientId', clientId);
    if (itemType) params = params.set('itemType', itemType);
    return this.http.get<ClientRate[]>(API, { params });
  }
  create(payload: Omit<ClientRate, 'id'>) {
    return this.http.post<ClientRate>(API, payload);
  }
  update(id: number, payload: Omit<ClientRate, 'id' | 'clientId'>) {
    return this.http.put<ClientRate>(`${API}/${id}`, payload);
  }
  delete(id: number) {
    return this.http.delete(`${API}/${id}`);
  }
}
