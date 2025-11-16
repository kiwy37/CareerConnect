import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, tap } from 'rxjs';

export interface AuthResponse {
  token: string;
  user: {
    id: number;
    email: string;
    nume: string;
    prenume: string;
    telefon?: string;
    dataNastere: string;
    rolNume: string;
    createdAt: string;
  };
}

export interface LoginRequest {
  email: string;
  parola: string;
}

export interface RegisterRequest {
  email: string;
  parola: string;
  nume: string;
  prenume: string;
  telefon?: string;
  dataNastere: string;
  rolId: number;
}

export interface VerifyCodeDto {
  email: string;
  code: string;
}

export interface CreateUserWithCodeDto extends RegisterRequest {
  code: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const token = this.getToken();
    if (token) {
      const userData = localStorage.getItem('currentUser');
      if (userData) {
        this.currentUserSubject.next(JSON.parse(userData));
      }
    }
  }

  // Metode pentru login flow
  initiateLogin(email: string, parola: string): Observable<any> {
    const request: LoginRequest = { email, parola };
    return this.http.post(`${this.apiUrl}/login/initiate`, request);
  }

  completeLogin(email: string, code: string): Observable<AuthResponse> {
    const request: VerifyCodeDto = { email, code };
    return this.http.post<AuthResponse>(`${this.apiUrl}/login/complete`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        this.setCurrentUser(response.user);
      })
    );
  }

  // Metode pentru register flow
  initiateRegister(data: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register/initiate`, data);
  }

  finalizeRegister(data: RegisterRequest, code: string): Observable<AuthResponse> {
    const request: CreateUserWithCodeDto = { ...data, code };
    return this.http.post<AuthResponse>(`${this.apiUrl}/register/finalize`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        this.setCurrentUser(response.user);
      })
    );
  }

  // Metodă pentru resend code
  resendVerificationCode(email: string, type: string): Observable<any> {
    const request = { email, type };
    return this.http.post(`${this.apiUrl}/resend-code`, request);
  }

  // Metodă simplă pentru login (dacă vrei să o păstrezi pentru testare)
  login(email: string, parola: string): Observable<AuthResponse> {
    const request: LoginRequest = { email, parola };
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        this.setCurrentUser(response.user);
      })
    );
  }

  googleLogin(idToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/google-login`, { idToken }).pipe(
      tap(response => {
        this.setToken(response.token);
        this.setCurrentUser(response.user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  getCurrentUser(): any {
    return this.currentUserSubject.value;
  }

  private setToken(token: string): void {
    localStorage.setItem('token', token);
  }

  private setCurrentUser(user: any): void {
    localStorage.setItem('currentUser', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }
}