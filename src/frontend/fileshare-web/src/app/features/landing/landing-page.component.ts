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
    { icon: '⚖️', label: 'Legal teams delivering contracts with verifiable timestamps', tag: 'Legal' },
    { icon: '🎓', label: 'Academic submissions with tamper-proof proof of date', tag: 'Academia' },
    { icon: '🗞️', label: 'Journalists protecting source material chain of custody', tag: 'Journalism' },
    { icon: '📊', label: 'Consultants delivering reports with audit-grade receipts', tag: 'Consulting' },
    { icon: '🎨', label: 'Designers handing off assets with integrity attestation', tag: 'Design' },
    { icon: '🤝', label: 'External collaborators without sharing credentials', tag: 'Collaboration' }
  ] as const;

  readonly plans = [
    {
      id: 'free',
      name: 'Free',
      price: '$0',
      period: 'forever',
      tagline: 'Quick shares, no strings attached.',
      highlight: false,
      features: [
        { text: 'Files up to 50 MB', enabled: true },
        { text: '3 downloads per link', enabled: true },
        { text: '24-hour retention', enabled: true },
        { text: 'Proof of Transfer (basic)', enabled: true },
        { text: 'No API access', enabled: false }
      ],
      cta: 'Start for free',
      ctaHint: 'No signup required'
    },
    {
      id: 'pro',
      name: 'Pro',
      price: '$9',
      period: '/month',
      tagline: 'For professionals sharing larger assets.',
      highlight: true,
      features: [
        { text: 'Files up to 5 GB', enabled: true },
        { text: 'Unlimited downloads', enabled: true },
        { text: '7-day retention', enabled: true },
        { text: 'Proof of Transfer HD (Ed25519)', enabled: true },
        { text: 'Priority ledger anchoring', enabled: true }
      ],
      cta: 'Upgrade with crypto',
      ctaHint: 'Pay in ETH · USDC · BTC'
    },
    {
      id: 'business',
      name: 'Business',
      price: '$29',
      period: '/month',
      tagline: 'For teams with compliance and audit needs.',
      highlight: false,
      features: [
        { text: 'Unlimited size', enabled: true },
        { text: '30-day retention', enabled: true },
        { text: 'API & webhooks', enabled: true },
        { text: 'Legal-grade Proof certificates', enabled: true },
        { text: 'Dedicated ledger node', enabled: true }
      ],
      cta: 'Contact sales',
      ctaHint: 'Custom invoicing available'
    }
  ] as const;

  readonly proofSteps = [
    {
      number: '01',
      title: 'Hash at rest',
      description: 'The SHA-256 of your file is computed before upload — never leaves the browser.'
    },
    {
      number: '02',
      title: 'Sign & anchor',
      description: 'An Ed25519 signature plus a ledger block hash are bound to the transfer event.'
    },
    {
      number: '03',
      title: 'Deliver with receipt',
      description: 'Sender and recipient both receive a Proof of Transfer certificate, verifiable forever.'
    }
  ] as const;
}
