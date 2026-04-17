import {
  AfterViewInit,
  Component,
  ElementRef,
  NgZone,
  OnDestroy,
  QueryList,
  ViewChildren
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { FloatingBackgroundComponent } from '../../shared/ui/floating-background.component';

@Component({
  selector: 'app-landing-page',
  imports: [RouterLink, FloatingBackgroundComponent],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss'
})
export class LandingPageComponent implements AfterViewInit, OnDestroy {
  @ViewChildren('reveal') revealElements!: QueryList<ElementRef<HTMLElement>>;

  private observer?: IntersectionObserver;

  constructor(private zone: NgZone) {}

  ngAfterViewInit(): void {
    this.zone.runOutsideAngular(() => {
      this.observer = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              entry.target.classList.add('visible');
              this.observer?.unobserve(entry.target);
            }
          });
        },
        { threshold: 0.12, rootMargin: '0px 0px -40px 0px' }
      );

      this.revealElements.forEach((el) => this.observer?.observe(el.nativeElement));

      // Also observe elements without the QueryList (via class)
      document.querySelectorAll('.reveal').forEach((el) => this.observer?.observe(el));
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  readonly steps = [
    {
      number: '01',
      icon: 'upload',
      title: 'Upload your file',
      description: 'Drag and drop any file type. We handle the rest securely.',
      color: 'teal'
    },
    {
      number: '02',
      icon: 'settings',
      title: 'Configure access',
      description: 'Set an expiration date and limit how many times it can be downloaded.',
      color: 'emerald'
    },
    {
      number: '03',
      icon: 'link',
      title: 'Share the link',
      description: 'Get a unique temporary link ready to share with anyone.',
      color: 'teal'
    }
  ] as const;

  readonly features = [
    {
      icon: 'clock',
      title: 'Auto-expiration',
      description: 'Links automatically deactivate at your chosen date and time.',
      badge: 'Time-based'
    },
    {
      icon: 'download',
      title: 'Download limits',
      description: 'Control exactly how many times a file can be retrieved.',
      badge: 'Count-based'
    },
    {
      icon: 'shield',
      title: 'Secure delivery',
      description: 'Each file is accessed via a unique, unguessable token.',
      badge: 'Token-protected'
    },
    {
      icon: 'zap',
      title: 'Instant sharing',
      description: 'No account required. Upload and get your link in seconds.',
      badge: 'Zero friction'
    }
  ] as const;

  readonly useCases = [
    { icon: '📦', label: 'Send large assets to clients', tag: 'Client delivery' },
    { icon: '🔒', label: 'Share sensitive documents once', tag: 'One-time access' },
    { icon: '🎓', label: 'Distribute course materials', tag: 'Education' },
    { icon: '🤝', label: 'Collaborate with external teams', tag: 'Collaboration' },
    { icon: '🎨', label: 'Preview design files before handoff', tag: 'Design' },
    { icon: '📊', label: 'Share reports with limited access', tag: 'Business' }
  ] as const;
}
