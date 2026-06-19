import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  loginMock() {
    localStorage.setItem('token', 'mock-token');
    localStorage.setItem('role', 'PM');
  }

  loginApi(email: string, password: string) {
    return this.http.post(`${this.apiUrl}/Auth/login`, {
      email,
      password,
    });
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
  }

  isLoggedIn() {
    return !!localStorage.getItem('token');
  }

  getRole() {
    return localStorage.getItem('role');
  }
}