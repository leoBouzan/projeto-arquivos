import { Component } from '@angular/core';

@Component({
  selector: 'app-share-animation',
  template: `
    <div class="share-graphic" aria-hidden="true">
      <svg viewBox="0 0 320 210" role="presentation">
        <defs>
          <linearGradient id="lineGradient" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stop-color="#0d746c" stop-opacity="0.25" />
            <stop offset="100%" stop-color="#0d746c" stop-opacity="0.85" />
          </linearGradient>
        </defs>

        <g class="connections">
          <path class="line line--one" d="M160 98C122 74 92 62 60 58" />
          <path class="line line--two" d="M160 106C122 132 92 146 58 152" />
          <path class="line line--three" d="M166 112C198 132 232 146 264 154" />
        </g>

        <g class="pulse pulse--one">
          <circle cx="108" cy="76" r="4" />
        </g>
        <g class="pulse pulse--two">
          <circle cx="104" cy="136" r="4" />
        </g>
        <g class="pulse pulse--three">
          <circle cx="220" cy="138" r="4" />
        </g>

        <g class="center-file">
          <rect x="132" y="74" width="56" height="64" rx="14" />
          <path d="M150 95h20M150 108h20M150 121h14" />
        </g>

        <g class="user user--one">
          <circle cx="52" cy="54" r="16" />
          <circle cx="52" cy="50" r="5" />
          <path d="M44 60c2.8-3 12.2-3 16 0" />
        </g>
        <g class="user user--two">
          <circle cx="52" cy="156" r="16" />
          <circle cx="52" cy="152" r="5" />
          <path d="M44 162c2.8-3 12.2-3 16 0" />
        </g>
        <g class="user user--three">
          <circle cx="268" cy="156" r="16" />
          <circle cx="268" cy="152" r="5" />
          <path d="M260 162c2.8-3 12.2-3 16 0" />
        </g>
      </svg>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .share-graphic {
      border: 1px solid rgba(16, 30, 28, 0.1);
      border-radius: 1.4rem;
      background:
        radial-gradient(circle at 85% 10%, rgba(22, 145, 135, 0.12), transparent 38%),
        rgba(255, 255, 255, 0.68);
      padding: 0.6rem;
      overflow: hidden;
    }

    svg {
      width: 100%;
      height: auto;
      display: block;
      color: #0d746c;
      fill: none;
      stroke-linecap: round;
      stroke-linejoin: round;
    }

    .line {
      stroke: url(#lineGradient);
      stroke-width: 2;
      stroke-dasharray: 6 8;
      animation: flow 3.1s linear infinite;
    }

    .line--two {
      animation-delay: 0.5s;
    }

    .line--three {
      animation-delay: 0.9s;
    }

    .pulse circle {
      fill: #0d746c;
      opacity: 0.25;
      animation: pulse 2.2s ease-in-out infinite;
    }

    .pulse--two circle {
      animation-delay: 0.6s;
    }

    .pulse--three circle {
      animation-delay: 1.1s;
    }

    .center-file rect {
      fill: rgba(13, 116, 108, 0.14);
      stroke: rgba(13, 116, 108, 0.36);
      stroke-width: 1.4;
    }

    .center-file path {
      stroke: rgba(13, 116, 108, 0.88);
      stroke-width: 1.6;
    }

    .center-file {
      animation: breathe 3.4s ease-in-out infinite;
      transform-origin: 160px 106px;
    }

    .user circle:first-child {
      fill: rgba(23, 38, 36, 0.08);
      stroke: rgba(23, 38, 36, 0.14);
      stroke-width: 1.2;
    }

    .user circle:nth-child(2),
    .user path {
      stroke: rgba(23, 38, 36, 0.5);
      stroke-width: 1.6;
    }

    @keyframes flow {
      from {
        stroke-dashoffset: 56;
      }
      to {
        stroke-dashoffset: 0;
      }
    }

    @keyframes pulse {
      0%,
      100% {
        opacity: 0.18;
        transform: scale(0.9);
      }
      50% {
        opacity: 0.6;
        transform: scale(1.12);
      }
    }

    @keyframes breathe {
      0%,
      100% {
        transform: translateY(0);
      }
      50% {
        transform: translateY(-2px);
      }
    }

    @media (max-width: 760px) {
      .share-graphic {
        padding: 0.45rem;
      }

      .line {
        animation-duration: 3.8s;
      }
    }
  `
})
export class ShareAnimationComponent {}
