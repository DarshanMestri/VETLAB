// src/app/clients/data/clients.service.ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { API_BASE } from '../../core/api.config';

@Injectable({ providedIn: 'root' })
export class ClientsService {
  private http = inject(HttpClient);

  getAll() {
    return this.http.get<any[]>(`${API_BASE}/clients`);
  }
}
