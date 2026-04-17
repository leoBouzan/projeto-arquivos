export interface TransferProofContract {
  fileHash: string;
  blockNumber: number;
  blockHash: string;
  signature: string;
  issuedAt: string;
}

export interface UploadFileResponse {
  id: string;
  accessToken: string;
  fileName: string;
  expiresAt: string;
  maxDownloads: number | null;
  hasPassword: boolean;
  proof: TransferProofContract;
  metadataUrl: string;
  availabilityUrl: string;
  downloadUrl: string;
}

export interface FileMetadataResponse {
  id: string;
  fileName: string;
  contentType: string;
  size: number;
  createdAt: string;
  expiresAt: string;
  downloadCount: number;
  maxDownloads: number | null;
  status: string;
  hasPassword: boolean;
  proof: TransferProofContract;
}

export interface FileAvailabilityResponse {
  available: boolean;
  status: string;
  reason: string | null;
  expiresAt: string | null;
  downloadCount: number | null;
  maxDownloads: number | null;
  hasPassword: boolean;
}
