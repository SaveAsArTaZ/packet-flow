import {
  Component,
  Input,
  Output,
  EventEmitter,
  AfterViewInit,
  OnDestroy,
  ElementRef,
  ViewChild,
  inject,
  NgZone,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { TypewriterService } from './typewriter.service';

let stReady = false;
try {
  gsap.registerPlugin(ScrollTrigger);
  stReady = true;
} catch {
  // jsdom unsupported
}

@Component({
  selector: 'pf-typewriter',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './typewriter.component.html',
  styleUrl: './typewriter.component.css',
})
export class TypewriterComponent implements AfterViewInit, OnDestroy {
  private zone = inject(NgZone);
  private service = inject(TypewriterService);

  // Use a setter so we can react when the parent re-binds text
  // (e.g. after a language switch via the translate pipe).
  private _text = '';

  @Input()
  set text(value: string) {
    this._text = value;
    // If the typewriter already finished animating, just swap the
    // displayed text immediately so language changes take effect.
    if (this.done()) {
      this.visibleText.set(value);
    }
  }
  get text(): string {
    return this._text;
  }

  @Input() speed = 30; // ms per character
  @Input() delay = 0; // ms delay before starting (relative to becoming active)
  @Input() cursor = true; // show blinking cursor while typing
  @Input() scrollTrigger = true; // wait for scroll vs start immediately

  /** Emitted when this typewriter finishes typing its text */
  @Output() finished = new EventEmitter<void>();

  @ViewChild('container', { static: true }) container!: ElementRef<HTMLElement>;

  // --- Signals (zone-independent change detection) ---

  /** The currently-visible portion of the text, grows character by character */
  visibleText = signal('');

  /** True while this component is the active job in the service queue */
  showCursor = signal(false);

  /** True once all characters have been revealed */
  done = signal(false);

  // --- Internal state ---

  private id = Symbol('tw');
  private tween?: gsap.core.Tween;
  private cursorTween?: gsap.core.Tween;
  private st?: ScrollTrigger;
  private resolveExecute?: () => void;
  private activeSub?: Subscription;
  private hasEnqueued = false;

  ngAfterViewInit(): void {
    // Subscribe to the service's activeId$.
    // Signals (showCursor, done, visibleText) trigger change detection
    // independently of Zone.js, so zone context doesn't matter here.
    this.activeSub = this.service.activeId$.subscribe((activeId) => {
      const isActive = activeId === this.id;

      if (isActive === this.showCursor()) return; // no change

      if (isActive) {
        this.showCursor.set(true);
        // Wait a frame for Angular to render the cursor span, then
        // start the GSAP blink outside Angular zone
        this.zone.runOutsideAngular(() => {
          requestAnimationFrame(() => this.startCursorBlink());
        });
      } else {
        this.stopCursorBlink();
        this.showCursor.set(false);
      }
    });

    // Nothing to animate if text is empty
    if (this.text.length === 0) return;

    if (this.scrollTrigger && stReady) {
      this.zone.runOutsideAngular(() => {
        this.st = ScrollTrigger.create({
          trigger: this.container.nativeElement,
          start: 'top 95%',
          once: true,
          onEnter: () => this.enqueueJob(),
        });
      });
    } else {
      this.zone.runOutsideAngular(() => {
        requestAnimationFrame(() => this.enqueueJob());
      });
    }
  }

  ngOnDestroy(): void {
    this.tween?.kill();
    this.cursorTween?.kill();
    this.st?.kill();
    this.activeSub?.unsubscribe();
    this.resolveExecute?.(); // unblock the service queue
    this.service.dequeue(this.id);
  }

  private enqueueJob(): void {
    if (this.hasEnqueued) return;
    this.hasEnqueued = true;

    this.service.enqueue({
      id: this.id,
      element: this.container.nativeElement,
      delay: this.delay,
      execute: () => this.executeTyping(),
    });
  }

  private executeTyping(): Promise<void> {
    // Snapshot the text now so the tween uses a stable value even if the
    // @Input() changes mid-animation (e.g. language switch).
    const snapshot = this.text;
    return new Promise<void>((resolve) => {
      this.resolveExecute = resolve;

      const total = snapshot.length;
      const obj = { progress: 0 };

      this.tween = gsap.to(obj, {
        progress: total,
        duration: (total * this.speed) / 1000,
        ease: 'none',
        onUpdate: () => {
          const idx = Math.floor(obj.progress);
          this.visibleText.set(snapshot.slice(0, idx));
        },
        onComplete: () => {
          this.visibleText.set(snapshot);
          // Don't stop the cursor blink here — the cursor keeps blinking
          // on the last typed word. When the next job starts, the
          // activeId$ subscription will call stopCursorBlink() for us.
          this.done.set(true);
          this.finished.emit();
          resolve();
        },
      });
    });
  }

  private startCursorBlink(): void {
    if (!this.cursor) return;
    const cursorEl =
      this.container.nativeElement.querySelector<HTMLElement>('.tw-cursor');
    if (!cursorEl) {
      // Angular hasn't rendered the cursor span yet; retry next frame
      requestAnimationFrame(() => this.startCursorBlink());
      return;
    }

    this.cursorTween = gsap.to(cursorEl, {
      opacity: 0,
      duration: 0.5,
      repeat: -1,
      yoyo: true,
      ease: 'steps(1)',
    });
  }

  private stopCursorBlink(): void {
    this.cursorTween?.kill();
    this.cursorTween = undefined;
  }
}
