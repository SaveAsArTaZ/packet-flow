import { Component, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UiButtonComponent } from '../ui-button/ui-button.component';
import { TypewriterComponent } from '../typewriter/typewriter.component';
import { NetworkBgComponent } from '../network-bg/network-bg.component';
import gsap from 'gsap';

@Component({
  selector: 'pf-hero',
  standalone: true,
  imports: [CommonModule, UiButtonComponent, TypewriterComponent, NetworkBgComponent],
  templateUrl: './hero.component.html',
  styleUrl: './hero.component.css',
})
export class HeroComponent implements AfterViewInit {
  @ViewChild('actions', { static: true }) actions!: ElementRef<HTMLElement>;
  @ViewChild('decorLine', { static: true }) decorLine!: ElementRef<HTMLElement>;

  private decorPlayed = false;

  ngAfterViewInit(): void {
    // Decor line + buttons are triggered by (finished) on the last typewriter.
    // The onHeroTypingComplete() method is bound in the template.
  }

  onHeroTypingComplete(): void {
    if (this.decorPlayed) return;
    this.decorPlayed = true;

    const tl = gsap.timeline();

    tl.fromTo(
      this.decorLine.nativeElement,
      { scaleX: 0 },
      { scaleX: 1, duration: 0.6, ease: 'power3.out', transformOrigin: 'left' },
    );

    tl.fromTo(
      this.actions.nativeElement,
      { opacity: 0, y: 8 },
      { opacity: 1, y: 0, duration: 0.5, ease: 'power2.out' },
      '-=0.2',
    );
  }
}
