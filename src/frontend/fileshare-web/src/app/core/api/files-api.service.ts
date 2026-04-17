import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import {
  FileAvailabilityResponse,
  FileMetadataResponse,
  UploadFileResponse
} from '../../shared/models/file-contracts';

@Injectable({ providedIn: 'root' })
export class FilesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/files';

  upload(formData: FormData) {
    return this.http.post<UploadFileResponse>(this.baseUrl, formData);
  }

  getMetadata(token: string) {
    return this.http.get<FileMetadataResponse>(`${this.baseUrl}/${token}/metadata`);
  }

  getAvailability(token: string) {
    return this.http.get<FileAvailabilityResponse>(`${this.baseUrl}/${token}/availability`);
  }

  getDownloadUrl(token: string, password?: string | null) {
    const base = `${this.baseUrl}/${token}/download`;
    if (!password) return base;
    return `${base}?password=${encodeURIComponent(password)}`;
  }

  verifyProof(hashPrefix: string) {
    return this.http.get<{
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
    }>(`/api/proof/${encodeURIComponent(hashPrefix)}`);
  }
}
