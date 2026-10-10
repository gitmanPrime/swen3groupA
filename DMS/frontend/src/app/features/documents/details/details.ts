import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Document, DocumentsApi } from '../documents-api';

@Component({
  selector: 'app-document-details',
  imports: [DatePipe, RouterLink],
  templateUrl: './details.html',
})
export class Details implements OnInit {
  private readonly api = inject(DocumentsApi);
  private readonly route = inject(ActivatedRoute);
  readonly document = signal<Document | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    this.api.getById(this.route.snapshot.paramMap.get('id')!).subscribe({
      next: (document) => {
        this.document.set(document);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load document details. The document may no longer exist.');
        this.loading.set(false);
      },
    });
  }
}
