import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: number;
  tone: ToastTone;
  title: string;
  message?: string;
  durationMs: number;
}

let counter = 0;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);

  readonly toasts = this._toasts.asReadonly();

  success(title: string, message?: string, durationMs = 3200): number {
    return this.push('success', title, message, durationMs);
  }

  error(title: string, message?: string, durationMs = 4500): number {
    return this.push('error', title, message, durationMs);
  }

  warning(title: string, message?: string, durationMs = 4000): number {
    return this.push('warning', title, message, durationMs);
  }

  info(title: string, message?: string, durationMs = 3000): number {
    return this.push('info', title, message, durationMs);
  }

  dismiss(id: number): void {
    this._toasts.update((list) => list.filter((t) => t.id !== id));
  }

  private push(tone: ToastTone, title: string, message: string | undefined, durationMs: number): number {
    counter += 1;
    const id = counter;
    this._toasts.update((list) => [...list, { id, tone, title, message, durationMs }]);
    setTimeout(() => this.dismiss(id), durationMs);
    return id;
  }
}
