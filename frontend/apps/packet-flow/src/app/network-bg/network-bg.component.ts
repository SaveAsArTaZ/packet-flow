import { Component, inject } from '@angular/core';
import { LanguageService } from '../language.service';

@Component({
  selector: 'pf-network-bg',
  standalone: true,
  host: {
    '[class.rtl-side]': 'lang.currentDir === "rtl"',
  },
  template: `
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 1200 800"
      preserveAspectRatio="xMidYMid slice"
      class="network-bg"
      [class.rtl-flip]="lang.currentDir === 'rtl'"
      aria-hidden="true"
    >
      <!-- BUS TOPOLOGY — bus backbone on right, computers connected via drop lines -->

      <!-- Bus backbone cable -->
      <line x1="900" y1="40" x2="900" y2="760" stroke="currentColor" stroke-width="3" />

      <!-- Terminators -->
      <rect x="892" y="33" width="16" height="14" rx="3" fill="currentColor" />
      <rect x="892" y="753" width="16" height="14" rx="3" fill="currentColor" />

      <!-- Drop lines (bus -> computer) -->
      <g stroke="currentColor" stroke-width="1.5">
        <line x1="900" y1="90"  x2="1120" y2="90" />
        <line x1="900" y1="214" x2="1120" y2="214" />
        <line x1="900" y1="338" x2="1120" y2="338" />
        <line x1="900" y1="462" x2="1120" y2="462" />
        <line x1="900" y1="586" x2="1120" y2="586" />
        <line x1="900" y1="710" x2="1120" y2="710" />
      </g>

      <!-- T-connector dots -->
      <g fill="currentColor">
        <circle cx="900" cy="90"  r="5" />
        <circle cx="900" cy="214" r="5" />
        <circle cx="900" cy="338" r="5" />
        <circle cx="900" cy="462" r="5" />
        <circle cx="900" cy="586" r="5" />
        <circle cx="900" cy="710" r="5" />
      </g>

      <!-- Computers (monitor + base) -->
      <g fill="currentColor">
        <rect x="1110" y="75"  width="22" height="16" rx="3" /><rect x="1116" y="91"  width="10" height="5" rx="1" />
        <rect x="1110" y="199" width="22" height="16" rx="3" /><rect x="1116" y="215" width="10" height="5" rx="1" />
        <rect x="1110" y="323" width="22" height="16" rx="3" /><rect x="1116" y="339" width="10" height="5" rx="1" />
        <rect x="1110" y="447" width="22" height="16" rx="3" /><rect x="1116" y="463" width="10" height="5" rx="1" />
        <rect x="1110" y="571" width="22" height="16" rx="3" /><rect x="1116" y="587" width="10" height="5" rx="1" />
        <rect x="1110" y="695" width="22" height="16" rx="3" /><rect x="1116" y="711" width="10" height="5" rx="1" />
      </g>

      <!-- Traveling packets -->
      <g fill="currentColor">
        <circle r="4.5" class="pkt pkt-1" />
        <circle r="4.5" class="pkt pkt-2" />
        <circle r="4.5" class="pkt pkt-3" />
        <circle r="4.5" class="pkt pkt-4" />
        <circle r="4.5" class="pkt pkt-5" />
        <circle r="4.5" class="pkt pkt-6" />
      </g>
    </svg>
  `,
  styles: `
    :host {
      position: absolute;
      top: 0;
      right: 0;
      width: 45%;
      height: 100%;
      pointer-events: none;
      z-index: 0;
    }

    :host(.rtl-side) {
      right: auto;
      left: 0;
    }

    .rtl-flip {
      transform: scaleX(-1);
    }

    .network-bg {
      width: 100%;
      height: 100%;
      color: var(--foreground, #000);
    }

    /* ===== TRAVELING PACKETS =====
       Each packet travels down the bus to its computer sequentially.
       14s total cycle for 6 computers. */
    .pkt {
      offset-rotate: 0deg;
      animation-name: pkt-bus;
      animation-duration: 14s;
      animation-iteration-count: infinite;
      animation-timing-function: linear;
    }

    @keyframes pkt-bus {
      0%   { offset-distance: 0%; opacity: 1; }
      10%  { offset-distance: 100%; opacity: 1; }
      14%  { offset-distance: 100%; opacity: 1; }
      16%  { offset-distance: 100%; opacity: 0; }
      100% { offset-distance: 100%; opacity: 0; }
    }

    .pkt-1 {
      offset-path: path('M900,40 L900,90 L1120,90');
      animation-delay: 0s;
    }
    .pkt-2 {
      offset-path: path('M900,40 L900,214 L1120,214');
      animation-delay: 2.33s;
    }
    .pkt-3 {
      offset-path: path('M900,40 L900,338 L1120,338');
      animation-delay: 4.66s;
    }
    .pkt-4 {
      offset-path: path('M900,40 L900,462 L1120,462');
      animation-delay: 7s;
    }
    .pkt-5 {
      offset-path: path('M900,40 L900,586 L1120,586');
      animation-delay: 9.33s;
    }
    .pkt-6 {
      offset-path: path('M900,40 L900,710 L1120,710');
      animation-delay: 11.66s;
    }
  `,
})
export class NetworkBgComponent {
  readonly lang = inject(LanguageService);
}
