import { Component, ViewEncapsulation } from '@angular/core';
import { AuthService, RegisterRequest } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  encapsulation: ViewEncapsulation.None,
})
export class LoginComponent {
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

  onLogin() {
    this.errors = {};
    this.successMessage = '';

    // Validare
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
          this.verificationType = 'login';
          this.showVerification = true;
          this.successMessage = 'Verification code sent to your email!';
        },
        error: (err) => {
          this.isLoading = false;
          console.error('Eroare initiere login:', err);
          
          if (err.status === 0) {
            this.errors.loginPassword = 'Cannot connect to server. Please check if backend is running.';
          } else if (err.error?.message) {
            this.errors.loginPassword = err.error.message;
          } else if (err.error?.errors) {
            Object.keys(err.error.errors).forEach((key) => {
              const errorKey = `login${key.charAt(0).toUpperCase() + key.slice(1)}`;
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

    if (!this.validatePhone(this.registerData.phone)) {
      this.errors.registerPhone = 'Please enter a valid phone number.';
      isValid = false;
    }

    if (!this.registerData.birthdate) {
      this.errors.registerBirthdate = 'Birth date is required.';
      isValid = false;
    } else {
      // Verifică dacă utilizatorul are cel puțin 18 ani
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
      rolId: 2, // Default: angajat
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
        console.error('Status:', err.status);
        console.error('Error body:', err.error);

        if (err.status === 0) {
          this.errors.general = 'Cannot connect to server. Please check if backend is running.';
        } else if (err.error?.message) {
          this.errors.general = err.error.message;
        } else if (err.error?.errors) {
          Object.keys(err.error.errors).forEach((key) => {
            const errorKey = `register${key.charAt(0).toUpperCase() + key.slice(1)}`;
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
        }
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
        }
      });
    }
  }

  private handleSuccessfulAuth(response: any) {
    this.successMessage = 'Authentication successful! Redirecting...';
    
    setTimeout(() => {
      const role = response.user.rolNume;
      if (role === 'admin') {
        this.router.navigate(['/admin']);
      } else if (role === 'angajator') {
        this.router.navigate(['/angajator']);
      } else {
        this.router.navigate(['/angajat']);
      }
    }, 1500);
  }

  private handleVerificationError(err: any) {
    if (err.status === 0) {
      this.errors.verification = 'Cannot connect to server. Please check if backend is running.';
    } else if (err.error?.message) {
      this.errors.verification = err.error.message;
    } else if (err.error?.errors) {
      this.errors.verification = Object.values(err.error.errors).flat().join(', ');
    } else {
      this.errors.verification = 'Invalid verification code. Please try again.';
    }
  }

  resendCode() {
    this.errors.verification = '';
    
    this.authService.resendVerificationCode(this.currentEmail, this.verificationType).subscribe({
      next: () => {
        this.successMessage = 'Verification code resent successfully!';
      },
      error: (err) => {
        console.error('Eroare retrimitere cod:', err);
        this.errors.verification = 'Failed to resend code. Please try again.';
      }
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
    
    // Permite doar cifre
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
      
      // Focus pe ultimul input
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