import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login.component';
import { AuthGuard } from './auth/auth.guard';
import { AdminDashboardComponent } from './auth/dashboard/admin-dashboard/admin-dashboard.component';
import { EmployerDashboardComponent } from './auth/dashboard/employer-dashboard/employer-dashboard.component';
import { EmployeeDashboardComponent } from './auth/dashboard/employee-dashboard/employee-dashboard.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { 
    path: 'admin', 
    component: AdminDashboardComponent,
    canActivate: [AuthGuard],
    data: { role: 'admin' }
  },
  { 
    path: 'employer', 
    component: EmployerDashboardComponent,
    canActivate: [AuthGuard],
    data: { role: 'angajator' }
  },
  { 
    path: 'employee', 
    component: EmployeeDashboardComponent,
    canActivate: [AuthGuard],
    data: { role: 'angajat' }
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}