import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AuthApiService } from '../services/auth-api.service';

let isRefreshing = false;

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);
    const authApi = inject(AuthApiService);
    const router = inject(Router);
    const token = auth.token();

    if (token) {
        req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }

    return next(req).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401 && !req.url.includes('/auth/steam/refresh') && !isRefreshing) {
                const refreshToken = auth.refreshToken();
                
                if (refreshToken) {
                    isRefreshing = true;
                    
                    return authApi.refreshToken(refreshToken).pipe(
                        switchMap((result) => {
                            isRefreshing = false;
                            auth.setTokens(result.token, result.refreshToken);
                            
                            const clonedReq = req.clone({
                                setHeaders: { Authorization: `Bearer ${result.token}` }
                            });
                            
                            return next(clonedReq);
                        }),
                        catchError((refreshError) => {
                            isRefreshing = false;
                            auth.clear();
                            router.navigate(['/']);
                            return throwError(() => refreshError);
                        })
                    );
                } else {
                    auth.clear();
                    router.navigate(['/']);
                }
            }
            
            return throwError(() => error);
        })
    );
};
