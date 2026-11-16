import { Component, inject, OnInit, signal } from '@angular/core';
import { Document, DocumentsApi } from '../documents-api';
import { RouterLink } from '@angular/router';

/**
 * Landing page of the DMS frontend: lists every document known to the
 * REST API and links each row to its detail page.
 */
@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  private readonly api = inject(DocumentsApi);

  readonly documents = signal<Document[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly searchTerm = signal('');

  ngOnInit() {
    this.loadDocuments();
  }

  loadDocuments(query = '') {
    const term = query.trim();
    this.searchTerm.set(term);
    this.loading.set(true);
    this.error.set('');

    const request = term ? this.api.search(term) : this.api.list();
    request.subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(term
          ? 'Could not search documents. Please try again.'
          : 'Could not load documents. Please try again later.');
        this.loading.set(false);
      },
    });
  }
}
