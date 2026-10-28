import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { ApiResponse } from '../models/api';
import { ApiRequestError } from './api-request-error';

/** Thin wrapper over HttpClient that unwraps the ApiResponse envelope. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  get<T>(url: string): Observable<T> {
    return this.unwrap(this.http.get<ApiResponse<T>>(url));
  }

  put<T>(url: string, body: unknown): Observable<T> {
    return this.unwrap(this.http.put<ApiResponse<T>>(url, body));
  }

  post<T>(url: string, body: unknown): Observable<T> {
    return this.unwrap(this.http.post<ApiResponse<T>>(url, body));
  }

  private unwrap<T>(request: Observable<ApiResponse<T>>): Observable<T> {
    return request.pipe(
      map((response) => response.data as T),
      catchError((error: unknown) => throwError(() => this.toRequestError(error))),
    );
  }

  private toRequestError(error: unknown): ApiRequestError | unknown {
    if (!(error instanceof HttpErrorResponse)) {
      return error;
    }
    const body = error.error as Partial<ApiResponse<unknown>> | null;
    const errors = Array.isArray(body?.errors) ? body.errors : [];
    return new ApiRequestError(error.status, errors);
  }
}
