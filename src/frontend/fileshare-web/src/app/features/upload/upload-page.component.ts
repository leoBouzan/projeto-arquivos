import { CommonModule } from '@angular/common';
import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { FilesApiService } from '../../core/api/files-api.service';
import { ToastService } from '../../core/services/toast.service';
import { UploadFileResponse } from '../../shared/models/file-contracts';
import { AppIconComponent } from '../../shared/ui/app-icon.component';
import { CopyFieldComponent } from '../../shared/ui/copy-field.component';
import {
  CryptoPaymentModalComponent,
  PaymentCurrency,
  PaymentPlan
} from '../../shared/ui/crypto-payment-modal.component';
import { FloatingBackgroundComponent } from '../../shared/ui/floating-background.component';
import {
  ProofOfTransferComponent,
  ProofRecord,
  proofFromBackend
} from '../../shared/ui/proof-of-transfer.component';
import { ShareAnimationComponent } from '../../shared/ui/share-animation.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

interface RecentLinkItem {
  id: string;
  accessToken: string;
  fileName: string;
  expiresAt: string;
  maxDownloads: number | null;
  createdAt: number;
  publicLink: string;
  directDownload: string;
}

const RECENT_LINKS_STORAGE_KEY = 'fileshare.recent-links.v1';
const FREE_TIER_MAX_BYTES = 50 * 1024 * 1024;
const PRO_TIER_MAX_BYTES = 5 * 1024 * 1024 * 1024;
const UNLOCKED_PLAN_STORAGE_KEY = 'fileshare.unlocked-plan.v1';

@Component({
  selector: 'app-upload-page',
  imports: [
    CommonModule,
    FormsModule,
    AppIconComponent,
    StatusBadgeComponent,
    ShareAnimationComponent,
    CopyFieldComponent,
    FloatingBackgroundComponent,
    CryptoPaymentModalComponent,
    ProofOfTransferComponent
  ],
  templateUrl: './upload-page.component.html',
  styleUrl: './upload-page.component.scss'
})
export class UploadPageComponent implements OnDestroy {
  private readonly filesApi = inject(FilesApiService);
  private readonly toast = inject(ToastService);
  private resetCopyFeedbackTimeout: ReturnType<typeof setTimeout> | null = null;
  private previewObjectUrl: string | null = null;

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly filePreviewUrl = signal<string | null>(null);
  protected readonly isDragOver = signal(false);
  protected readonly expiresAt = signal(this.buildDefaultExpiration());
  protected readonly maxDownloads = signal<number | null>(5);
  protected readonly isSubmitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly response = signal<UploadFileResponse | null>(null);
  protected readonly responseCreatedAt = signal<number | null>(null);
  protected readonly copyFeedback = signal<string | null>(null);
  protected readonly recentLinks = signal<RecentLinkItem[]>(this.loadRecentLinks());
  protected readonly unlockedPlan = signal<PaymentPlan | null>(this.loadUnlockedPlan());
  protected readonly paymentTxHash = signal<string | null>(null);
  protected readonly paymentCurrency = signal<PaymentCurrency | null>(null);
  protected readonly showPaymentModal = signal(false);
  protected readonly requiredPlan = signal<PaymentPlan>('pro');
  protected readonly proof = signal<ProofRecord | null>(null);
  protected readonly password = signal<string>('');
  protected readonly passwordEnabled = signal<boolean>(false);

  protected readonly requiresUpgrade = computed(() => {
    const file = this.selectedFile();
    if (!file) return false;
    return file.size > FREE_TIER_MAX_BYTES && !this.unlockedPlan();
  });

  protected readonly currentTier = computed<'free' | PaymentPlan>(() => this.unlockedPlan() ?? 'free');

  protected readonly tierLabel = computed(() => {
    switch (this.currentTier()) {
      case 'pro': return 'Pro';
      case 'business': return 'Business';
      default: return 'Free';
    }
  });

  protected readonly selectedSizeMb = computed(() => {
    const file = this.selectedFile();
    return file ? file.size / (1024 * 1024) : 0;
  });

