import { Component, computed, inject } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';
import { NgIf } from '@angular/common';

@Component({
    selector: 'app-login-button',
    standalone: true,
    imports: [NgIf],
    templateUrl: './login-button.component.html',
    styleUrl: './login-button.component.css'
})
export class LoginButtonComponent {
    private auth = inject(AuthService);
    isLoggedIn = computed(() => !!this.auth.token());
    loginUrl = environment.steamLoginUrl;

    login() {
        window.location.href = this.loginUrl;
    }    

    logout() { this.auth.clear(); }
}