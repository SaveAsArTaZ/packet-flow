import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface ApiOptions {
  headers?: Record<string, string>;
  params?: Record<string, string | number | boolean>;
  withCredentials?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  /** Base URL for the auth API — configurable per environment */
  private baseUrl = 'http://localhost:5159/api';

  private buildHeaders(opts?: ApiOptions): HttpHeaders {
    let headers = new HttpHeaders();
    headers = headers.set('Content-Type', 'application/json');
    if (opts?.headers) {
      Object.entries(opts.headers).forEach(([k, v]) => {
        headers = headers.set(k, v);
      });
    }
    return headers;
  }

  private buildParams(opts?: ApiOptions): HttpParams | undefined {
    if (!opts?.params) return undefined;
    let params = new HttpParams();
    Object.entries(opts.params).forEach(([k, v]) => {
      params = params.set(k, String(v));
    });
    return params;
  }

  get<T>(path: string, opts?: ApiOptions): Observable<T> {
    return this.http
      .get<T>(`${this.baseUrl}${path}`, {
        headers: this.buildHeaders(opts),
        params: this.buildParams(opts),
        withCredentials: opts?.withCredentials ?? true,
      })
      .pipe(catchError((e) => this.handleError(e)));
  }

  post<T>(path: string, body: unknown, opts?: ApiOptions): Observable<T> {
    return this.http
      .post<T>(`${this.baseUrl}${path}`, body, {
        headers: this.buildHeaders(opts),
        params: this.buildParams(opts),
        withCredentials: opts?.withCredentials ?? true,
      })
      .pipe(catchError((e) => this.handleError(e)));
  }

  put<T>(path: string, body: unknown, opts?: ApiOptions): Observable<T> {
    return this.http
      .put<T>(`${this.baseUrl}${path}`, body, {
        headers: this.buildHeaders(opts),
        params: this.buildParams(opts),
        withCredentials: opts?.withCredentials ?? true,
      })
      .pipe(catchError((e) => this.handleError(e)));
  }

  delete<T>(path: string, opts?: ApiOptions): Observable<T> {
    return this.http
      .delete<T>(`${this.baseUrl}${path}`, {
        headers: this.buildHeaders(opts),
        params: this.buildParams(opts),
        withCredentials: opts?.withCredentials ?? true,
      })
      .pipe(catchError((e) => this.handleError(e)));
  }

  private handleError(err: HttpErrorResponse): Observable<never> {
    let message = 'An unexpected error occurred';
    if (err.error?.message) {
      message = err.error.message;
    } else if (err.error?.title) {
      message = err.error.title;
    } else if (typeof err.error === 'string') {
      message = err.error;
    } else if (err.status === 0) {
      message = 'Cannot reach the server. Is it running?';
    }
    return throwError(() => new Error(message));
  }
}
