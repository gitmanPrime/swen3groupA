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
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly api = inject(DocumentsApi);

  readonly documents = signal<Document[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    // fetch the document list once when the component is created
    this.api.list().subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load documents. Please try again later.');
        this.loading.set(false);
      },
    });
  }
}
