import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';

import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-stack" role="region" aria-label="Notifications" aria-live="polite">
      @for (toast of toasts(); track toast.id) {
        <div
          class="toast toast--{{ toast.tone }}"
          role="status"
        >
          <div class="toast__icon" aria-hidden="true">
            @switch (toast.tone) {
              @case ('success') {
                <svg viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                  <path d="m8 12 3 3 5-6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              }
              @case ('error') {
                <svg viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                  <path d="M12 8v5M12 16.5v.01" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                </svg>
              }
              @case ('warning') {
                <svg viewBox="0 0 24 24" fill="none">
                  <path d="M12 3 2 20h20L12 3z" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>
                  <path d="M12 10v4M12 17.5v.01" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                </svg>
              }
              @default {
                <svg viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                  <path d="M12 7.5v.01M12 11v6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/>
                </svg>
              }
            }
          </div>
          <div class="toast__body">
            <p class="toast__title">{{ toast.title }}</p>
            @if (toast.message) {
              <p class="toast__msg">{{ toast.message }}</p>
            }
          </div>
          <button class="toast__close" type="button" (click)="dismiss(toast.id)" aria-label="Dismiss">
            <svg viewBox="0 0 20 20" fill="none"><path d="M6 6l8 8M6 14L14 6" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>
          </button>
          <span class="toast__progress" [style.animation-duration.ms]="toast.durationMs"></span>
        </div>
      }
    </div>
  `,
  styles: `
    :host { display: contents; }

    .toast-stack {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 2000;
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      width: min(100%, 22rem);
      pointer-events: none;
    }

    .toast {
      position: relative;
      display: grid;
      grid-template-columns: auto 1fr auto;
      gap: 0.75rem;
      align-items: flex-start;
      padding: 0.85rem 0.95rem 0.85rem 0.85rem;
      border-radius: var(--radius-lg);
      background: #fffcf6;
      border: 1px solid var(--surface-border-strong);
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15), 0 2px 8px rgba(0, 0, 0, 0.06);
      pointer-events: auto;
      overflow: hidden;
      animation: toast-in 260ms cubic-bezier(0.34, 1.56, 0.64, 1);
      color: var(--ink-900);
    }

    .toast__icon {
      width: 1.6rem;
      height: 1.6rem;
      display: grid;
      place-items: center;
      flex-shrink: 0;
    }

    .toast__icon svg {
      width: 100%;
      height: 100%;
    }

    .toast--success {
      border-color: rgba(11, 99, 92, 0.35);
      background: linear-gradient(135deg, #fffcf6 0%, rgba(30, 173, 163, 0.05) 100%);
    }
    .toast--success .toast__icon { color: var(--success-text); }

    .toast--error {
      border-color: rgba(154, 47, 36, 0.35);
      background: linear-gradient(135deg, #fffcf6 0%, rgba(154, 47, 36, 0.05) 100%);
    }
    .toast--error .toast__icon { color: var(--danger-text); }

    .toast--warning {
      border-color: rgba(138, 90, 24, 0.35);
      background: linear-gradient(135deg, #fffcf6 0%, rgba(138, 90, 24, 0.05) 100%);
    }
    .toast--warning .toast__icon { color: var(--warning-text); }

    .toast--info .toast__icon { color: var(--accent-700); }

    .toast__title {
      margin: 0;
      font: 600 0.88rem/1.25 var(--font-sans);
      color: var(--ink-900);
    }

    .toast__msg {
      margin: 0.2rem 0 0;
      font: 400 0.78rem/1.4 var(--font-sans);
      color: var(--ink-700);
    }

    .toast__close {
      flex-shrink: 0;
      width: 1.5rem;
      height: 1.5rem;
      border: none;
      border-radius: var(--radius-sm);
      background: transparent;
      color: var(--ink-600);
      cursor: pointer;
      display: grid;
      place-items: center;
      transition: background var(--duration-fast) ease, color var(--duration-fast) ease;
    }

    .toast__close:hover {
      background: rgba(22, 36, 34, 0.08);
      color: var(--ink-900);
    }

    .toast__close svg {
      width: 0.9rem;
      height: 0.9rem;
    }

    .toast__progress {
      position: absolute;
      bottom: 0;
      left: 0;
      height: 2px;
      width: 100%;
      transform-origin: left;
      animation: toast-progress linear forwards;
    }

    .toast--success .toast__progress { background: var(--success-text); }
    .toast--error .toast__progress { background: var(--danger-text); }
    .toast--warning .toast__progress { background: var(--warning-text); }
    .toast--info .toast__progress { background: var(--accent-500); }

    @keyframes toast-in {
      from { opacity: 0; transform: translateX(16px) scale(0.96); }
      to   { opacity: 1; transform: translateX(0) scale(1); }
    }

    @keyframes toast-progress {
      from { transform: scaleX(1); }
      to   { transform: scaleX(0); }
    }

    @media (max-width: 640px) {
      .toast-stack {
        left: 1rem;
        right: 1rem;
        top: auto;
        bottom: 1rem;
        width: auto;
      }
    }
  `
})
export class ToastContainerComponent {
  private readonly toastService = inject(ToastService);

  protected readonly toasts = this.toastService.toasts;

  protected dismiss(id: number): void {
    this.toastService.dismiss(id);
  }
}
