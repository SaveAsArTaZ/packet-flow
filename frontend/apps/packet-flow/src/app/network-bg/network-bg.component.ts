import { Component } from '@angular/core';

@Component({
  selector: 'pf-network-bg',
  standalone: true,
  template: `
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 1200 800"
      preserveAspectRatio="xMidYMid slice"
      class="network-bg"
      aria-hidden="true"
    >
      <!--
        BUS TOPOLOGY — right side of the viewBox.
        Bus backbone: vertical line at x=900.
        6 computers at x=1120, connected via drop lines.
        Packets travel sequentially down the bus to each computer.
      -->

      <!-- ===== BUS BACKBONE CABLE ===== -->
      <line x1="900" y1="55" x2="900" y2="745" stroke="currentColor" stroke-width="3" opacity="0.15" />

      <!-- ===== TERMINATORS ===== -->
      <rect x="892" y="48" width="16" height="14" rx="3" fill="currentColor" opacity="0.18" />
      <rect x="892" y="738" width="16" height="14" rx="3" fill="currentColor" opacity="0.18" />

      <!-- ===== DROP LINES (bus -> computer) ===== -->
      <g stroke="currentColor" stroke-width="1.5" opacity="0.1">
        <line x1="900" y1="110" x2="1120" y2="110" />
        <line x1="900" y1="210" x2="1120" y2="210" />
        <line x1="900" y1="310" x2="1120" y2="310" />
        <line x1="900" y1="410" x2="1120" y2="410" />
        <line x1="900" y1="510" x2="1120" y2="510" />
        <line x1="900" y1="610" x2="1120" y2="610" />
      </g>

      <!-- T-connector dots at each tap point -->
      <g fill="currentColor" opacity="0.12">
        <circle cx="900" cy="110" r="5" />
        <circle cx="900" cy="210" r="5" />
        <circle cx="900" cy="310" r="5" />
        <circle cx="900" cy="410" r="5" />
        <circle cx="900" cy="510" r="5" />
        <circle cx="900" cy="610" r="5" />
      </g>

      <!-- ===== COMPUTERS (monitor + base) ===== -->
      <g fill="currentColor" opacity="0.12">
        <!-- Computer 1 -->
        <rect x="1110" y="95" width="22" height="16" rx="3" />
        <rect x="1116" y="111" width="10" height="5" rx="1" />
        <!-- Computer 2 -->
        <rect x="1110" y="195" width="22" height="16" rx="3" />
        <rect x="1116" y="211" width="10" height="5" rx="1" />
        <!-- Computer 3 -->
        <rect x="1110" y="295" width="22" height="16" rx="3" />
        <rect x="1116" y="311" width="10" height="5" rx="1" />
        <!-- Computer 4 -->
        <rect x="1110" y="395" width="22" height="16" rx="3" />
        <rect x="1116" y="411" width="10" height="5" rx="1" />
        <!-- Computer 5 -->
        <rect x="1110" y="495" width="22" height="16" rx="3" />
        <rect x="1116" y="511" width="10" height="5" rx="1" />
        <!-- Computer 6 -->
        <rect x="1110" y="595" width="22" height="16" rx="3" />
        <rect x="1116" y="611" width="10" height="5" rx="1" />
      </g>

      <!-- Computer glow when packet arrives (opacity will animate) -->
      <g fill="currentColor" opacity="0">
        <rect id="glow1" x="1110" y="95" width="22" height="16" rx="3" class="comp-glow comp-glow-1" />
        <rect id="glow2" x="1110" y="195" width="22" height="16" rx="3" class="comp-glow comp-glow-2" />
        <rect id="glow3" x="1110" y="295" width="22" height="16" rx="3" class="comp-glow comp-glow-3" />
        <rect id="glow4" x="1110" y="395" width="22" height="16" rx="3" class="comp-glow comp-glow-4" />
        <rect id="glow5" x="1110" y="495" width="22" height="16" rx="3" class="comp-glow comp-glow-5" />
        <rect id="glow6" x="1110" y="595" width="22" height="16" rx="3" class="comp-glow comp-glow-6" />
      </g>

      <!-- ===== TRAVELING PACKETS ===== -->
      <!-- Each packet travels from bus top to its computer, then the next starts.
           Staggered via animation-delay. 14s total cycle for 6 computers. -->
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
      width: 42%;
      height: 100%;
      overflow: hidden;
      pointer-events: none;
      z-index: 0;
    }

    .network-bg {
      width: 100%;
      height: 100%;
      color: var(--foreground, #000);
    }

    /* ---- Packets: one travels to each computer sequentially ---- */
    .pkt {
      offset-rotate: 0deg;
      animation-name: pkt-bus;
      animation-duration: 14s;
      animation-iteration-count: infinite;
      animation-timing-function: linear;
      animation-fill-mode: both;
    }

    /*
      Each packet's offset-path: down the bus, then right along drop line to computer.
      Keyframe:
        0%-10%: travel from bus start to computer
        10%-14%: hold at computer
        14%-16%: fade out
        16%-100%: invisible (next packet's turn)
    */
    @keyframes pkt-bus {
      0%   { offset-distance: 0%; opacity: 1; }
      10%  { offset-distance: 100%; opacity: 1; }
      14%  { offset-distance: 100%; opacity: 1; }
      16%  { offset-distance: 100%; opacity: 0; }
      100% { offset-distance: 100%; opacity: 0; }
    }

    /* Paths — down the bus then right to each computer */
    .pkt-1 {
      offset-path: path('M900,55 L900,110 L1120,110');
      animation-delay: 0s;
    }
    .pkt-2 {
      offset-path: path('M900,55 L900,210 L1120,210');
      animation-delay: 2.33s;
    }
    .pkt-3 {
      offset-path: path('M900,55 L900,310 L1120,310');
      animation-delay: 4.66s;
    }
    .pkt-4 {
      offset-path: path('M900,55 L900,410 L1120,410');
      animation-delay: 7s;
    }
    .pkt-5 {
      offset-path: path('M900,55 L900,510 L1120,510');
      animation-delay: 9.33s;
    }
    .pkt-6 {
      offset-path: path('M900,55 L900,610 L1120,610');
      animation-delay: 11.66s;
    }

    /* ---- Computer glow flashes when its packet arrives ---- */
    .comp-glow {
      animation-name: comp-flash;
      animation-duration: 14s;
      animation-iteration-count: infinite;
      animation-timing-function: step-end;
      animation-fill-mode: both;
    }
    /*
       Glow timing: each computer glows just after its packet arrives.
       10% = 1.4s travel time, glow starts at 10% and lasts briefly.
    */
    .comp-glow-1 { animation-delay: 1.4s; }
    .comp-glow-2 { animation-delay: 3.73s; }
    .comp-glow-3 { animation-delay: 6.06s; }
    .comp-glow-4 { animation-delay: 8.4s; }
    .comp-glow-5 { animation-delay: 10.73s; }
    .comp-glow-6 { animation-delay: 13.06s; }

    @keyframes comp-flash {
      0%     { opacity: 0; }
      2%     { opacity: 0.4; }
      4%     { opacity: 0; }
      100%   { opacity: 0; }
    }
  `,
})
export class NetworkBgComponent {}
