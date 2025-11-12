import { Component, inject, computed } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterOutlet, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './services/auth.service';
import { LoginButtonComponent } from './components/login-button/login-button.component';
import { ProfileWidget } from './components/profile-widget/profile-widget';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [NgIf, RouterOutlet, RouterLink, RouterLinkActive, LoginButtonComponent, ProfileWidget],
    templateUrl: './app.component.html',
    styleUrl: './app.component.css'
})
export class AppComponent {
    private router = inject(Router);
    private auth = inject(AuthService);
    
    isLoggedIn = computed(() => !!this.auth.token());

    // Handle token from backend redirect
    ngOnInit() {
        const url = new URL(window.location.href);
        const token = url.searchParams.get('token');
        const refreshToken = url.searchParams.get('refreshToken');
        if (token && refreshToken) {
            this.auth.setTokens(token, refreshToken);
            this.router.navigateByUrl('/');
        }
    }
}
