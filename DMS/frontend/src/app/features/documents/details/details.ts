import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Document, DocumentsApi } from '../documents-api';
import { DocumentTags } from '../document-tags/document-tags';

/**
 * Detail page for a single document: shows its metadata, lets the user
 * edit its description, manage its tags, or delete it.
 */
@Component({
  selector: 'app-document-details',
  imports: [DatePipe, NgClass, RouterLink, DocumentTags],
  templateUrl: './details.html',
})
export class Details implements OnInit {
  private readonly api = inject(DocumentsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly document = signal<Document | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly actionError = signal('');
  readonly success = signal('');
  readonly confirmingDelete = signal(false);

  ngOnInit() {
    // The document id comes from the route (/documents/:id).
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

  saveDescription(event: Event, description: string) {
    event.preventDefault();
    const document = this.document();
    if (!document || this.busy()) return;
    this.busy.set(true);
    this.actionError.set('');
    this.success.set('');
    this.api.updateDescription(document.id, description).subscribe({
      next: (updated) => {
        this.document.set(updated);
        this.success.set('Description saved.');
        this.busy.set(false);
      },
      error: () => {
        this.actionError.set('Could not save the description. Please try again.');
        this.busy.set(false);
      },
    });
  }

  deleteDocument() {
    const document = this.document();
    if (!document || this.busy() || !this.confirmingDelete()) return;
    this.busy.set(true);
    this.actionError.set('');
    this.success.set('');
    this.api.delete(document.id).subscribe({
      next: () => { void this.router.navigate(['/documents']); },
      error: () => {
        this.actionError.set('Could not delete the document. Please try again.');
        this.busy.set(false);
      },
    });
  }

  // formats a byte count as a short, human-readable size (e.g. "1.4 MB")
  formatBytes(bytes: number): string {
    if (!bytes || bytes <= 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB'];
    const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    const value = bytes / Math.pow(1024, exponent);
    return `${exponent === 0 ? value : value.toFixed(1)} ${units[exponent]}`;
  }
}
