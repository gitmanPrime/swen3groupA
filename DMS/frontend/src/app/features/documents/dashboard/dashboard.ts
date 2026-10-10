import { Component, inject, OnInit, signal } from '@angular/core';
import { Document, DocumentsApi } from '../documents-api';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly api = inject(DocumentsApi);
  readonly documents = signal<Document[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
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
