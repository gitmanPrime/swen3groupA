import { Component, OnInit, inject, input, signal, computed, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Tag } from '../../../shared/models/tag.model';
import { TagService } from '../../../shared/services/tag.service';

/**
 * Manages the tags of a single document: shows the tags currently
 * assigned, lets the user assign an existing tag or create a new one,
 * and remove a tag again.
 */
@Component({
  selector: 'app-document-tags',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './document-tags.html'
})
export class DocumentTags implements OnInit {
  private readonly tagService = inject(TagService);

  documentId = input.required<string>();
  tagIds = input<string[]>([]);

  tags = signal<Tag[]>([]);
  selectedTagId = signal('');
  newTagName = signal('');
  errorMessage = signal('');
  successMessage = signal('');

  private readonly assignedTagIds = signal<string[]>([]);

  constructor() {
    // re-sync whenever the parent passes a new tagIds value (e.g. after navigating to a different document).
    effect(() => {
      this.assignedTagIds.set(this.tagIds());
    });
  }

  assignedTags = computed(() => {
    const ids = this.assignedTagIds();
    return this.tags().filter(t => ids.includes(t.id));
  });

  ngOnInit(): void {
    this.loadAllTags();
  }

  loadAllTags(): void {
    this.tagService.getTags().subscribe({
      next: tags => this.tags.set(tags),
      error: () => this.errorMessage.set('Could not load tags.')
    });
  }

  createTag(): void {
    const name = this.newTagName().trim();
    if (!name) return;

    this.tagService.createTag({ name }).subscribe({
      next: tag => {
        this.tags.update(tags => [...tags, tag]);
        this.newTagName.set('');
        this.errorMessage.set('');
        this.successMessage.set('Tag created.');
      },
      error: () => this.errorMessage.set('Could not create the tag.')
    });
  }

  assignTag(): void {
    const tagId = this.selectedTagId();
    if (!tagId) return;

    if (this.assignedTags().some(tag => tag.id === tagId)) {
      this.errorMessage.set('This tag is already assigned.');
      return;
    }

    this.tagService.assignTagToDocument(this.documentId(), tagId).subscribe({
      next: () => {
        // update local state instead of reloading the page
        this.assignedTagIds.update(ids => [...ids, tagId]);
        this.selectedTagId.set('');
        this.errorMessage.set('');
        this.successMessage.set('Tag assigned to the document.');
      },
      error: () => this.errorMessage.set('Could not assign the tag.')
    });
  }

  removeTag(tag: Tag): void {
    this.tagService.removeTagFromDocument(this.documentId(), tag.id).subscribe({
      next: () => {
        this.assignedTagIds.update(ids => ids.filter(id => id !== tag.id));
        this.errorMessage.set('');
        this.successMessage.set('Tag removed.');
      },
      error: () => this.errorMessage.set('Could not remove the tag.')
    });
  }
}
