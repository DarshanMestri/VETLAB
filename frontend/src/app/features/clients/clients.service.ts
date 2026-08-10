import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../config';

export interface Client {
  id: number;
  name: string;
  code?: string | null;
  email?: string | null;
  phone?: string | null;
  billingAddress?: string | null;
  shippingAddress?: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class ClientsService {
  private base = `${environment.apiBase}/clients`;
  constructor(private http: HttpClient) {}

  list() {
    return this.http.get<{ items: Client[] }>(`${this.base}?page=1&pageSize=100`);
  }
  create(data: Omit<Client, 'id'>) {
    return this.http.post(this.base, data);
  }
  update(id: number, data: Omit<Client, 'id'>) {
    return this.http.put(`${this.base}/${id}`, data);
  }
  remove(id: number) {
    return this.http.delete(`${this.base}/${id}`);
  }
}
