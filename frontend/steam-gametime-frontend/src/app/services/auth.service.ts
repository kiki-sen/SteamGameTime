import { Injectable, signal } from '@angular/core';

const TOKEN_KEY = 'jwtToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
    token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

    setToken(t: string) {
        localStorage.setItem(TOKEN_KEY, t);
        this.token.set(t);
    }
    
    clear() {
        localStorage.removeItem(TOKEN_KEY);
        this.token.set(null);
    }
}