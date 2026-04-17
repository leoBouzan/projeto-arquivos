import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { FilesApiService } from '../../core/api/files-api.service';
import { ToastService } from '../../core/services/toast.service';
import {
  FileAvailabilityResponse,
  FileMetadataResponse
} from '../../shared/models/file-contracts';
import { AppIconComponent } from '../../shared/ui/app-icon.component';
import { FloatingBackgroundComponent } from '../../shared/ui/floating-background.component';
import {
  ProofOfTransferComponent,
  ProofRecord,
  proofFromBackend
} from '../../shared/ui/proof-of-transfer.component';
import { ShareAnimationComponent } from '../../shared/ui/share-animation.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

@Component({
  selector: 'app-public-download-page',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    AppIconComponent,
    StatusBadgeComponent,
    ShareAnimationComponent,
    FloatingBackgroundComponent,
    ProofOfTransferComponent
  ],
  templateUrl: './public-download-page.component.html',
  styleUrl: './public-download-page.component.scss'
})
export class PublicDownloadPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly filesApi = inject(FilesApiService);
  private readonly toast = inject(ToastService);

  protected readonly token = signal('');
  protected readonly metadata = signal<FileMetadataResponse | null>(null);
  protected readonly availability = signal<FileAvailabilityResponse | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly revealImagePreview = signal(false);
  protected readonly proof = signal<ProofRecord | null>(null);
  protected readonly password = signal<string>('');
  protected readonly passwordError = signal<string | null>(null);
  protected readonly passwordShake = signal<boolean>(false);
  protected readonly downloading = signal<boolean>(false);
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
          this.proof.set(proofFromBackend(
            { name: metadata.fileName, size: metadata.size },
            metadata.proof
          ));
        },
        error: (error) => {
          this.error.set(error?.error?.detail ?? 'The public link could not be resolved.');
        }
      });
  }

  protected async download(): Promise<void> {
    const meta = this.metadata();
    if (this.downloading()) {
      return;
    }

    if (meta?.hasPassword && !this.password().trim()) {
      this.passwordError.set('Password is required to download this file.');
      this.triggerShake();
      this.toast.warning('Password required', 'Enter the password shared by the sender.');
      return;
    }

    this.passwordError.set(null);
    this.downloading.set(true);

    const password = meta?.hasPassword ? this.password().trim() : null;
    const url = this.filesApi.getDownloadUrl(this.token(), password);

    try {
      const response = await fetch(url);

      if (!response.ok) {
        const body = await response.json().catch(() => null);
        const code = body?.title ?? '';
        if (response.status === 400 && code.includes('password_invalid')) {
          this.passwordError.set('Incorrect password. Try again.');
          this.triggerShake();
          this.toast.error('Incorrect password', 'The password you entered does not match.');
        } else if (response.status === 400 && code.includes('password_required')) {
          this.passwordError.set('Password is required to download this file.');
          this.triggerShake();
          this.toast.warning('Password required', 'Enter the password shared by the sender.');
        } else {
          const detail = body?.detail ?? body?.title ?? `Download failed (HTTP ${response.status}).`;
          this.toast.error('Download failed', detail);
        }
        this.downloading.set(false);
        return;
      }

      const blob = await response.blob();
      const blobUrl = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = blobUrl;
      anchor.download = meta?.fileName ?? 'download';
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      setTimeout(() => URL.revokeObjectURL(blobUrl), 1500);

      this.toast.success('Download started', meta?.fileName ?? '');
      this.availability.update((a) => a ? { ...a, downloadCount: (a.downloadCount ?? 0) + 1 } : a);
      this.metadata.update((m) => m ? { ...m, downloadCount: m.downloadCount + 1 } : m);
    } catch (err) {
      this.toast.error('Network error', 'Could not reach the server. Try again.');
    } finally {
      this.downloading.set(false);
    }
  }

  private triggerShake(): void {
    this.passwordShake.set(false);
    setTimeout(() => this.passwordShake.set(true), 10);
    setTimeout(() => this.passwordShake.set(false), 520);
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
