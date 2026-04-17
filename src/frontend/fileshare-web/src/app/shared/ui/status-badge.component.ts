import { Component, input } from '@angular/core';

import { AppIconComponent } from './app-icon.component';

@Component({
  selector: 'app-status-badge',
  imports: [AppIconComponent],
  template: `
    <span class="status-badge" [class.status-badge--available]="tone() === 'available'" [class.status-badge--warning]="tone() === 'warning'" [class.status-badge--danger]="tone() === 'danger'">
      <app-icon [name]="iconName()" />
      {{ label() }}
    </span>
  `,
  styles: `
    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      border-radius: 999px;
      padding: 0.36rem 0.78rem;
      font: 600 0.78rem/1 var(--font-mono);
      letter-spacing: 0.06em;
      text-transform: uppercase;
      border: 1px solid transparent;
    }

    .status-badge app-icon {
      --icon-size: 0.86rem;
    }

    .status-badge--available {
      color: #0b635c;
      border-color: rgba(11, 99, 92, 0.24);
      background: rgba(11, 99, 92, 0.1);
    }

    .status-badge--warning {
      color: #8a5a18;
      border-color: rgba(138, 90, 24, 0.24);
      background: rgba(138, 90, 24, 0.1);
    }

    .status-badge--danger {
      color: #9a2f24;
      border-color: rgba(154, 47, 36, 0.22);
      background: rgba(154, 47, 36, 0.1);
    }
  `
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input<'available' | 'warning' | 'danger'>('available');

  protected iconName(): string {
    if (this.tone() === 'available') {
      return 'success';
    }
    if (this.tone() === 'warning') {
      return 'warning';
    }
    return 'expired';
  }
}
