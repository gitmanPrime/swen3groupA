import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CollectionDto, CollectionService } from '../../../shared/services/collection.service';

@Component({
  selector: 'app-collections',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './collections.html',
})
export class Collections implements OnInit {
  private readonly api = inject(CollectionService);

  readonly collections = signal<CollectionDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly success = signal('');

  readonly newName = signal('');
  readonly newDescription = signal('');
  readonly creating = signal(false);

  readonly editingId = signal<string | null>(null);
  readonly editName = signal('');

  ngOnInit(): void {
    this.loadCollections();
  }

  loadCollections(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.list().subscribe({
      next: (data) => {
        this.collections.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load collections.');
        this.loading.set(false);
      },
    });
  }

  createCollection(event: Event): void {
    event.preventDefault();
    const name = this.newName().trim();
    if (!name) return;

    this.creating.set(true);
    this.error.set('');
    this.success.set('');

    this.api.create({ name, description: this.newDescription().trim() || undefined }).subscribe({
      next: (created) => {
        this.collections.update((list) => [...list, created]);
        this.newName.set('');
        this.newDescription.set('');
        this.creating.set(false);
        this.success.set(`Collection "${created.name}" created successfully.`);
      },
      error: () => {
        this.error.set('Could not create collection. Name might be invalid or already taken.');
        this.creating.set(false);
      },
    });
  }

  startEditing(collection: CollectionDto): void {
    this.editingId.set(collection.id);
    this.editName.set(collection.name);
  }

  cancelEditing(): void {
    this.editingId.set(null);
    this.editName.set('');
  }

  saveRename(id: string): void {
    const name = this.editName().trim();
    if (!name) return;

    this.error.set('');
    this.api.rename(id, { name }).subscribe({
      next: (updated) => {
        this.collections.update((list) =>
          list.map((c) => (c.id === id ? updated : c))
        );
        this.cancelEditing();
        this.success.set('Collection renamed successfully.');
      },
      error: () => {
        this.error.set('Could not rename collection.');
      },
    });
  }

  deleteCollection(id: string, name: string, documentCount: number): void {
    if (documentCount > 0) {
      this.error.set(`Collection "${name}" is not empty and cannot be deleted.`);
      return;
    }

    if (!confirm(`Are you sure you want to delete the empty collection "${name}"?`)) {
      return;
    }

    this.error.set('');
    this.api.delete(id).subscribe({
      next: () => {
        this.collections.update((list) => list.filter((c) => c.id !== id));
        this.success.set(`Collection "${name}" deleted.`);
      },
      error: (err) => {
        if (err.status === 409) {
          this.error.set(`Collection "${name}" contains documents and cannot be deleted.`);
        } else {
          this.error.set('Could not delete collection.');
        }
      },
    });
  }
}