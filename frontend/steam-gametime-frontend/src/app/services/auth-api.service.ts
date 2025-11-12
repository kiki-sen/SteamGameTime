import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthResultDto } from '../models/auth-result.dto';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
    private http = inject(HttpClient);

    refreshToken(refreshToken: string): Observable<AuthResultDto> {
        return this.http.post<AuthResultDto>(`${environment.apiBaseUrl}/auth/steam/refresh`, {
            refreshToken
        });
    }

    logout(refreshToken: string | null): Observable<void> {
        return this.http.post<void>(`${environment.apiBaseUrl}/auth/steam/logout`, {
            refreshToken
        });
    }
}
