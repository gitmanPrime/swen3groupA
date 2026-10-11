import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';

export interface Document {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  description: string | null;
  status: string;
  collectionIds: string[];
  tagIds: string[];
}

@Injectable({ providedIn: 'root' })
export class DocumentsApi {
  private readonly http = inject(HttpClient);

  list() {
    return this.http.get<Document[]>('/api/documents');
  }

  getById(id: string) {
    return this.http.get<Document>(`/api/documents/${encodeURIComponent(id)}`);
  }

  upload(file: File, description: string) {
    const form = new FormData();
    form.append('file', file);
    if (description.trim()) form.append('description', description.trim());
    return this.http.post<Document>('/api/documents', form);
  }

  updateDescription(id: string, description: string) {
    return this.http.patch<Document>(`/api/documents/${encodeURIComponent(id)}`, { description });
  }
  
  updateCollections(documentId: string, collectionIds: string[]): Observable<Document> {
    return this.http.patch<Document>(`/api/documents/${documentId}`, { collectionIds });
  }

  delete(id: string) {
    return this.http.delete<void>(`/api/documents/${encodeURIComponent(id)}`);
  }
}
