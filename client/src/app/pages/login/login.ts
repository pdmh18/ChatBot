import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  login() {
    this.authService.loginMock();
    this.router.navigate(['/dashboard']);
  }
}