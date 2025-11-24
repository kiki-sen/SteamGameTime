import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { LoginButtonComponent } from './login-button.component';
import { AuthService } from '../../services/auth.service';
import { AuthApiService } from '../../services/auth-api.service';
import { signal } from '@angular/core';
import { of } from 'rxjs';

describe('LoginButtonComponent', () => {
  let component: LoginButtonComponent;
  let fixture: ComponentFixture<LoginButtonComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockAuthApiService: jasmine.SpyObj<AuthApiService>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['clear', 'refreshToken'], {
      token: signal<string | null>(null)
    });
    mockAuthService.refreshToken.and.returnValue('mock-refresh-token');

    mockAuthApiService = jasmine.createSpyObj('AuthApiService', ['logout']);
    mockAuthApiService.logout.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [LoginButtonComponent, HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: AuthApiService, useValue: mockAuthApiService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginButtonComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should calculate isLoggedIn correctly when token exists', () => {
    mockAuthService.token.set('valid-token');
    expect(component.isLoggedIn()).toBe(true);
  });

  it('should calculate isLoggedIn correctly when no token', () => {
    mockAuthService.token.set(null);
    expect(component.isLoggedIn()).toBe(false);
  });

  it('should have loginUrl property', () => {
    expect(component.loginUrl).toBeDefined();
  });

  it('should have login method', () => {
    expect(component.login).toBeDefined();
  });

  it('should call auth.clear on logout', () => {
    component.logout();
    expect(mockAuthService.clear).toHaveBeenCalled();
  });
});