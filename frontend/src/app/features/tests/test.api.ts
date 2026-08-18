import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API = 'http://localhost:5035/api/tests';

export interface Test {
  id: number;
  name: string;
  code?: string;
  departmentId: number;
  isActive: boolean;
}
export interface TestParam {
  id: number;
  testId: number;
  name: string;
  unitId?: number;
  sortOrder: number;
  isActive: boolean;
}
export interface TestRange {
  id: number;
  testParameterId: number;
  speciesId: number;
  ageFromDays?: number;
  ageToDays?: number;
  low?: number;
  high?: number;
  note?: string;
  isActive: boolean;
}
export interface Paged<T> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class TestApi {
  private http = inject(HttpClient);

  list(search = '', departmentId?: number, page = 1, pageSize = 20): Observable<Paged<Test>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<Paged<Test>>(API, { params });
  }
  create(payload: Omit<Test, 'id'>) {
    return this.http.post<Test>(API, payload);
  }
  get(id: number) {
    return this.http.get<Test>(`${API}/${id}`);
  }
  update(id: number, payload: Omit<Test, 'id'>) {
    return this.http.put<Test>(`${API}/${id}`, payload);
  }
  toggleStatus(id: number, isActive: boolean) {
    return this.http.patch(`${API}/${id}/status`, isActive);
  }

  // parameters
  listParams(testId: number) {
    return this.http.get<TestParam[]>(`${API}/${testId}/parameters`);
  }
  createParam(testId: number, payload: Omit<TestParam, 'id'>) {
    return this.http.post<TestParam>(`${API}/${testId}/parameters`, payload);
  }
  updateParam(testId: number, paramId: number, payload: Omit<TestParam, 'id' | 'testId'>) {
    return this.http.put<TestParam>(`${API}/${testId}/parameters/${paramId}`, payload);
  }
  deleteParam(testId: number, paramId: number) {
    return this.http.delete(`${API}/${testId}/parameters/${paramId}`);
  }

  // ranges
  listRanges(paramId: number) {
    return this.http.get<TestRange[]>(`${API}/parameters/${paramId}/ranges`);
  }
  createRange(paramId: number, payload: Omit<TestRange, 'id' | 'testParameterId'>) {
    return this.http.post<TestRange>(`${API}/parameters/${paramId}/ranges`, {
      ...payload,
      testParameterId: paramId,
    });
  }
  updateRange(
    paramId: number,
    rangeId: number,
    payload: Omit<TestRange, 'id' | 'testParameterId'>
  ) {
    return this.http.put<TestRange>(`${API}/parameters/${paramId}/ranges/${rangeId}`, payload);
  }
  deleteRange(paramId: number, rangeId: number) {
    return this.http.delete(`${API}/parameters/${paramId}/ranges/${rangeId}`);
  }
}
