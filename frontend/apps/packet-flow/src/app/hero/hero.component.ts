import { Component, AfterViewInit, ElementRef, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TypewriterComponent } from '../typewriter/typewriter.component';
import { TranslatePipe } from '@ngx-translate/core';
import { NetworkBgComponent } from '../network-bg/network-bg.component';
import gsap from 'gsap';

@Component({
  selector: 'pf-hero',
  standalone: true,
  imports: [CommonModule, TranslatePipe, TypewriterComponent, NetworkBgComponent],
  templateUrl: './hero.component.html',
  styleUrl: './hero.component.css',
})
export class HeroComponent implements AfterViewInit {
  @ViewChild('actions', { static: true }) actions!: ElementRef<HTMLElement>;
  @ViewChild('decorLine', { static: true }) decorLine!: ElementRef<HTMLElement>;

  private decorPlayed = signal(false);

  ngAfterViewInit(): void {}

  onHeroTypingComplete(): void {
    if (this.decorPlayed()) return;
    this.decorPlayed.set(true);

    // Remove invisible class so elements are in the DOM flow, then animate in
    const line = this.decorLine.nativeElement;
    const btns = this.actions.nativeElement;

    line.classList.remove('invisible');
    btns.classList.remove('invisible');

    const tl = gsap.timeline();

    tl.fromTo(line,
      { scaleX: 0 },
      { scaleX: 1, duration: 0.6, ease: 'power3.out', transformOrigin: 'left center' },
    );

    tl.fromTo(btns,
      { opacity: 0, y: 8 },
      { opacity: 1, y: 0, duration: 0.5, ease: 'power2.out' },
      '-=0.2',
    );
  }
}
