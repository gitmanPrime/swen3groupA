import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CollectionDto {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  documentCount: number;
}

export interface CreateCollectionRequest {
  name: string;
  description?: string;
}

export interface RenameCollectionRequest {
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class CollectionService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = '/api/collections';

    list(): Observable<CollectionDto[]> {
        return this.http.get<CollectionDto[]>(this.baseUrl);
    }

    getById(id: string): Observable<CollectionDto> {
        return this.http.get<CollectionDto>(`${this.baseUrl}/${id}`);
    }

    create(request: CreateCollectionRequest): Observable<CollectionDto> {
        return this.http.post<CollectionDto>(this.baseUrl, request);
    }

    rename(id: string, request: RenameCollectionRequest): Observable<CollectionDto> {
        return this.http.patch<CollectionDto>(`${this.baseUrl}/${id}`, request);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}