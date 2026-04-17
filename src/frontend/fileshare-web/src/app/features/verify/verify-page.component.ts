import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { FilesApiService } from '../../core/api/files-api.service';
import { ToastService } from '../../core/services/toast.service';
import { AppIconComponent } from '../../shared/ui/app-icon.component';
import { FloatingBackgroundComponent } from '../../shared/ui/floating-background.component';

interface VerifyResult {
  verified: boolean;
  fileName: string;
  size: number;
  fileHash: string;
  blockNumber: number;
  blockHash: string;
  signature: string;
  issuedAt: string;
  expiresAt: string;
  status: string;
  downloadCount: number;
  maxDownloads: number | null;
}

@Component({
  selector: 'app-verify-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AppIconComponent, FloatingBackgroundComponent],
  templateUrl: './verify-page.component.html',
  styleUrl: './verify-page.component.scss'
})
export class VerifyPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly filesApi = inject(FilesApiService);
  private readonly toast = inject(ToastService);

  protected readonly query = signal<string>('');
  protected readonly isLoading = signal<boolean>(false);
  protected readonly result = signal<VerifyResult | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly computedFileHash = signal<string | null>(null);
  protected readonly comparingFile = signal<boolean>(false);
  protected readonly copied = signal<string | null>(null);

  protected readonly integrityStatus = computed<'match' | 'mismatch' | 'idle'>(() => {
    const fileHash = this.computedFileHash();
    const verified = this.result()?.fileHash;
    if (!fileHash || !verified) return 'idle';
    return fileHash.toLowerCase() === verified.toLowerCase() ? 'match' : 'mismatch';
  });

  protected readonly daysSinceIssued = computed(() => {
    const issued = this.result()?.issuedAt;
    if (!issued) return null;
    const diff = Date.now() - new Date(issued).getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    if (days < 1) {
      const hours = Math.floor(diff / (1000 * 60 * 60));
      return hours < 1 ? 'minutes ago' : `${hours}h ago`;
    }
    return `${days} day${days > 1 ? 's' : ''} ago`;
  });

  ngOnInit(): void {
    const hash = this.route.snapshot.paramMap.get('hash');
    if (hash) {
      this.query.set(hash);
      this.lookup();
    }
  }

  protected submit(): void {
    const value = this.query().trim();
    if (!value) {
      this.toast.warning('Empty input', 'Paste a SHA-256 hash or hash prefix to verify.');
      return;
    }
    this.router.navigate(['/verify', value.toLowerCase()]);
    this.lookup();
  }

  protected lookup(): void {
    const value = this.query().trim();
    if (value.length < 8) {
      this.error.set('Hash prefix must be at least 8 characters.');
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);
    this.result.set(null);
    this.computedFileHash.set(null);

    this.filesApi.verifyProof(value)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.result.set(response);
          this.toast.success('Proof verified', `${response.fileName} is in the ledger.`);
        },
        error: (err) => {
          const detail = err?.error?.detail ?? err?.error?.title ?? 'No record found for this hash.';
          this.error.set(detail);
          this.toast.error('Proof not found', detail);
        }
      });
  }

  protected async onFileCompare(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.comparingFile.set(true);
    try {
      const buffer = await file.arrayBuffer();
      const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
      const hex = Array.from(new Uint8Array(hashBuffer))
        .map((b) => b.toString(16).padStart(2, '0'))
        .join('');
      this.computedFileHash.set(hex);

      if (this.result()?.fileHash?.toLowerCase() === hex.toLowerCase()) {
        this.toast.success('Integrity verified', 'Local file matches the ledger hash exactly.');
      } else {
        this.toast.error('Hash mismatch', 'The file you selected is different from the one in the ledger.');
      }
    } catch {
      this.toast.error('Hash failed', 'Could not compute the SHA-256 of this file.');
    } finally {
      this.comparingFile.set(false);
      input.value = '';
    }
  }

  protected copy(value: string, label: string): void {
    navigator.clipboard.writeText(value).then(() => {
      this.copied.set(label);
      setTimeout(() => this.copied.set(null), 1600);
    });
  }

  protected formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  protected truncate(value: string, head: number, tail: number): string {
    if (!value || value.length <= head + tail + 3) return value;
    return `${value.slice(0, head)}...${value.slice(-tail)}`;
  }
}
