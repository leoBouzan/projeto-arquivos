import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { FilesApiService } from '../../core/api/files-api.service';
import {
  FileAvailabilityResponse,
  FileMetadataResponse
} from '../../shared/models/file-contracts';

@Component({
  selector: 'app-public-download-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './public-download-page.component.html',
  styleUrl: './public-download-page.component.scss'
})
export class PublicDownloadPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly filesApi = inject(FilesApiService);

  protected readonly token = signal('');
  protected readonly metadata = signal<FileMetadataResponse | null>(null);
  protected readonly availability = signal<FileAvailabilityResponse | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token') ?? '';
    this.token.set(token);

    if (!token) {
      this.error.set('The link token is missing.');
      this.isLoading.set(false);
      return;
    }

    forkJoin({
      metadata: this.filesApi.getMetadata(token),
      availability: this.filesApi.getAvailability(token)
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ metadata, availability }) => {
          this.metadata.set(metadata);
          this.availability.set(availability);
        },
        error: (error) => {
          this.error.set(error?.error?.detail ?? 'The public link could not be resolved.');
        }
      });
  }

  protected download(): void {
    window.location.href = this.filesApi.getDownloadUrl(this.token());
  }
}
