import { CommonModule } from '@angular/common';
import { Component, computed, input, output, signal } from '@angular/core';

import { AppIconComponent } from './app-icon.component';

export type PaymentPlan = 'pro' | 'business';
export type PaymentCurrency = 'eth' | 'usdc' | 'btc';

interface CurrencyInfo {
  id: PaymentCurrency;
  symbol: string;
  label: string;
  rate: number;
  address: string;
  network: string;
}

const CURRENCIES: Record<PaymentCurrency, CurrencyInfo> = {
  eth: {
    id: 'eth',
    symbol: 'ETH',
    label: 'Ethereum',
    rate: 0.00032,
    address: '0x7fE9c8a2f51B3d0a9A7c9b31dC4ae2A7E5b6b1C8',
    network: 'Ethereum · ERC-20'
  },
  usdc: {
    id: 'usdc',
    symbol: 'USDC',
    label: 'USD Coin',
    rate: 1,
    address: '0x2B8a3d4F9e6C7a1b5D8e9F0c3A4B6d7E8f9A0b1C',
    network: 'Polygon · ERC-20'
  },
  btc: {
    id: 'btc',
    symbol: 'BTC',
    label: 'Bitcoin',
    rate: 0.0000105,
    address: 'bc1q8x7f2kq9v5m3n7r4t8y2u6i3o9p1a5s8d7f6g3',
    network: 'Bitcoin · Mainnet'
  }
};

const PLAN_PRICES: Record<PaymentPlan, number> = {
  pro: 9,
  business: 29
};

@Component({
  selector: 'app-crypto-payment-modal',
  standalone: true,
  imports: [CommonModule, AppIconComponent],
  templateUrl: './crypto-payment-modal.component.html',
  styleUrl: './crypto-payment-modal.component.scss'
})
export class CryptoPaymentModalComponent {
  readonly plan = input<PaymentPlan>('pro');
  readonly fileSizeMb = input<number>(0);
  readonly paid = output<{ plan: PaymentPlan; txHash: string; currency: PaymentCurrency }>();
  readonly closed = output<void>();

  protected readonly currencies = Object.values(CURRENCIES);
  protected readonly selectedCurrency = signal<PaymentCurrency>('usdc');
  protected readonly state = signal<'picking' | 'paying' | 'confirming' | 'confirmed'>('picking');
  protected readonly txHash = signal<string>('');
  protected readonly confirmations = signal<number>(0);
  protected readonly copied = signal<string | null>(null);

  protected readonly currency = computed(() => CURRENCIES[this.selectedCurrency()]);
  protected readonly usdPrice = computed(() => PLAN_PRICES[this.plan()]);
  protected readonly cryptoAmount = computed(() => {
    const c = this.currency();
    const value = this.usdPrice() * c.rate;
    return c.id === 'btc' ? value.toFixed(8) : value.toFixed(4);
  });
  protected readonly planLabel = computed(() => (this.plan() === 'pro' ? 'Pro' : 'Business'));
  protected readonly planPerks = computed(() =>
    this.plan() === 'pro'
      ? ['Files up to 5 GB', 'Unlimited downloads', '7-day retention', 'Proof of Transfer HD']
      : ['Unlimited size', 'API access', '30-day retention', 'Priority Proof ledger']
  );

  protected selectCurrency(id: PaymentCurrency): void {
    if (this.state() !== 'picking') return;
    this.selectedCurrency.set(id);
  }

  protected copy(value: string, label: string): void {
    navigator.clipboard.writeText(value).then(() => {
      this.copied.set(label);
      setTimeout(() => this.copied.set(null), 1600);
    });
  }

  protected startPayment(): void {
    this.state.set('paying');
    this.txHash.set(this.generateTxHash());

    setTimeout(() => {
      this.state.set('confirming');
      this.runConfirmations();
    }, 1400);
  }

  protected bypassPayment(): void {
    this.txHash.set(this.generateTxHash());
    this.confirmations.set(3);
    this.state.set('confirmed');
  }

  protected close(): void {
    this.closed.emit();
  }

  protected finishPayment(): void {
    this.paid.emit({
      plan: this.plan(),
      txHash: this.txHash(),
      currency: this.selectedCurrency()
    });
  }

  protected truncate(value: string, head = 10, tail = 8): string {
    if (value.length <= head + tail + 3) return value;
    return `${value.slice(0, head)}...${value.slice(-tail)}`;
  }

  protected qrCells(): boolean[][] {
    const seed = this.currency().address;
    const size = 21;
    const grid: boolean[][] = [];
    let acc = 0;
    for (let i = 0; i < seed.length; i++) acc = (acc * 31 + seed.charCodeAt(i)) >>> 0;

    for (let y = 0; y < size; y++) {
      const row: boolean[] = [];
      for (let x = 0; x < size; x++) {
        if (this.isFinderPattern(x, y, size)) {
          row.push(this.finderFill(x, y, size));
          continue;
        }
        acc = (acc * 1103515245 + 12345) >>> 0;
        row.push(((acc >> 8) & 1) === 1);
      }
      grid.push(row);
    }
    return grid;
  }

  private isFinderPattern(x: number, y: number, size: number): boolean {
    const inTL = x < 7 && y < 7;
    const inTR = x >= size - 7 && y < 7;
    const inBL = x < 7 && y >= size - 7;
    return inTL || inTR || inBL;
  }

  private finderFill(x: number, y: number, size: number): boolean {
    const localX = x >= size - 7 ? x - (size - 7) : x;
    const localY = y >= size - 7 ? y - (size - 7) : y;
    if (localX === 0 || localX === 6 || localY === 0 || localY === 6) return true;
    if (localX >= 2 && localX <= 4 && localY >= 2 && localY <= 4) return true;
    return false;
  }

  private runConfirmations(): void {
    let count = 0;
    const target = 3;
    const tick = () => {
      count++;
      this.confirmations.set(count);
      if (count < target) {
        setTimeout(tick, 700);
      } else {
        setTimeout(() => this.state.set('confirmed'), 400);
      }
    };
    setTimeout(tick, 500);
  }

  private generateTxHash(): string {
    const chars = '0123456789abcdef';
    let out = '0x';
    for (let i = 0; i < 64; i++) {
      out += chars[Math.floor(Math.random() * 16)];
    }
    return out;
  }
}
