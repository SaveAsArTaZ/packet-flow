import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';

export interface TemplateSummary {
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  isPublic: boolean;
  tags: string[];
  thumbnailUrl?: string;
  usageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface TemplateDetail extends TemplateSummary {
  topologyJson: string;
  version: number;
  clonedFromId?: string;
  metadata?: string;
}

export interface CreateTemplatePayload {
  name: string;
  description?: string;
  topologyJson: string;
  isPublic: boolean;
  tags: string[];
  thumbnailUrl?: string;
  metadata?: string;
}

export interface UpdateTemplatePayload {
  name?: string;
  description?: string;
  topologyJson?: string;
  isPublic?: boolean;
  tags?: string[];
  thumbnailUrl?: string;
  metadata?: string;
}

const TEMPLATES_BASE = 'http://localhost:5024/api/templates';

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  readonly templates = signal<TemplateSummary[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal('');

  private getHeaders(): Record<string, string> {
    return this.auth.getAuthHeaders();
  }

  /** Fetch current user's templates */
  loadMyTemplates(page = 1, pageSize = 20): Observable<TemplateSummary[]> {
    this.isLoading.set(true);
    this.error.set('');
    return this.api
      .get<TemplateSummary[]>(
        `${TEMPLATES_BASE}/my?page=${page}&pageSize=${pageSize}`,
        { headers: this.getHeaders() },
      )
      .pipe(
        tap({
          next: (data) => {
            this.templates.set(data);
            this.isLoading.set(false);
          },
          error: (err: Error) => {
            this.error.set(err.message);
            this.isLoading.set(false);
          },
        }),
      );
  }

  /** Fetch public templates */
  loadPublicTemplates(page = 1, pageSize = 20): Observable<TemplateSummary[]> {
    this.isLoading.set(true);
    this.error.set('');
    return this.api
      .get<TemplateSummary[]>(`${TEMPLATES_BASE}/public?page=${page}&pageSize=${pageSize}`)
      .pipe(
        tap({
          next: (data) => {
            this.templates.set(data);
            this.isLoading.set(false);
          },
          error: (err: Error) => {
            this.error.set(err.message);
            this.isLoading.set(false);
          },
        }),
      );
  }

  /** Search templates */
  search(query: string, page = 1, pageSize = 20): Observable<TemplateSummary[]> {
    this.isLoading.set(true);
    this.error.set('');
    return this.api
      .get<TemplateSummary[]>(
        `${TEMPLATES_BASE}/search?q=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`,
      )
      .pipe(
        tap({
          next: (data) => {
            this.templates.set(data);
            this.isLoading.set(false);
          },
          error: (err: Error) => {
            this.error.set(err.message);
            this.isLoading.set(false);
          },
        }),
      );
  }

  /** Get single template by ID */
  getById(id: string): Observable<TemplateDetail> {
    return this.api.get<TemplateDetail>(`${TEMPLATES_BASE}/${id}`, {
      headers: this.getHeaders(),
    });
  }

  /** Create a new template */
  create(payload: CreateTemplatePayload): Observable<TemplateDetail> {
    return this.api.post<TemplateDetail>(TEMPLATES_BASE, payload, {
      headers: this.getHeaders(),
    });
  }

  /** Update a template */
  update(id: string, payload: UpdateTemplatePayload): Observable<TemplateDetail> {
    return this.api.put<TemplateDetail>(`${TEMPLATES_BASE}/${id}`, payload, {
      headers: this.getHeaders(),
    });
  }

  /** Delete a template */
  delete(id: string): Observable<void> {
    return this.api.delete<void>(`${TEMPLATES_BASE}/${id}`, {
      headers: this.getHeaders(),
    });
  }

  /** Clone a template */
  clone(id: string, newName?: string): Observable<TemplateDetail> {
    return this.api.post<TemplateDetail>(
      `${TEMPLATES_BASE}/${id}/clone`,
      newName ? { newName } : {},
      { headers: this.getHeaders() },
    );
  }
}
