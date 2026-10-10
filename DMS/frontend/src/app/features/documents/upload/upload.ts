import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DocumentsApi } from '../documents-api';

/** Upload form for adding a new PDF document to the archive. */
@Component({
  selector: 'app-document-upload',
  imports: [RouterLink],
  templateUrl: './upload.html',
})
export class Upload {
  private readonly api = inject(DocumentsApi);
  readonly uploading = signal(false);
  readonly error = signal('');
  readonly success = signal('');

  upload(event: Event, fileInput: HTMLInputElement, description: string) {
    event.preventDefault();
    if (this.uploading()) return;
    this.error.set('');
    this.success.set('');

    // Client-side validation: must be a non-empty PDF. The REST API still
    // re-validates this server-side, this just avoids an unnecessary
    // round trip for an obviously invalid file.
    const file = fileInput.files?.[0];
    if (!file || !file.name.toLowerCase().endsWith('.pdf') ||
        (file.type && file.type !== 'application/pdf') || file.size === 0) {
      this.error.set('Please select a non-empty PDF file.');
      return;
    }

    this.uploading.set(true);
    this.api.upload(file, description).subscribe({
      next: (document) => {
        this.success.set(`Uploaded ${document.fileName} successfully.`);
        this.uploading.set(false);
        (fileInput.form as HTMLFormElement).reset();
      },
      error: () => {
        this.error.set('Upload failed. Please try again.');
        this.uploading.set(false);
      },
    });
  }
}
