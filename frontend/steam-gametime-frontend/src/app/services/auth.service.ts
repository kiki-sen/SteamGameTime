import { Injectable, signal } from '@angular/core';

const TOKEN_KEY = 'jwtToken';
const REFRESH_TOKEN_KEY = 'refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
    token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
    refreshToken = signal<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY));

    setToken(t: string) {
        localStorage.setItem(TOKEN_KEY, t);
        this.token.set(t);
    }

    setRefreshToken(t: string) {
        localStorage.setItem(REFRESH_TOKEN_KEY, t);
        this.refreshToken.set(t);
    }

    setTokens(accessToken: string, refreshToken: string) {
        this.setToken(accessToken);
        this.setRefreshToken(refreshToken);
    }
    
    clear() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        this.token.set(null);
        this.refreshToken.set(null);
    }
}
