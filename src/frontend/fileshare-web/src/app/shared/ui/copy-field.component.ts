import { Component, OnDestroy, input, signal } from '@angular/core';

import { AppIconComponent } from './app-icon.component';

@Component({
  selector: 'app-copy-field',
  imports: [AppIconComponent],
  template: `
    <div class="copy-field">
      <div class="copy-field__top">
        <p class="copy-field__label">
          <app-icon [name]="icon()" />
          {{ label() }}
        </p>
        <div class="copy-field__actions">
          <button type="button" class="action" (click)="copyValue()">
            <app-icon name="copy" />
            Copy
          </button>
          @if (openHref()) {
            <a class="action" [href]="openHref()" target="_blank" rel="noreferrer">
              <app-icon name="open" />
              Open
            </a>
          }
        </div>
      </div>
      <p class="copy-field__value" [title]="value()">{{ value() }}</p>
      @if (copied()) {
        <p class="copy-field__feedback">Copied</p>
      }
    </div>
  `,
  styles: `
    .copy-field {
      border: 1px solid rgba(16, 30, 28, 0.12);
      border-radius: 1rem;
      background: rgba(255, 255, 255, 0.78);
      padding: 0.84rem;
      min-width: 0;
    }

    .copy-field__top {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.9rem;
    }

    .copy-field__label {
      margin: 0;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      font: 600 0.74rem/1.2 var(--font-mono);
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ink-600);
    }

    .copy-field__label app-icon {
      --icon-size: 0.78rem;
      color: var(--accent-700);
    }

    .copy-field__actions {
      display: inline-flex;
      gap: 0.45rem;
      flex-wrap: wrap;
      justify-content: flex-end;
    }

    .action {
      border: 1px solid rgba(16, 30, 28, 0.16);
      border-radius: 0.65rem;
      padding: 0.37rem 0.56rem;
      background: rgba(255, 255, 255, 0.66);
      color: var(--ink-900);
      text-decoration: none;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
      font: 500 0.75rem/1 var(--font-sans);
      transition: border-color 130ms ease, transform 130ms ease;
    }

    .action:hover {
      border-color: var(--accent-500);
      transform: translateY(-1px);
    }

    .action app-icon {
      --icon-size: 0.75rem;
    }

    .copy-field__value {
      margin: 0.72rem 0 0;
      padding-top: 0.58rem;
      border-top: 1px solid rgba(16, 30, 28, 0.09);
      color: var(--ink-900);
      font: 500 0.83rem/1.45 var(--font-sans);
      overflow-wrap: anywhere;
      word-break: break-word;
    }

    .copy-field__feedback {
      margin: 0.4rem 0 0;
      color: var(--accent-700);
      font-size: 0.76rem;
    }

    @media (max-width: 600px) {
      .copy-field__top {
        flex-direction: column;
        align-items: flex-start;
      }
    }
  `
})
export class CopyFieldComponent implements OnDestroy {
  private resetFeedbackTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly icon = input('link');
  readonly openHref = input<string | null>(null);

  protected readonly copied = signal(false);

  protected copyValue(): void {
    navigator.clipboard
      .writeText(this.value())
      .then(() => {
        this.copied.set(true);
        if (this.resetFeedbackTimeout) {
          clearTimeout(this.resetFeedbackTimeout);
        }
        this.resetFeedbackTimeout = setTimeout(() => this.copied.set(false), 1200);
      })
      .catch(() => {
        this.copied.set(false);
      });
  }

  ngOnDestroy(): void {
    if (this.resetFeedbackTimeout) {
      clearTimeout(this.resetFeedbackTimeout);
    }
  }
}
