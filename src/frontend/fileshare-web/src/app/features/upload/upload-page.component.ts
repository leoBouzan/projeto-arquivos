import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { FilesApiService } from '../../core/api/files-api.service';
import { UploadFileResponse } from '../../shared/models/file-contracts';

@Component({
  selector: 'app-upload-page',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './upload-page.component.html',
  styleUrl: './upload-page.component.scss'
})
export class UploadPageComponent {
  private readonly filesApi = inject(FilesApiService);

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly expiresAt = signal(this.buildDefaultExpiration());
  protected readonly maxDownloads = signal<number | null>(5);
  protected readonly isSubmitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly response = signal<UploadFileResponse | null>(null);
  protected readonly downloadPreviewPath = computed(() => {
    const response = this.response();
    return response ? `/files/${response.accessToken}` : null;
  });

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  protected onMaxDownloadsChange(value: string): void {
    const parsed = Number(value);
    this.maxDownloads.set(Number.isFinite(parsed) && parsed > 0 ? parsed : null);
  }

  protected submit(): void {
    const file = this.selectedFile();
    if (!file) {
      this.error.set('Select a file before generating a temporary link.');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);
    this.response.set(null);

    const formData = new FormData();
    formData.append('file', file);
    formData.append('expiresAt', new Date(this.expiresAt()).toISOString());

    const maxDownloads = this.maxDownloads();
    if (maxDownloads !== null) {
      formData.append('maxDownloads', maxDownloads.toString());
    }

    this.filesApi
      .upload(formData)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response) => {
          this.response.set(response);
        },
        error: (error) => {
          this.error.set(error?.error?.detail ?? 'The upload could not be completed.');
        }
      });
  }

  private buildDefaultExpiration(): string {
    const future = new Date(Date.now() + 24 * 60 * 60 * 1000);
    future.setMinutes(future.getMinutes() - future.getTimezoneOffset());
    return future.toISOString().slice(0, 16);
  }
}
