import { Component, input } from '@angular/core';

@Component({
  selector: 'app-floating-background',
  template: `
    <div class="floating-bg" [class.floating-bg--subtle]="subtle()" aria-hidden="true">
      <svg class="floating-bg__svg" viewBox="0 0 1200 700" preserveAspectRatio="xMidYMid slice">
        <defs>
          <linearGradient id="fbGrad1" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stop-color="var(--accent-700)" stop-opacity="0.18"/>
            <stop offset="100%" stop-color="var(--accent-400)" stop-opacity="0.06"/>
          </linearGradient>
          <linearGradient id="fbGrad2" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stop-color="var(--accent-500)" stop-opacity="0"/>
            <stop offset="50%" stop-color="var(--accent-500)" stop-opacity="0.3"/>
            <stop offset="100%" stop-color="var(--accent-500)" stop-opacity="0"/>
          </linearGradient>
          <filter id="fbBlur">
            <feGaussianBlur stdDeviation="1.5"/>
          </filter>
        </defs>

        <!-- Connection lines -->
        <g class="fb-lines" filter="url(#fbBlur)">
          <path class="fb-line fb-line--1" d="M 80 200 Q 350 120 600 300 Q 850 480 1100 350"/>
          <path class="fb-line fb-line--2" d="M 50 450 Q 300 380 550 480 Q 800 580 1050 500"/>
          <path class="fb-line fb-line--3" d="M 200 80 Q 450 150 650 200 Q 900 260 1150 180"/>
          <path class="fb-line fb-line--4" d="M 100 600 Q 350 540 600 570 Q 850 600 1100 560"/>
        </g>

        <!-- Floating file cards -->
        <g class="fb-card fb-card--1">
          <rect x="0" y="0" width="72" height="88" rx="12" fill="white" fill-opacity="0.55" stroke="var(--accent-700)" stroke-opacity="0.2" stroke-width="1"/>
          <rect x="8" y="10" width="36" height="4" rx="2" fill="var(--accent-700)" fill-opacity="0.25"/>
          <rect x="8" y="20" width="56" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.1"/>
          <rect x="8" y="29" width="48" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.1"/>
          <rect x="8" y="38" width="52" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.1"/>
          <rect x="8" y="54" width="24" height="16" rx="4" fill="var(--accent-100)"/>
          <rect x="38" y="54" width="26" height="16" rx="4" fill="var(--accent-50)"/>
          <text x="20" y="66" font-size="6" fill="var(--accent-700)" font-family="monospace" opacity="0.7">PDF</text>
          <text x="42" y="66" font-size="6" fill="var(--accent-700)" font-family="monospace" opacity="0.7">2.1MB</text>
          <rect x="8" y="76" width="56" height="4" rx="2" fill="var(--accent-200)" fill-opacity="0.5"/>
        </g>

        <g class="fb-card fb-card--2">
          <rect x="0" y="0" width="68" height="82" rx="11" fill="white" fill-opacity="0.5" stroke="var(--accent-700)" stroke-opacity="0.18" stroke-width="1"/>
          <rect x="8" y="10" width="28" height="4" rx="2" fill="var(--accent-700)" fill-opacity="0.2"/>
          <rect x="8" y="20" width="52" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.08"/>
          <rect x="8" y="29" width="44" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.08"/>
          <rect x="8" y="46" width="52" height="22" rx="6" fill="var(--accent-100)" fill-opacity="0.6"/>
          <text x="34" y="61" font-size="8" fill="var(--accent-700)" font-family="monospace" opacity="0.8" text-anchor="middle">IMG</text>
          <rect x="8" y="72" width="52" height="3" rx="2" fill="var(--accent-200)" fill-opacity="0.5"/>
        </g>

        <g class="fb-card fb-card--3">
          <rect x="0" y="0" width="80" height="96" rx="13" fill="white" fill-opacity="0.52" stroke="var(--accent-700)" stroke-opacity="0.22" stroke-width="1"/>
          <rect x="8" y="10" width="40" height="4" rx="2" fill="var(--accent-700)" fill-opacity="0.22"/>
          <rect x="8" y="20" width="64" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.1"/>
          <rect x="8" y="29" width="56" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.1"/>
          <rect x="8" y="38" width="60" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.1"/>
          <rect x="8" y="47" width="48" height="3" rx="2" fill="var(--ink-900)" fill-opacity="0.07"/>
          <rect x="8" y="62" width="64" height="20" rx="5" fill="var(--accent-50)"/>
          <rect x="12" y="66" width="28" height="3" rx="2" fill="var(--accent-700)" fill-opacity="0.3"/>
          <rect x="12" y="74" width="20" height="3" rx="2" fill="var(--accent-700)" fill-opacity="0.2"/>
          <rect x="8" y="86" width="64" height="4" rx="2" fill="var(--accent-200)" fill-opacity="0.4"/>
        </g>

        <!-- Pulse dots -->
        <circle class="fb-dot fb-dot--1" cx="600" cy="300" r="5" fill="var(--accent-500)" fill-opacity="0.25"/>
        <circle class="fb-dot fb-dot--2" cx="350" cy="150" r="3.5" fill="var(--accent-400)" fill-opacity="0.2"/>
        <circle class="fb-dot fb-dot--3" cx="850" cy="480" r="4" fill="var(--accent-600)" fill-opacity="0.18"/>
        <circle class="fb-dot fb-dot--4" cx="1050" cy="200" r="3" fill="var(--accent-500)" fill-opacity="0.15"/>
      </svg>
    </div>
  `,
  styles: `
    :host {
      display: block;
      position: absolute;
      inset: 0;
      overflow: hidden;
      pointer-events: none;
      z-index: 0;
    }

    .floating-bg {
      position: absolute;
      inset: 0;
    }

    .floating-bg__svg {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      overflow: visible;
    }

    /* Lines */
    .fb-line {
      fill: none;
      stroke: url(#fbGrad2);
      stroke-width: 1.2;
      stroke-dasharray: 8 12;
      animation: fb-flow 8s linear infinite;
    }
    .fb-line--2 { animation-delay: -2.5s; animation-duration: 10s; }
    .fb-line--3 { animation-delay: -5s; animation-duration: 12s; }
    .fb-line--4 { animation-delay: -1s; animation-duration: 9s; }

    /* Cards */
    .fb-card {
      animation: fb-float 7s ease-in-out infinite;
      transform-box: fill-box;
      transform-origin: center;
    }
    .fb-card--1 {
      transform: translate(90px, 160px);
      animation-duration: 8s;
    }
    .fb-card--2 {
      transform: translate(1010px, 80px);
      animation-duration: 9.5s;
      animation-delay: -3s;
    }
    .fb-card--3 {
      transform: translate(1070px, 500px);
      animation-duration: 11s;
      animation-delay: -6s;
    }

    /* Pulse dots */
    .fb-dot {
      animation: fb-pulse 3.5s ease-in-out infinite;
    }
    .fb-dot--2 { animation-delay: -1.2s; animation-duration: 4s; }
    .fb-dot--3 { animation-delay: -2.5s; }
    .fb-dot--4 { animation-delay: -0.8s; animation-duration: 5s; }

    /* Animations */
    @keyframes fb-flow {
      from { stroke-dashoffset: 80; }
      to { stroke-dashoffset: 0; }
    }

    @keyframes fb-float {
      0%, 100% { transform: translate(var(--tx, 90px), var(--ty, 160px)) translateY(0px) rotate(0deg); }
      33% { transform: translate(var(--tx, 90px), var(--ty, 160px)) translateY(-8px) rotate(0.8deg); }
      66% { transform: translate(var(--tx, 90px), var(--ty, 160px)) translateY(-3px) rotate(-0.5deg); }
    }

    .fb-card--1 { --tx: 90px; --ty: 160px; }
    .fb-card--2 { --tx: 1010px; --ty: 80px; }
    .fb-card--3 { --tx: 1070px; --ty: 500px; }

    @keyframes fb-pulse {
      0%, 100% { opacity: 0.2; transform: scale(0.85); }
      50% { opacity: 0.8; transform: scale(1.25); }
    }

    /* Subtle variant (for upload/download pages) */
    .floating-bg--subtle .fb-line {
      stroke-opacity: 0.5;
    }

    .floating-bg--subtle .fb-card rect:first-child {
      fill-opacity: 0.3;
    }

    .floating-bg--subtle .fb-dot {
      fill-opacity: 0.12;
    }

    /* Reduce motion */
    @media (prefers-reduced-motion: reduce) {
      .fb-line, .fb-card, .fb-dot {
        animation: none;
      }
    }
  `
})
export class FloatingBackgroundComponent {
  readonly subtle = input(false);
}
