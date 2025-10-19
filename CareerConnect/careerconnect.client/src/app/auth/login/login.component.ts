import { Component, ViewEncapsulation } from '@angular/core';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  encapsulation: ViewEncapsulation.None  // ← IMPORTANT!
})
export class LoginComponent {
  showLogin = true;
  showVerification = false;
  
  loginData = { email: '', password: '' };
  registerData = {
    email: '',
    password: '',
    firstname: '',
    lastname: '',
    phone: '',
    birthdate: ''
  };
  
  codeDigits = ['', '', '', '', '', ''];
  errors: any = {};
  
  constructor(private authService: AuthService) {}

  onLogin() {
    this.errors = {};
    
    if (!this.validateEmail(this.loginData.email)) {
      this.errors.loginEmail = 'Please enter a valid email address.';
      return;
    }
    
    if (this.loginData.password.length < 6) {
      this.errors.loginPassword = 'Password must be at least 6 characters long.';
      return;
    }

    this.authService.login(this.loginData.email, this.loginData.password).subscribe({
      next: () => {
        this.showVerification = true;
        this.generateVerificationCode();
      },
      error: (err) => {
        this.errors.loginPassword = err.error?.message || 'Invalid credentials';
      }
    });
  }

  onRegister() {
    this.errors = {};
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
    }
    
    if (!isValid) return;
    
    this.showVerification = true;
    this.generateVerificationCode();
  }

  verifyCode() {
    const code = this.codeDigits.join('');
    
    this.authService.verifyCode(this.loginData.email, code).subscribe({
      next: () => {
        alert('Login successful!');
      },
      error: () => {
        alert('Invalid verification code');
      }
    });
  }

  resendCode() {
    this.authService.resendCode(this.loginData.email).subscribe(() => {
      alert('Code resent to your email');
      this.generateVerificationCode();
    });
  }

  backToLogin() {
    this.showVerification = false;
    this.showLogin = true;
    this.codeDigits = ['', '', '', '', '', ''];
  }

  onCodeInput(index: number, event: any) {
    const value = event.target.value;
    if (value.length === 1 && index < 5) {
      const inputs = document.querySelectorAll('.code-input');
      (inputs[index + 1] as HTMLElement).focus();
    }
  }

  onCodeKeydown(index: number, event: any) {
    if (event.key === 'Backspace' && index > 0 && event.target.value === '') {
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

  private generateVerificationCode() {
    const code = Math.floor(100000 + Math.random() * 900000).toString();
    console.log(`Verification code: ${code}`);
    alert(`Verification code (simulated): ${code}`);
  }
}