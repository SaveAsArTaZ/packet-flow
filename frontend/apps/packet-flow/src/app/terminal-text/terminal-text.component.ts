import {
  Component,
  Input,
  AfterViewInit,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import gsap from 'gsap';

@Component({
  selector: 'pf-terminal-text',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './terminal-text.component.html',
  styleUrl: './terminal-text.component.css',
})
export class TerminalTextComponent implements AfterViewInit, OnDestroy {
  @Input() lines: string[] = [];
  @Input() prefix = '$ ';

  displayedLines: Array<{ text: string; done: boolean }> = [];
  currentLine = 0;

  private tweens: gsap.core.Tween[] = [];
  private cursorTween!: gsap.core.Tween;

  ngAfterViewInit(): void {
    this.displayedLines = this.lines.map(() => ({ text: '', done: false }));
    this.typeNext(0);

    // Blink cursor
    this.cursorTween = gsap.to('.cursor', {
      opacity: 0,
      duration: 0.55,
      repeat: -1,
      yoyo: true,
      ease: 'steps(1)',
    });
  }

  ngOnDestroy(): void {
    this.tweens.forEach((t) => t.kill());
    this.cursorTween?.kill();
  }

  private typeNext(index: number): void {
    if (index >= this.lines.length) return;

    this.currentLine = index;
    const fullText = `${this.prefix}${this.lines[index]}`;
    const obj = { progress: 0 };

    const tween = gsap.to(obj, {
      progress: fullText.length,
      duration: fullText.length * 0.045,
      ease: 'none',
      delay: index === 0 ? 0.6 : 0.35,
      onUpdate: () => {
        const chars = Math.floor(obj.progress);
        this.displayedLines[index] = {
          text: fullText.slice(0, chars),
          done: chars >= fullText.length,
        };
      },
      onComplete: () => {
        this.displayedLines[index] = { text: fullText, done: true };
        this.typeNext(index + 1);
      },
    });

    this.tweens.push(tween);
  }
}
