import { Component, ViewEncapsulation } from '@angular/core';
import { AuthService, RegisterRequest } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class LoginComponent {
  showLogin = true;
  showVerification = false;
  isLoading = false;
  
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
  successMessage = '';
  
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

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

    this.authService.login(this.loginData.email, this.loginData.password).subscribe({
      next: (response) => {
        this.isLoading = false;
        
        // Redirect based on role
        const role = response.user.rolNume;
        if (role === 'admin') {
          this.router.navigate(['/admin']);
        } else if (role === 'angajator') {
          this.router.navigate(['/angajator']);
        } else {
          this.router.navigate(['/angajat']);
        }
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Eroare login:', err);
        
        if (err.status === 401) {
          this.errors.loginPassword = 'Email or password incorrect';
        } else if (err.error?.message) {
          this.errors.loginPassword = err.error.message;
        } else {
          this.errors.loginPassword = 'An erorr occured. Please try again.';
        }
      }
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
      
      if (age < 18 || (age === 18 && monthDiff < 0) || 
          (age === 18 && monthDiff === 0 && today.getDate() < birthDate.getDate())) {
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
      rolId: 2 // Default: angajat
    };


    this.authService.register(registerRequest).subscribe({
      next: (response) => {
        this.isLoading = false;
        
        this.successMessage = 'Cont creat cu succes! Redirecting...';
        
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
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Eroare completă înregistrare:', err);
        console.error('Status:', err.status);
        console.error('Error body:', err.error);
        
        if (err.status === 0) {
          this.errors.general = 'Nu se poate conecta la server. Verificați dacă backend-ul rulează.';
        } else if (err.error?.message) {
          this.errors.general = err.error.message;
        } else if (err.error?.errors) {
          Object.keys(err.error.errors).forEach(key => {
            const errorKey = `register${key.charAt(0).toUpperCase() + key.slice(1)}`;
            this.errors[errorKey] = err.error.errors[key][0];
          });
        } else if (err.error?.title) {
          this.errors.general = err.error.title;
        } else {
          this.errors.general = 'A apărut o eroare. Vă rugăm încercați din nou.';
        }
      }
    });
  }

  verifyCode() {
    const code = this.codeDigits.join('');
    alert('Funcționalitatea de verificare email nu este implementată încă.');
  }

  resendCode() {
    alert('Funcționalitatea de retrimitere cod nu este implementată încă.');
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
}