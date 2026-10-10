import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Tag, CreateTagRequest } from '../models/tag.model';

/** Thin wrapper around the /api/tags and /api/documents/{id}/tags endpoints. */
@Injectable({
  providedIn: 'root'
})
export class TagService {
  private readonly http = inject(HttpClient);
  private readonly documentsUrl = '/api/documents';
  private readonly tagsUrl = '/api/tags';

  // tags currently assigned to a specific document.
  getDocumentTags(documentId: string): Observable<Tag[]> {
    return this.http.get<Tag[]>(`/api/documents/${documentId}/tags`);
  }

  // all tags that exist in the system, assigned or not.
  getTags(): Observable<Tag[]> {
    return this.http.get<Tag[]>(this.tagsUrl);
  }

  // creates a new, unassigned tag
  createTag(request: CreateTagRequest): Observable<Tag> {
    return this.http.post<Tag>(this.tagsUrl, request);
  }

  // assigns an existing tag to a document
  assignTagToDocument(documentId: string, tagId: string) {
    return this.http.post(
      `${this.documentsUrl}/${documentId}/tags/${tagId}`,
      {}
    );
  }

  // removes a tag from a document (the tag itself still exists afterwards)
  removeTagFromDocument(documentId: string, tagId: string) {
    return this.http.delete(
      `${this.documentsUrl}/${documentId}/tags/${tagId}`
    );
  }
}
