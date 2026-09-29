import { Component, inject, OnInit } from '@angular/core';
import {
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import { HealthService } from './core/services/health.service';

type ApiStatus = 'checking' | 'online' | 'offline';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly healthService = inject(HealthService);

  apiStatus: ApiStatus = 'checking';

  ngOnInit(): void {
    this.checkApi();
  }

  private checkApi(): void {
    this.apiStatus = 'checking';

    this.healthService.check().subscribe({
      next: () => {
        this.apiStatus = 'online';
      },
      error: () => {
        this.apiStatus = 'offline';
      }
    });
  }
}
