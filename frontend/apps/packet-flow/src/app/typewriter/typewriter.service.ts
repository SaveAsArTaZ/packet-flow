import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface TypewriterJob {
  /** Unique identifier for this job */
  id: symbol;
  /** Start typing. Returns promise that resolves when complete or cancelled. */
  execute: () => Promise<void>;
  /** DOM element for sorting by document position */
  element: HTMLElement;
  /** Delay in ms before starting this job, relative to becoming active */
  delay: number;
}

@Injectable({ providedIn: 'root' })
export class TypewriterService {
  private queue: TypewriterJob[] = [];
  private processing = false;

  /** The ID of the currently active typewriter job, or null if idle */
  readonly activeId$ = new BehaviorSubject<symbol | null>(null);

  /** Add a job to the queue. Processing starts automatically if idle. */
  enqueue(job: TypewriterJob): void {
    // Guard against duplicate registrations
    if (this.queue.some((j) => j.id === job.id)) return;
    if (this.activeId$.value === job.id) return;

    this.queue.push(job);
    this.sortByDOMOrder();

    if (!this.processing) {
      this.processNext();
    }
  }

  /** Remove a job from the queue. Safe to call at any time. */
  dequeue(id: symbol): void {
    this.queue = this.queue.filter((j) => j.id !== id);
  }

  private sortByDOMOrder(): void {
    this.queue.sort((a, b) => {
      const rectA = a.element.getBoundingClientRect();
      const rectB = b.element.getBoundingClientRect();
      // Primary: top-to-bottom (with 10px tolerance for same-row elements)
      if (Math.abs(rectA.top - rectB.top) > 10) {
        return rectA.top - rectB.top;
      }
      // Secondary: left-to-right
      return rectA.left - rectB.left;
    });
  }

  private async processNext(): Promise<void> {
    if (this.queue.length === 0) {
      this.processing = false;
      // Don't clear activeId$ — the cursor keeps blinking on the last job
      return;
    }

    this.processing = true;
    const job = this.queue.shift()!;
    // Move the cursor to the new job. The previous job's cursor is
    // naturally hidden because its showCursor is bound to activeId$.
    this.activeId$.next(job.id);

    try {
      if (job.delay > 0) {
        await new Promise((resolve) => setTimeout(resolve, job.delay));
      }
      await job.execute();
    } finally {
      // Don't clear activeId$ — the next processNext() call will set
      // it to the next job's ID, or if the queue is empty the cursor
      // stays on this (last) job and keeps blinking.
    }

    // Brief pause between consecutive jobs
    await new Promise((resolve) => setTimeout(resolve, 80));
    this.processNext();
  }
}
