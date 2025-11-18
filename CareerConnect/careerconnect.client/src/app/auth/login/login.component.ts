import { Component, ViewEncapsulation, OnInit } from '@angular/core';
import { AuthService, RegisterRequest } from '../auth.service';
import { Router } from '@angular/router';

declare const FB: any;
declare const google: any;

type ViewMode = 'login' | 'register' | 'verification' | 'forgotPassword' | 'resetPassword';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  encapsulation: ViewEncapsulation.None,
})
export class LoginComponent implements OnInit {
  private linkedInConfig = {
    clientId: '77qbiu7uucxtzn',
    redirectUri: 'https://localhost:52623',
  };

  currentView: ViewMode = 'login';
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

  forgotPasswordData = { email: '' };
  resetPasswordData = { newPassword: '', confirmPassword: '' };

  codeDigits = ['', '', '', '', '', ''];
  errors: any = {};
  successMessage = '';
  pendingVerification = false;
  currentEmail = '';
  verificationType: 'Login' | 'Register' | 'ResetPassword' = 'Login';

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.initFacebookSDK();
    this.initGoogleSDK();
    this.initLinkedInSDK();
    this.checkLinkedInCallback();
  }

  // View management
  showView(view: ViewMode) {
    this.currentView = view;
    this.errors = {};
    this.successMessage = '';
    this.codeDigits = ['', '', '', '', '', ''];
  }

  get showLogin(): boolean {
    return this.currentView === 'login';
  }

  get showRegister(): boolean {
    return this.currentView === 'register';
  }

  get showVerification(): boolean {
    return this.currentView === 'verification';
  }

  get showForgotPassword(): boolean {
    return this.currentView === 'forgotPassword';
  }

  get showResetPassword(): boolean {
    return this.currentView === 'resetPassword';
  }

  initLinkedInSDK() {
    console.log('LinkedIn auth ready');
  }

  // ==================== Facebook SDK ====================
  initFacebookSDK() {
    if (typeof FB !== 'undefined') {
      this.configureFacebookSDK();
      return;
    }

    const script = document.createElement('script');
    script.src = 'https://connect.facebook.net/en_US/sdk.js';
    script.async = true;
    script.defer = true;
    script.onload = () => this.configureFacebookSDK();
    document.body.appendChild(script);
  }

  configureFacebookSDK() {
    FB.init({
      appId: '25212061275130005',
      cookie: true,
      xfbml: true,
      version: 'v18.0',
    });
  }

  onFacebookLogin() {
    FB.login(
      (response: any) => {
        if (response.authResponse) {
          this.isLoading = true;

          FB.api(
            '/me',
            { fields: 'id,first_name,last_name' },
            (userInfo: any) => {
              const tempEmail = `facebook_${userInfo.id}@careerconnect.temp`;

              this.authService
                .facebookLogin(
                  response.authResponse.accessToken,
                  tempEmail,
                  userInfo.first_name,
                  userInfo.last_name,
                  userInfo.id
                )
                .subscribe({
                  next: (authResponse) => {
                    this.isLoading = false;
                    this.handleSuccessfulAuth(authResponse, true);
                  },
                  error: (err) => {
                    this.isLoading = false;
                    console.error('Eroare Facebook login:', err);
                    this.errors.general = err.error?.error || err.error?.message || 'Facebook login failed.';
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
        this.handleSuccessfulAuth(authResponse, true);
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
    this.errors.general = 'Twitter login is not yet implemented.';
  }

  // ==================== LinkedIn Login ====================
  onLinkedInLogin() {
    const state = this.generateRandomState();
    sessionStorage.setItem('linkedin_oauth_state', state);

    const authUrl =
      'https://www.linkedin.com/oauth/v2/authorization?' +
      `response_type=code&` +
      `client_id=${this.linkedInConfig.clientId}&` +
      `redirect_uri=${encodeURIComponent(this.linkedInConfig.redirectUri)}&` +
      `state=${state}&` +
      `scope=openid%20profile%20email`;

    window.location.href = authUrl;
  }

  private generateRandomState(): string {
    const array = new Uint32Array(2);
    window.crypto.getRandomValues(array);
    return Array.from(array, (dec) => ('0' + dec.toString(16)).substr(-2)).join('');
  }

  private checkLinkedInCallback() {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    const error = urlParams.get('error');

    if (error) {
      this.errors.general = 'LinkedIn authentication was cancelled or failed.';
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

      sessionStorage.removeItem('linkedin_oauth_state');
      this.isLoading = true;

      this.authService.linkedInLogin(code).subscribe({
        next: (authResponse) => {
          this.isLoading = false;
          window.history.replaceState({}, document.title, '/login');
          this.handleSuccessfulAuth(authResponse, true);
        },
        error: (err) => {
          this.isLoading = false;
          console.error('LinkedIn login error:', err);
          window.history.replaceState({}, document.title, '/login');
          this.errors.general = err.error?.error || err.error?.message || 'LinkedIn login failed.';
        },
      });
    }
  }

  // ==================== Login ====================
  onLogin() {
    this.errors = {};
    this.successMessage = '';

    if (!this.validateEmail(this.loginData.email)) {
      this.errors.loginEmail = 'Please enter a valid email address.';
      return;
    }

    if (this.loginData.password.length < 6) {
      this.errors.loginPassword = 'Password must be at least 6 characters long.';
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
          this.verificationType = 'Login';
          this.showView('verification');
          this.successMessage = 'Verification code sent to your email!';
        },
        error: (err) => {
          this.isLoading = false;
          console.error('Eroare initiere login:', err);

          if (err.status === 0) {
            this.errors.loginPassword = 'Cannot connect to server.';
          } else if (err.error?.message) {
            this.errors.loginPassword = err.error.message;
          } else {
            this.errors.loginPassword = 'An error occurred. Please try again.';
          }
        },
      });
  }

  // ==================== Register ====================
  onRegister() {
    this.errors = {};
    this.successMessage = '';
    let isValid = true;

    if (!this.validateEmail(this.registerData.email)) {
      this.errors.registerEmail = 'Please enter a valid email address.';
      isValid = false;
    }

    if (this.registerData.password.length < 6) {
      this.errors.registerPassword = 'Password must be at least 6 characters long.';
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

    if (this.registerData.phone && !this.validatePhone(this.registerData.phone)) {
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
        this.verificationType = 'Register';
        this.showView('verification');
        this.successMessage = 'Verification code sent to your email!';
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Eroare initiere înregistrare:', err);

        if (err.error?.message) {
          this.errors.general = err.error.message;
        } else if (err.error?.errors) {
          Object.keys(err.error.errors).forEach((key) => {
            const errorKey = `register${key.charAt(0).toUpperCase() + key.slice(1)}`;
            this.errors[errorKey] = err.error.errors[key][0];
          });
        } else {
          this.errors.general = 'An error occurred. Please try again.';
        }
      },
    });
  }

  // ==================== Forgot Password ====================
  onForgotPassword() {
    this.errors = {};
    this.successMessage = '';

    if (!this.validateEmail(this.forgotPasswordData.email)) {
      this.errors.forgotEmail = 'Please enter a valid email address.';
      return;
    }

    this.isLoading = true;

    this.authService.initiateForgotPassword(this.forgotPasswordData.email).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.currentEmail = this.forgotPasswordData.email;
        this.verificationType = 'ResetPassword';
        this.showView('verification');
        this.successMessage = 'Verification code sent to your email!';
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Eroare forgot password:', err);

        if (err.error?.message) {
          this.errors.forgotEmail = err.error.message;
        } else {
          this.errors.forgotEmail = 'An error occurred. Please try again.';
        }
      },
    });
  }

  // ==================== Verify Code ====================
  verifyCode() {
    const code = this.codeDigits.join('');

    if (code.length !== 6) {
      this.errors.verification = 'Please enter the complete 6-digit code.';
      return;
    }

    this.isVerifying = true;
    this.errors.verification = '';

    if (this.verificationType === 'ResetPassword') {
      this.authService.verifyResetCode(this.currentEmail, code).subscribe({
        next: (response) => {
          this.isVerifying = false;
          this.showView('resetPassword');
          this.successMessage = 'Code verified! Please enter your new password.';
        },
        error: (err) => {
          this.isVerifying = false;
          console.error('Eroare verificare cod reset:', err);
          this.handleVerificationError(err);
        },
      });
    } else if (this.verificationType === 'Login') {
      this.authService.completeLogin(this.currentEmail, code).subscribe({
        next: (response) => {
          this.isVerifying = false;
          this.router.navigate(['/landing']);
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
          this.router.navigate(['/landing']);
        },
        error: (err) => {
          this.isVerifying = false;
          console.error('Eroare verificare cod register:', err);
          this.handleVerificationError(err);
        },
      });
    }
  }

  // ==================== Reset Password ====================
  onResetPassword() {
    this.errors = {};

    if (this.resetPasswordData.newPassword.length < 6) {
      this.errors.newPassword = 'Password must be at least 6 characters long.';
      return;
    }

    if (this.resetPasswordData.newPassword !== this.resetPasswordData.confirmPassword) {
      this.errors.confirmPassword = 'Passwords do not match.';
      return;
    }

    this.isLoading = true;
    const code = this.codeDigits.join('');

    this.authService
      .resetPassword(this.currentEmail, code, this.resetPasswordData.newPassword)
      .subscribe({
        next: (response) => {
          this.isLoading = false;
          this.successMessage = 'Password reset successfully! Redirecting to login...';

          setTimeout(() => {
            this.showView('login');
            this.resetPasswordData = { newPassword: '', confirmPassword: '' };
            this.codeDigits = ['', '', '', '', '', ''];
          }, 2000);
        },
        error: (err) => {
          this.isLoading = false;
          console.error('Eroare reset parolă:', err);

          if (err.error?.message) {
            this.errors.general = err.error.message;
          } else {
            this.errors.general = 'Failed to reset password. Please try again.';
          }
        },
      });
  }

  // ==================== Helper Methods ====================
  private handleSuccessfulAuth(response: any, isSocialLogin: boolean = false) {
    this.successMessage = 'Authentication successful! Redirecting...';

    setTimeout(() => {
      this.router.navigate(['/landing']);
    }, 1500);
  }

  private handleVerificationError(err: any) {
    if (err.status === 0) {
      this.errors.verification = 'Cannot connect to server.';
    } else if (err.error?.message) {
      this.errors.verification = err.error.message;
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
    this.showView('login');
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

  private validateEmail(email: string): boolean {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
  }

  private validatePhone(phone: string): boolean {
    const re = /^[0-9+\-\s()]{10,}$/;
    return re.test(phone);
  }
}