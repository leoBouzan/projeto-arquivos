import { CommonModule } from '@angular/common';
import { Component, computed, input, signal } from '@angular/core';

import { AppIconComponent } from './app-icon.component';

export interface ProofRecord {
  fileName: string;
  fileSize: number;
  fileHash: string;
  timestamp: string;
  blockNumber: number;
  blockHash: string;
  signature: string;
  txHash?: string;
}

@Component({
  selector: 'app-proof-of-transfer',
  standalone: true,
  imports: [CommonModule, AppIconComponent],
  templateUrl: './proof-of-transfer.component.html',
  styleUrl: './proof-of-transfer.component.scss'
})
export class ProofOfTransferComponent {
  readonly proof = input.required<ProofRecord>();
  readonly compact = input<boolean>(false);

  protected readonly copied = signal<string | null>(null);

  protected readonly verifyUrl = computed(() => {
    const origin = typeof window !== 'undefined' ? window.location.origin : '';
    return `${origin}/verify/${this.proof().fileHash.slice(0, 16)}`;
  });

  protected readonly shortHash = computed(() => this.truncate(this.proof().fileHash, 10, 8));
  protected readonly shortBlockHash = computed(() => this.truncate(this.proof().blockHash, 10, 8));
  protected readonly shortSignature = computed(() => this.truncate(this.proof().signature, 12, 10));
  protected readonly formattedTs = computed(() => {
    const d = new Date(this.proof().timestamp);
    return d.toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  });
  protected readonly iso = computed(() => new Date(this.proof().timestamp).toISOString());

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

  private truncate(value: string, head: number, tail: number): string {
    if (value.length <= head + tail + 3) return value;
    return `${value.slice(0, head)}...${value.slice(-tail)}`;
  }
}

import type { TransferProofContract } from '../models/file-contracts';

export function proofFromBackend(
  file: { name: string; size: number },
  contract: TransferProofContract,
  txHash?: string
): ProofRecord {
  return {
    fileName: file.name,
    fileSize: file.size,
    fileHash: contract.fileHash,
    timestamp: contract.issuedAt,
    blockNumber: contract.blockNumber,
    blockHash: contract.blockHash,
    signature: contract.signature,
    txHash
  };
}
