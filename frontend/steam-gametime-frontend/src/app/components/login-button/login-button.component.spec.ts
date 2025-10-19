import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginButtonComponent } from './login-button.component';
import { AuthService } from '../../services/auth.service';
import { signal } from '@angular/core';

describe('LoginButtonComponent', () => {
  let component: LoginButtonComponent;
  let fixture: ComponentFixture<LoginButtonComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['clear'], {
      token: signal<string | null>(null)
    });

    await TestBed.configureTestingModule({
      imports: [LoginButtonComponent],
      providers: [
        { provide: AuthService, useValue: mockAuthService }
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