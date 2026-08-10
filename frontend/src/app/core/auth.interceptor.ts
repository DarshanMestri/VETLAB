// src/app/core/auth.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();
  const clinicId = auth.getSelectedClinicId();
  const branchId = auth.getSelectedBranchId();

  const headers: Record<string, string> = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (clinicId != null) headers['X-Clinic-Id'] = String(clinicId);
  if (branchId != null) headers['X-Branch-Id'] = String(branchId);

  if (Object.keys(headers).length > 0) {
    req = req.clone({ setHeaders: headers });
  }

  return next(req);
};
