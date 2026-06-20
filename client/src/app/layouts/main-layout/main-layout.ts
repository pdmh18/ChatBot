import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth';
import { FloatingAiBot } from '../../components/floating-ai-bot/floating-ai-bot';

@Component({
  selector: 'app-main-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, FloatingAiBot],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}