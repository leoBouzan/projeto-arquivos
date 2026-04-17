import { Component, HostListener, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly isScrolled = signal(false);

  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled.set(window.scrollY > 12);
  }
}
