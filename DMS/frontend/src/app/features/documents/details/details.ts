import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Document, DocumentsApi } from '../documents-api';
import { DocumentTags } from '../document-tags/document-tags';
import { CollectionDto, CollectionService } from '../../../shared/services/collection.service';

@Component({
  selector: 'app-document-details',
  imports: [DatePipe, NgClass, RouterLink, FormsModule, DocumentTags],
  templateUrl: './details.html',
})
export class Details implements OnInit {
  private readonly api = inject(DocumentsApi);
  private readonly collectionService = inject(CollectionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly document = signal<Document | null>(null);
  readonly allCollections = signal<CollectionDto[]>([]);
  readonly selectedCollectionId = signal<string>('');
  
  readonly loading = signal(true);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly actionError = signal('');
  readonly success = signal('');
  readonly confirmingDelete = signal(false);

  ngOnInit() {
    const documentId = this.route.snapshot.paramMap.get('id')!;
    
    this.api.getById(documentId).subscribe({
      next: (document) => {
        this.document.set(document);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load document details.');
        this.loading.set(false);
      },
    });

    this.loadCollections();
  }

  loadCollections() {
    this.collectionService.list().subscribe({
      next: (cols) => this.allCollections.set(cols),
      error: () => console.error('Could not load collections')
    });
  }

  addToCollection() {
    const doc = this.document();
    const colId = this.selectedCollectionId();
    if (!doc || !colId || this.busy()) return;

    this.busy.set(true);
    this.actionError.set('');
    this.success.set('');

    const currentCollectionIds = (doc as any).collectionIds || [];
    const updatedCollectionIds = Array.from(new Set([...currentCollectionIds, colId]));

    this.api.updateCollections(doc.id, updatedCollectionIds).subscribe({
      next: (updated) => {
        this.document.set(updated);
        this.busy.set(false);
        this.selectedCollectionId.set('');
        this.success.set('Document added to collection successfully.');
      },
      error: () => {
        this.actionError.set('Could not add document to collection.');
        this.busy.set(false);
      },
    });
  }

  removeFromCollection(collectionId: string) {
    const doc = this.document();
    if (!doc || this.busy()) return;

    this.busy.set(true);
    this.actionError.set('');
    this.success.set('');

    const currentCollectionIds = (doc as any).collectionIds || [];
    const updatedCollectionIds = currentCollectionIds.filter((id: string) => id !== collectionId);

    this.api.updateCollections(doc.id, updatedCollectionIds).subscribe({
      next: (updated) => {
        this.document.set(updated);
        this.busy.set(false);
        this.success.set('Document removed from collection.');
      },
      error: () => {
        this.actionError.set('Could not remove document from collection.');
        this.busy.set(false);
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
        this.actionError.set('Could not save the description.');
        this.busy.set(false);
      },
    });
  }

  deleteDocument() {
    const document = this.document();
    if (!document || this.busy() || !this.confirmingDelete()) return;
    this.busy.set(true);
    this.api.delete(document.id).subscribe({
      next: () => { void this.router.navigate(['/documents']); },
      error: () => {
        this.actionError.set('Could not delete the document.');
        this.busy.set(false);
      },
    });
  }

  formatBytes(bytes: number): string {
    if (!bytes || bytes <= 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB'];
    const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    const value = bytes / Math.pow(1024, exponent);
    return `${exponent === 0 ? value : value.toFixed(1)} ${units[exponent]}`;
  }
}