import { Component, input } from '@angular/core';

@Component({
  selector: 'app-icon',
  template: `
    <span class="icon" aria-hidden="true">
      @switch (name()) {
        @case ('upload') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 16V4M12 4L7.5 8.5M12 4L16.5 8.5M5 15.5V18a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-2.5" />
          </svg>
        }
        @case ('file') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8zM14 3v5h5" />
          </svg>
        }
        @case ('image') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zM8 14l2.7-2.7a1 1 0 0 1 1.4 0L16 15M8 9h.01" />
          </svg>
        }
        @case ('pdf') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8zM14 3v5h5M8 16h8M8 12h4" />
          </svg>
        }
        @case ('archive') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M4 8h16v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zM4 3h16v5H4zM10 12h4v4h-4z" />
          </svg>
        }
        @case ('link') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M10 13.5l4-4M8.5 8.5l-2 2a3 3 0 0 0 4.2 4.2l2-2M15.5 15.5l2-2a3 3 0 1 0-4.2-4.2l-2 2" />
          </svg>
        }
        @case ('clock') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 7v5l3 2M21 12a9 9 0 1 1-18 0a9 9 0 0 1 18 0z" />
          </svg>
        }
        @case ('download') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 4v10m0 0l-4-4m4 4l4-4M5 19h14" />
          </svg>
        }
        @case ('success') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="m7 12 3 3 7-7M21 12a9 9 0 1 1-18 0a9 9 0 0 1 18 0z" />
          </svg>
        }
        @case ('warning') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 9v4m0 4h.01M10.3 4.84 2.7 18a2 2 0 0 0 1.73 3h15.14A2 2 0 0 0 21.3 18L13.7 4.84a2 2 0 0 0-3.4 0z" />
          </svg>
        }
        @case ('expired') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 7v5l3 2M5.64 5.64l12.72 12.72M21 12a9 9 0 1 1-18 0a9 9 0 0 1 18 0z" />
          </svg>
        }
        @case ('error') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 8v5m0 3h.01M21 12a9 9 0 1 1-18 0a9 9 0 0 1 18 0z" />
          </svg>
        }
        @case ('copy') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M9 9h10v10H9zM5 15H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v1" />
          </svg>
        }
        @case ('open') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M14 4h6v6M10 14 20 4M20 13v6a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1h6" />
          </svg>
        }
        @case ('delete') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M4 7h16M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
          </svg>
        }
        @case ('user') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M20 21a8 8 0 0 0-16 0M12 11a4 4 0 1 0 0-8a4 4 0 0 0 0 8z" />
          </svg>
        }
        @case ('group') {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M16 19a4 4 0 0 1 4 4M8 19a4 4 0 0 0-4 4M12 16a4 4 0 1 0 0-8a4 4 0 0 0 0 8M18 11a3 3 0 1 0 0-6M6 11a3 3 0 1 1 0-6" />
          </svg>
        }
        @default {
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 4v16M4 12h16" />
          </svg>
        }
      }
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
      line-height: 0;
    }

    .icon {
      width: var(--icon-size, 1rem);
      height: var(--icon-size, 1rem);
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }

    svg {
      width: 100%;
      height: 100%;
      stroke: currentColor;
      stroke-width: 1.8;
      stroke-linecap: round;
      stroke-linejoin: round;
    }
  `
})
export class AppIconComponent {
  readonly name = input<string>('file');
}
