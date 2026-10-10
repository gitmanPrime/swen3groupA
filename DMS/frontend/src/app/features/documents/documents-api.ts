import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

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
}