  protected readonly canSubmit = computed(() =>
    Boolean(this.selectedFile()) && !this.isSubmitting() && !this.requiresUpgrade()
  );
  protected readonly selectedFileTypeLabel = computed(() => this.getFileTypeLabel(this.selectedFile()));
  protected readonly selectedFileExtension = computed(() => this.getFileExtension(this.selectedFile()));
  protected readonly selectedFileIcon = computed(() => this.getFileIconName(this.selectedFile()));
  protected readonly previewMode = computed<'image' | 'pdf' | 'file' | null>(() => {
    const file = this.selectedFile();
    if (!file) {
      return null;
    }

    if (file.type.startsWith('image/')) {
      return 'image';
    }
    if (file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')) {
      return 'pdf';
    }
    return 'file';
  });
  protected readonly downloadPreviewPath = computed(() => {
    const response = this.response();
    return response ? `/files/${response.accessToken}` : null;
  });
  protected readonly publicLink = computed(() => {
    const path = this.downloadPreviewPath();
    return path ? this.toAbsoluteUrl(path) : '';
  });
  protected readonly resultTimeLeft = computed(() => {
    const response = this.response();
    return response ? this.formatTimeLeft(response.expiresAt) : '';
  });
  protected readonly resultValidityPercent = computed(() => {
    const response = this.response();
    const createdAt = this.responseCreatedAt();
    if (!response || !createdAt) {
      return 0;
    }

    const expiresAt = new Date(response.expiresAt).getTime();
    const total = expiresAt - createdAt;
    if (total <= 0) {
      return 0;
    }

    const remaining = Math.max(0, expiresAt - Date.now());
    return Math.round((remaining / total) * 100);
  });
  protected readonly recentLinksPreview = computed(() => this.recentLinks().slice(0, 3));

  protected readonly expirationPresets = [
    { label: '1 hour', hours: 1 },
    { label: '24 hours', hours: 24 },
    { label: '3 days', hours: 24 * 3 },
    { label: '7 days', hours: 24 * 7 }
  ] as const;
  protected readonly downloadPresets = [1, 3, 5, 10] as const;

  ngOnDestroy(): void {
    this.cleanupObjectUrl();
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.setFile(input.files?.[0] ?? null);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    this.setFile(event.dataTransfer?.files?.[0] ?? null);
  }

  protected removeSelectedFile(fileInput: HTMLInputElement): void {
    fileInput.value = '';
    this.setFile(null);
  }

  protected applyExpirationPreset(hours: number): void {
    const future = new Date(Date.now() + hours * 60 * 60 * 1000);
    future.setMinutes(future.getMinutes() - future.getTimezoneOffset());
    this.expiresAt.set(future.toISOString().slice(0, 16));
  }

