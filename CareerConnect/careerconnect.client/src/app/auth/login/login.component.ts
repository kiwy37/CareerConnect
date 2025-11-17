import { Component, ViewEncapsulation, OnInit } from '@angular/core';
import { AuthService, RegisterRequest } from '../auth.service';
import { Router } from '@angular/router';

declare const FB: any;
declare const google: any;

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  encapsulation: ViewEncapsulation.None,
})
export class LoginComponent implements OnInit {
  private linkedInConfig = {
    clientId: '77qbiu7uucxtzn',
    redirectUri: 'https://localhost:52623', // Must match LinkedIn settings exactly
  };

  showLogin = true;
  showVerification = false;
  isLoading = false;
  isVerifying = false;

  loginData = { email: '', password: '' };
  registerData = {
    email: '',
    password: '',
    firstname: '',
    lastname: '',
    phone: '',
    birthdate: '',
  };

  codeDigits = ['', '', '', '', '', ''];
  errors: any = {};
  successMessage = '';
  pendingVerification = false;
  currentEmail = '';
  verificationType: 'login' | 'register' = 'login';

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    // Inițializează SDK-urile pentru social login
    this.initFacebookSDK();
    this.initGoogleSDK();
    this.initLinkedInSDK();
    this.checkLinkedInCallback();
  }

  initLinkedInSDK() {
    console.log('LinkedIn auth ready');
  }

  // ==================== Facebook SDK ====================
  initFacebookSDK() {
    // Verifică dacă SDK-ul FB este deja încărcat
    if (typeof FB !== 'undefined') {
      this.configureFacebookSDK();
      return;
    }

    // Încarcă SDK-ul Facebook
    const script = document.createElement('script');
    script.src = 'https://connect.facebook.net/en_US/sdk.js';
    script.async = true;
    script.defer = true;
    script.onload = () => this.configureFacebookSDK();
    document.body.appendChild(script);
  }

  configureFacebookSDK() {
    FB.init({
      appId: '25212061275130005', // ID-ul tău din appsettings.json
      cookie: true,
      xfbml: true,
      version: 'v18.0',
    });
  }

  onFacebookLogin() {
    FB.login(
      (response: any) => {
        if (response.authResponse) {
          console.log('Facebook auth response:', response.authResponse);
          this.isLoading = true;

          FB.api(
            '/me',
            { fields: 'id,email,first_name,last_name' },
            (userInfo: any) => {
              console.log('Facebook user info:', userInfo);

              if (!userInfo.email) {
                this.isLoading = false;
                this.errors.general =
                  'Email is required from Facebook. Please grant email permission.';
                return;
              }

              this.authService
                .facebookLogin(
                  response.authResponse.accessToken,
                  userInfo.email,
                  userInfo.first_name,
                  userInfo.last_name,
                  userInfo.id
                )
                .subscribe({
                  next: (authResponse) => {
                    this.isLoading = false;
                    console.log('Facebook login successful:', authResponse);
                    this.handleSuccessfulAuth(authResponse, true); // Pass true for social login
                  },
                  error: (err) => {
                    this.isLoading = false;
                    console.error('Eroare Facebook login:', err);
                    if (err.error?.error) {
                      this.errors.general = err.error.error;
                    } else if (err.error?.message) {
                      this.errors.general = err.error.message;
                    } else {
                      this.errors.general =
                        'Facebook login failed. Please try again.';
                    }
                  },
                });
            }
          );
        }
      },
      { scope: 'public_profile' }
    );
  }

  // ==================== Google SDK ====================
  initGoogleSDK() {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => this.configureGoogleSDK();
    document.body.appendChild(script);
  }

  configureGoogleSDK() {
    google.accounts.id.initialize({
      client_id:
        '937265656787-unp24ld8lqsjbu8jh3rvmjct1i0d66ei.apps.googleusercontent.com',
      callback: (response: any) => this.handleGoogleCallback(response),
    });
  }

  onGoogleLogin() {
    google.accounts.id.prompt();
  }

  handleGoogleCallback(response: any) {
    this.isLoading = true;

    this.authService.googleLogin(response.credential).subscribe({
      next: (authResponse) => {
        this.isLoading = false;
        this.handleSuccessfulAuth(authResponse);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Eroare Google login:', err);
        this.errors.general = 'Google login failed. Please try again.';
      },
    });
  }

  // ==================== Twitter Login ====================
  onTwitterLogin() {
    // Twitter necesită OAuth pe partea de backend
    this.errors.general = 'Twitter login is not yet implemented.';
  }

  // ==================== LinkedIn Login ====================
  onLinkedInLogin() {
    const state = this.generateRandomState();
    sessionStorage.setItem('linkedin_oauth_state', state);

    // Build LinkedIn OAuth URL
    const authUrl =
      'https://www.linkedin.com/oauth/v2/authorization?' +
      `response_type=code&` +
      `client_id=${this.linkedInConfig.clientId}&` +
      `redirect_uri=${encodeURIComponent(this.linkedInConfig.redirectUri)}&` +
      `state=${state}&` +
      `scope=openid%20profile%20email`;

    // Open in same window (like Facebook does)
    window.location.href = authUrl;
  }

  private generateRandomState(): string {
    const array = new Uint32Array(2);
    window.crypto.getRandomValues(array);
    return Array.from(array, (dec) => ('0' + dec.toString(16)).substr(-2)).join(
      ''
    );
  }

  private checkLinkedInCallback() {
    // Check if we're coming back from LinkedIn
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    const error = urlParams.get('error');

    if (error) {
      this.errors.general = 'LinkedIn authentication was cancelled or failed.';
      // Clean URL
      window.history.replaceState({}, document.title, '/login');
      return;
    }

    if (code && state) {
      const savedState = sessionStorage.getItem('linkedin_oauth_state');

      if (state !== savedState) {
        this.errors.general = 'Invalid state parameter. Please try again.';
        window.history.replaceState({}, document.title, '/login');
        return;
      }

      // Clear state
      sessionStorage.removeItem('linkedin_oauth_state');

      // Show loading
      this.isLoading = true;

      // Send code to backend
      this.authService.linkedInLogin(code).subscribe({
        next: (authResponse) => {
          this.isLoading = false;
          window.history.replaceState({}, document.title, '/login');
          this.handleSuccessfulAuth(authResponse);
        },
        error: (err) => {
          this.isLoading = false;
          console.error('LinkedIn login error:', err);
          window.history.replaceState({}, document.title, '/login');

          if (err.error?.error) {
            this.errors.general = err.error.error;
          } else if (err.error?.message) {
            this.errors.general = err.error.message;
          } else {
            this.errors.general = 'LinkedIn login failed. Please try again.';
          }
        },
      });
    }
  }

  // ==================== Rest of existing methods ====================
  onLogin() {
    this.errors = {};
    this.successMessage = '';

    if (!this.validateEmail(this.loginData.email)) {
      this.errors.loginEmail = 'Please enter a valid email address.';
      return;
    }

    if (this.loginData.password.length < 6) {
      this.errors.loginPassword =
        'Password must be at least 6 characters long.';
      return;
    }

    this.isLoading = true;

    this.authService
      .initiateLogin(this.loginData.email, this.loginData.password)
      .subscribe({
        next: (response) => {
          this.isLoading = false;
          this.pendingVerification = true;
          this.currentEmail = this.loginData.email;
          this.verificationType = 'login';
          this.showVerification = true;
          this.successMessage = 'Verification code sent to your email!';
        },
        error: (err) => {
          this.isLoading = false;
          console.error('Eroare initiere login:', err);

          if (err.status === 0) {
            this.errors.loginPassword =
              'Cannot connect to server. Please check if backend is running.';
          } else if (err.error?.message) {
            this.errors.loginPassword = err.error.message;
          } else if (err.error?.errors) {
            Object.keys(err.error.errors).forEach((key) => {
              const errorKey = `login${
                key.charAt(0).toUpperCase() + key.slice(1)
              }`;
              this.errors[errorKey] = err.error.errors[key][0];
            });
          } else {
            this.errors.loginPassword = 'An error occurred. Please try again.';
          }
        },
      });
  }

  onRegister() {
    this.errors = {};
    this.successMessage = '';
    let isValid = true;

    if (!this.validateEmail(this.registerData.email)) {
      this.errors.registerEmail = 'Please enter a valid email address.';
      isValid = false;
    }

    if (this.registerData.password.length < 6) {
      this.errors.registerPassword =
        'Password must be at least 6 characters long.';
      isValid = false;
    }

    if (!this.registerData.firstname.trim()) {
      this.errors.registerFirstname = 'First name is required.';
      isValid = false;
    }

    if (!this.registerData.lastname.trim()) {
      this.errors.registerLastname = 'Last name is required.';
      isValid = false;
    }

    if (!this.validatePhone(this.registerData.phone)) {
      this.errors.registerPhone = 'Please enter a valid phone number.';
      isValid = false;
    }

    if (!this.registerData.birthdate) {
      this.errors.registerBirthdate = 'Birth date is required.';
      isValid = false;
    } else {
      const birthDate = new Date(this.registerData.birthdate);
      const today = new Date();
      const age = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();

      if (
        age < 18 ||
        (age === 18 && monthDiff < 0) ||
        (age === 18 && monthDiff === 0 && today.getDate() < birthDate.getDate())
      ) {
        this.errors.registerBirthdate = 'You must be at least 18.';
        isValid = false;
      }
    }

    if (!isValid) {
      return;
    }

    this.isLoading = true;

    const registerRequest: RegisterRequest = {
      email: this.registerData.email,
      parola: this.registerData.password,
      nume: this.registerData.lastname,
      prenume: this.registerData.firstname,
      telefon: this.registerData.phone || undefined,
      dataNastere: this.registerData.birthdate,
      rolId: 2,
    };

    this.authService.initiateRegister(registerRequest).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.pendingVerification = true;
        this.currentEmail = this.registerData.email;
        this.verificationType = 'register';
        this.showVerification = true;
        this.successMessage = 'Verification code sent to your email!';
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Eroare initiere înregistrare:', err);

        if (err.status === 0) {
          this.errors.general =
            'Cannot connect to server. Please check if backend is running.';
        } else if (err.error?.message) {
          this.errors.general = err.error.message;
        } else if (err.error?.errors) {
          Object.keys(err.error.errors).forEach((key) => {
            const errorKey = `register${
              key.charAt(0).toUpperCase() + key.slice(1)
            }`;
            this.errors[errorKey] = err.error.errors[key][0];
          });
        } else if (err.error?.title) {
          this.errors.general = err.error.title;
        } else {
          this.errors.general = 'An error occurred. Please try again.';
        }
      },
    });
  }

  verifyCode() {
    const code = this.codeDigits.join('');

    if (code.length !== 6) {
      this.errors.verification = 'Please enter the complete 6-digit code.';
      return;
    }

    this.isVerifying = true;
    this.errors.verification = '';

    if (this.verificationType === 'login') {
      this.authService.completeLogin(this.currentEmail, code).subscribe({
        next: (response) => {
          this.isVerifying = false;
          this.handleSuccessfulAuth(response);
        },
        error: (err) => {
          this.isVerifying = false;
          console.error('Eroare verificare cod login:', err);
          this.handleVerificationError(err);
        },
      });
    } else {
      const registerRequest: RegisterRequest = {
        email: this.currentEmail,
        parola: this.registerData.password,
        nume: this.registerData.lastname,
        prenume: this.registerData.firstname,
        telefon: this.registerData.phone || undefined,
        dataNastere: this.registerData.birthdate,
        rolId: 2,
      };

      this.authService.finalizeRegister(registerRequest, code).subscribe({
        next: (response) => {
          this.isVerifying = false;
          this.handleSuccessfulAuth(response);
        },
        error: (err) => {
          this.isVerifying = false;
          console.error('Eroare verificare cod register:', err);
          this.handleVerificationError(err);
        },
      });
    }
  }

  private handleSuccessfulAuth(response: any, isSocialLogin: boolean = false) {
    this.successMessage = 'Authentication successful! Redirecting...';

    setTimeout(() => {
      // If it's a social login (Facebook, Google, LinkedIn, Twitter), go to landing page
      if (isSocialLogin) {
        this.router.navigate(['/landing']);
      } else {
        // Regular login goes directly to dashboard based on role
        const role = response.user.rolNume;
        if (role === 'admin') {
          this.router.navigate(['/admin']);
        } else if (role === 'angajator') {
          this.router.navigate(['/employer']);
        } else {
          this.router.navigate(['/employee']);
        }
      }
    }, 1500);
  }

  private handleVerificationError(err: any) {
    if (err.status === 0) {
      this.errors.verification =
        'Cannot connect to server. Please check if backend is running.';
    } else if (err.error?.message) {
      this.errors.verification = err.error.message;
    } else if (err.error?.errors) {
      this.errors.verification = Object.values(err.error.errors)
        .flat()
        .join(', ');
    } else {
      this.errors.verification = 'Invalid verification code. Please try again.';
    }
  }

  resendCode() {
    this.errors.verification = '';

    this.authService
      .resendVerificationCode(this.currentEmail, this.verificationType)
      .subscribe({
        next: () => {
          this.successMessage = 'Verification code resent successfully!';
        },
        error: (err) => {
          console.error('Eroare retrimitere cod:', err);
          this.errors.verification = 'Failed to resend code. Please try again.';
        },
      });
  }

  backToLogin() {
    this.showVerification = false;
    this.showLogin = true;
    this.pendingVerification = false;
    this.codeDigits = ['', '', '', '', '', ''];
    this.errors = {};
    this.successMessage = '';
  }

  onCodeInput(index: number, event: any) {
    const value = event.target.value;

    if (!/^\d*$/.test(value)) {
      event.target.value = '';
      this.codeDigits[index] = '';
      return;
    }

    if (value.length === 1 && index < 5) {
      this.codeDigits[index] = value;
      const inputs = document.querySelectorAll('.code-input');
      (inputs[index + 1] as HTMLElement).focus();
    } else if (value.length === 1) {
      this.codeDigits[index] = value;
    }
  }

  onCodeKeydown(index: number, event: any) {
    if (event.key === 'Backspace' && !event.target.value && index > 0) {
      const inputs = document.querySelectorAll('.code-input');
      (inputs[index - 1] as HTMLElement).focus();
    }
  }

  onCodePaste(event: ClipboardEvent) {
    event.preventDefault();
    const pasteData = event.clipboardData?.getData('text').trim();

    if (pasteData && /^\d{6}$/.test(pasteData)) {
      const digits = pasteData.split('');
      this.codeDigits = [...digits];

      setTimeout(() => {
        const inputs = document.querySelectorAll('.code-input');
        (inputs[5] as HTMLElement).focus();
      }, 0);
    }
  }

  private validateEmail(email: string): boolean {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
  }

  private validatePhone(phone: string): boolean {
    const re = /^[0-9+\-\s()]{10,}$/;
    return re.test(phone);
  }
}
