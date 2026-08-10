import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Species {
  id: number;
  name: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class SpeciesService {
  private base = 'http://localhost:5200/api/species'; // adjust port
  constructor(private http: HttpClient) {}
  list(): Observable<{ items: Species[] }> {
    return this.http.get<{ items: Species[] }>(`${this.base}?page=1&pageSize=50`);
  }
  create(data: { name: string; isActive: boolean }) {
    return this.http.post(this.base, data);
  }
}
