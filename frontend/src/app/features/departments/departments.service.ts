import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../config';

export interface Department {
  id: number;
  name: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class DepartmentsService {
  private base = `${environment.apiBase}/departments`;
  constructor(private http: HttpClient) {}

  list() {
    return this.http.get<{ items: Department[] }>(`${this.base}?page=1&pageSize=100`);
  }
  create(data: { name: string; isActive: boolean }) {
    return this.http.post(this.base, data);
  }
  update(id: number, data: { name: string; isActive: boolean }) {
    return this.http.put(`${this.base}/${id}`, data);
  }
  remove(id: number) {
    return this.http.delete(`${this.base}/${id}`);
  }
}
