import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { FilesApiService } from '../../core/api/files-api.service';
import {
  FileAvailabilityResponse,
  FileMetadataResponse
} from '../../shared/models/file-contracts';
import { AppIconComponent } from '../../shared/ui/app-icon.component';
import { FloatingBackgroundComponent } from '../../shared/ui/floating-background.component';
import { ShareAnimationComponent } from '../../shared/ui/share-animation.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

@Component({
  selector: 'app-public-download-page',
  imports: [CommonModule, RouterLink, AppIconComponent, StatusBadgeComponent, ShareAnimationComponent, FloatingBackgroundComponent],
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
  protected readonly revealImagePreview = signal(false);
  protected readonly statusLabel = computed(() => this.resolveStatusLabel());
  protected readonly statusTone = computed<'available' | 'warning' | 'danger'>(() => this.resolveStatusTone());
  protected readonly expirationDisplay = computed(() => this.buildExpirationLabel());
  protected readonly downloadsDisplay = computed(() => this.buildDownloadsLabel());
  protected readonly downloadUrl = computed(() => this.filesApi.getDownloadUrl(this.token()));
  protected readonly canDownload = computed(() => Boolean(this.availability()?.available));
  protected readonly fileIcon = computed(() => this.getFileIconName());
  protected readonly imagePreviewPossible = computed(() => {
    const type = this.metadata()?.contentType?.toLowerCase() ?? '';
    return type.startsWith('image/');
  });

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

  protected showImagePreview(): void {
    this.revealImagePreview.set(true);
  }

  protected formatFileSize(bytes: number | undefined): string {
    if (!bytes || bytes < 0) {
      return '-';
    }
    if (bytes < 1024) {
      return `${bytes} B`;
    }
    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }
    if (bytes < 1024 * 1024 * 1024) {
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
  }

  private resolveStatusLabel(): string {
    const availability = this.availability();
    if (!availability) {
      return 'Unavailable';
    }

    if (availability.available) {
      return 'Available';
    }

    const status = `${availability.status} ${availability.reason ?? ''}`.toLowerCase();
    if (status.includes('limit')) {
      return 'Download limit reached';
    }
    if (status.includes('expired')) {
      return 'Expired';
    }
    return 'Unavailable';
  }

  private resolveStatusTone(): 'available' | 'warning' | 'danger' {
    const statusLabel = this.statusLabel();
    if (statusLabel === 'Available') {
      return 'available';
    }
    if (statusLabel === 'Download limit reached') {
      return 'warning';
    }
    return 'danger';
  }

  private buildExpirationLabel(): string {
    const rawExpiresAt = this.availability()?.expiresAt ?? this.metadata()?.expiresAt;
    if (!rawExpiresAt) {
      return 'No expiration date';
    }

    const expiresAt = new Date(rawExpiresAt);
    if (Number.isNaN(expiresAt.getTime())) {
      return 'Expiration date unavailable';
    }

    const now = Date.now();
    const diffMs = expiresAt.getTime() - now;
    const diffMinutes = Math.round(diffMs / (1000 * 60));
    const absMinutes = Math.abs(diffMinutes);
    const exact = expiresAt.toLocaleString();

    if (absMinutes < 60) {
      const minutes = Math.max(1, absMinutes);
      return diffMinutes >= 0 ? `Expires in ${minutes} min (${exact})` : `Expired ${minutes} min ago (${exact})`;
    }

    const diffHours = Math.round(diffMinutes / 60);
    const absHours = Math.abs(diffHours);
    if (absHours < 48) {
      return diffHours >= 0 ? `Expires in ${absHours} hour${absHours > 1 ? 's' : ''} (${exact})` : `Expired ${absHours} hour${absHours > 1 ? 's' : ''} ago (${exact})`;
    }

    const diffDays = Math.round(diffHours / 24);
    const absDays = Math.abs(diffDays);
    return diffDays >= 0 ? `Expires in ${absDays} day${absDays > 1 ? 's' : ''} (${exact})` : `Expired ${absDays} day${absDays > 1 ? 's' : ''} ago (${exact})`;
  }

  private buildDownloadsLabel(): string {
    const used = this.availability()?.downloadCount ?? this.metadata()?.downloadCount ?? 0;
    const limit = this.availability()?.maxDownloads ?? this.metadata()?.maxDownloads;

    if (limit === null || limit === undefined) {
      return `${used} downloads used (no limit)`;
    }

    return `${used} of ${limit} downloads used`;
  }

  private getFileIconName(): string {
    const type = this.metadata()?.contentType?.toLowerCase() ?? '';
    if (type.startsWith('image/')) {
      return 'image';
    }
    if (type.includes('pdf')) {
      return 'pdf';
    }
    if (type.includes('zip') || type.includes('archive')) {
      return 'archive';
    }
    return 'file';
  }
}