  protected applyDownloadPreset(value: number): void {
    this.maxDownloads.set(value);
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

    if (file.size > FREE_TIER_MAX_BYTES && !this.unlockedPlan()) {
      this.openPaymentModal();
      return;
    }

    if (file.size > PRO_TIER_MAX_BYTES && this.unlockedPlan() !== 'business') {
      this.requiredPlan.set('business');
      this.showPaymentModal.set(true);
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);
    this.response.set(null);
    this.responseCreatedAt.set(null);
    this.proof.set(null);

    const formData = new FormData();
    formData.append('file', file);
    formData.append('expiresAt', new Date(this.expiresAt()).toISOString());

    const maxDownloads = this.maxDownloads();
    if (maxDownloads !== null) {
      formData.append('maxDownloads', maxDownloads.toString());
    }

    const password = this.passwordEnabled() ? this.password().trim() : '';
    if (password) {
      formData.append('password', password);
    }

    this.filesApi
      .upload(formData)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response) => {
          this.response.set(response);
          this.responseCreatedAt.set(Date.now());
          this.addRecentLink(response);
          this.error.set(null);
          const record = proofFromBackend(file, response.proof, this.paymentTxHash() ?? undefined);
          this.proof.set(record);
          this.toast.success('Link ready', `${response.fileName} is live with a proof certificate.`);
        },
        error: (error) => {
          const detail = error?.error?.detail ?? error?.error?.title ?? 'The upload could not be completed.';
          this.error.set(detail);
          this.toast.error('Upload failed', detail);
        }
      });
  }

  protected openPaymentModal(): void {
    const file = this.selectedFile();
    const requiresBusiness = file && file.size > PRO_TIER_MAX_BYTES;
    this.requiredPlan.set(requiresBusiness ? 'business' : 'pro');
    this.showPaymentModal.set(true);
  }

  protected onPaymentClosed(): void {
    this.showPaymentModal.set(false);
  }

  protected onPaymentConfirmed(detail: { plan: PaymentPlan; txHash: string; currency: PaymentCurrency }): void {
    this.unlockedPlan.set(detail.plan);
    this.paymentTxHash.set(detail.txHash);
    this.paymentCurrency.set(detail.currency);
    this.saveUnlockedPlan(detail.plan);
    this.showPaymentModal.set(false);
    this.toast.success(`${detail.plan === 'pro' ? 'Pro' : 'Business'} unlocked`, 'Proceeding with your upload...');
    setTimeout(() => this.submit(), 200);
  }

  protected downgradeToFree(): void {
    this.unlockedPlan.set(null);
    this.paymentTxHash.set(null);
    this.paymentCurrency.set(null);
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(UNLOCKED_PLAN_STORAGE_KEY);
    }
  }

  protected resetGeneratedResult(fileInput: HTMLInputElement): void {
    this.response.set(null);
    this.responseCreatedAt.set(null);
    this.error.set(null);
    this.proof.set(null);
    this.removeSelectedFile(fileInput);
  }

  protected copyLink(value: string, label: string): void {
    if (!value) {
      return;
    }

    navigator.clipboard
      .writeText(value)
      .then(() => {
        this.copyFeedback.set(`${label} copied`);
        this.clearCopyFeedback();
      })
      .catch(() => {
        this.copyFeedback.set('Could not copy automatically. Please copy manually.');
        this.clearCopyFeedback();
      });
  }

  protected copyAll(response: UploadFileResponse): void {
    const details = [
      `File: ${response.fileName}`,
      `Public page: ${this.publicLink()}`,
      `Direct download: ${response.downloadUrl}`,
      `Expires at: ${new Date(response.expiresAt).toLocaleString()}`,
      `Max downloads: ${response.maxDownloads ?? 'Unlimited'}`
    ].join('\n');

    this.copyLink(details, 'Link details');
  }

  protected formatFileSize(bytes: number): string {
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

  protected getResultStatus(expiresAt: string): string {
    return new Date(expiresAt).getTime() > Date.now() ? 'Active' : 'Expired';
  }

  protected getResultStatusTone(expiresAt: string): 'available' | 'danger' {
    return new Date(expiresAt).getTime() > Date.now() ? 'available' : 'danger';
  }

  protected getRecentStatusTone(expiresAt: string): 'available' | 'danger' {
    return new Date(expiresAt).getTime() > Date.now() ? 'available' : 'danger';
  }

  protected formatRecentExpiry(expiresAt: string): string {
    return this.formatTimeLeft(expiresAt);
  }

  protected compactToken(token: string): string {
    if (token.length <= 18) {
      return token;
    }
    return `${token.slice(0, 8)}...${token.slice(-6)}`;
  }

  protected getFileIconFromName(fileName: string): string {
    const normalized = fileName.toLowerCase();
    if (normalized.endsWith('.pdf')) {
      return 'pdf';
    }
    if (
      normalized.endsWith('.png') ||
      normalized.endsWith('.jpg') ||
      normalized.endsWith('.jpeg') ||
      normalized.endsWith('.gif') ||
      normalized.endsWith('.webp') ||
      normalized.endsWith('.bmp') ||
      normalized.endsWith('.svg')
    ) {
      return 'image';
    }
    if (normalized.endsWith('.zip') || normalized.endsWith('.rar') || normalized.endsWith('.7z')) {
      return 'archive';
    }
    return 'file';
  }

  private setFile(file: File | null): void {
    this.cleanupObjectUrl();

    this.selectedFile.set(file);
    if (file && file.type.startsWith('image/')) {
      this.previewObjectUrl = URL.createObjectURL(file);
      this.filePreviewUrl.set(this.previewObjectUrl);
    } else {
      this.filePreviewUrl.set(null);
    }

    this.error.set(null);
  }

  private getFileIconName(file: File | null): string {
    if (!file) {
      return 'file';
    }

    if (file.type.startsWith('image/')) {
      return 'image';
    }
    if (file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')) {
      return 'pdf';
    }
    if (
      file.type.includes('zip') ||
      file.name.toLowerCase().endsWith('.zip') ||
      file.name.toLowerCase().endsWith('.rar') ||
      file.name.toLowerCase().endsWith('.7z')
    ) {
      return 'archive';
    }
    return 'file';
  }

  private getFileExtension(file: File | null): string {
    if (!file) {
      return '';
    }

    const dotIndex = file.name.lastIndexOf('.');
    if (dotIndex < 0 || dotIndex === file.name.length - 1) {
      return 'FILE';
    }

    return file.name.slice(dotIndex + 1).toUpperCase();
  }

  private getFileTypeLabel(file: File | null): string {
    if (!file) {
      return 'Unknown';
    }
    if (file.type.startsWith('image/')) {
      return 'Image';
    }
    if (file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')) {
      return 'PDF document';
    }
    if (file.type.startsWith('video/')) {
      return 'Video';
    }
    if (file.type.startsWith('audio/')) {
      return 'Audio';
    }
    return file.type || 'File';
  }

  private addRecentLink(response: UploadFileResponse): void {
    const next: RecentLinkItem = {
      id: response.id,
      accessToken: response.accessToken,
      fileName: response.fileName,
      expiresAt: response.expiresAt,
      maxDownloads: response.maxDownloads,
      createdAt: Date.now(),
      publicLink: this.toAbsoluteUrl(`/files/${response.accessToken}`),
      directDownload: response.downloadUrl
    };

    const deduped = this.recentLinks().filter((item) => item.id !== next.id);
    const updated = [next, ...deduped].slice(0, 8);
    this.recentLinks.set(updated);
    this.saveRecentLinks(updated);
  }

  private loadRecentLinks(): RecentLinkItem[] {
    if (typeof localStorage === 'undefined') {
      return [];
    }

    try {
      const raw = localStorage.getItem(RECENT_LINKS_STORAGE_KEY);
      if (!raw) {
        return [];
      }

      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) {
        return [];
      }

      return parsed
        .filter((item) => item && typeof item.id === 'string' && typeof item.fileName === 'string')
        .slice(0, 8);
    } catch {
      return [];
    }
  }

  private saveRecentLinks(links: RecentLinkItem[]): void {
    if (typeof localStorage === 'undefined') {
      return;
    }

    localStorage.setItem(RECENT_LINKS_STORAGE_KEY, JSON.stringify(links));
  }

  private loadUnlockedPlan(): PaymentPlan | null {
    if (typeof localStorage === 'undefined') return null;
    const raw = localStorage.getItem(UNLOCKED_PLAN_STORAGE_KEY);
    if (raw === 'pro' || raw === 'business') return raw;
    return null;
  }

  private saveUnlockedPlan(plan: PaymentPlan): void {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(UNLOCKED_PLAN_STORAGE_KEY, plan);
  }

  protected togglePassword(enabled: boolean): void {
    this.passwordEnabled.set(enabled);
    if (!enabled) this.password.set('');
  }

  private formatTimeLeft(expiresAtValue: string): string {
    const expiresAt = new Date(expiresAtValue).getTime();
    if (Number.isNaN(expiresAt)) {
      return 'Unknown expiration';
    }

    const diff = expiresAt - Date.now();
    if (diff <= 0) {
      return 'Expired';
    }

    const minutes = Math.round(diff / 60000);
    if (minutes < 60) {
      return `${minutes} min left`;
    }

    const hours = Math.round(minutes / 60);
    if (hours < 48) {
      return `${hours}h left`;
    }

    const days = Math.round(hours / 24);
    return `${days}d left`;
  }

  private cleanupObjectUrl(): void {
    if (this.previewObjectUrl) {
      URL.revokeObjectURL(this.previewObjectUrl);
      this.previewObjectUrl = null;
    }
  }

  private clearCopyFeedback(): void {
    if (this.resetCopyFeedbackTimeout) {
      clearTimeout(this.resetCopyFeedbackTimeout);
    }

    this.resetCopyFeedbackTimeout = setTimeout(() => this.copyFeedback.set(null), 1800);
  }

  private toAbsoluteUrl(path: string): string {
    if (typeof window === 'undefined') {
      return path;
    }
    return new URL(path, window.location.origin).toString();
  }

  private buildDefaultExpiration(): string {
    const future = new Date(Date.now() + 24 * 60 * 60 * 1000);
    future.setMinutes(future.getMinutes() - future.getTimezoneOffset());
    return future.toISOString().slice(0, 16);
  }
}
