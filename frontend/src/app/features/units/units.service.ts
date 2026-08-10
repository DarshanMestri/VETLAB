import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../config';

// Small DTOs mirroring your API
export interface Unit {
  id: number;
  symbol: string;
  name?: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class UnitsService {
  private base = `${environment.apiBase}/units`;

  constructor(private http: HttpClient) {}

  list() {
    // API returns a PagedResult; we only need items here
    return this.http.get<{ items: Unit[] }>(`${this.base}?page=1&pageSize=100`);
  }

  create(data: { symbol: string; name?: string | null; isActive: boolean }) {
    return this.http.post<Unit>(this.base, data);
  }

  update(id: number, data: { symbol: string; name?: string | null; isActive: boolean }) {
    return this.http.put(`${this.base}/${id}`, data);
  }

  remove(id: number) {
    return this.http.delete(`${this.base}/${id}`);
  }
}
