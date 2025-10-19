import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    // Mock localStorage
    let store: { [key: string]: string } = {};
    const mockLocalStorage = {
      getItem: (key: string): string | null => key in store ? store[key] : null,
      setItem: (key: string, value: string) => store[key] = `${value}`,
      removeItem: (key: string) => delete store[key],
      clear: () => store = {}
    };

    Object.defineProperty(window, 'localStorage', {
      value: mockLocalStorage
    });

    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize with null token when localStorage is empty', () => {
    expect(service.token()).toBeNull();
  });

  it('should set token in localStorage and signal', () => {
    const testToken = 'test-jwt-token';
    
    service.setToken(testToken);
    
    expect(service.token()).toBe(testToken);
    expect(localStorage.getItem('jwtToken')).toBe(testToken);
  });

  it('should clear token from localStorage and signal', () => {
    const testToken = 'test-jwt-token';
    service.setToken(testToken);
    
    service.clear();
    
    expect(service.token()).toBeNull();
    expect(localStorage.getItem('jwtToken')).toBeNull();
  });

  it('should load existing token from localStorage on initialization', () => {
    localStorage.setItem('jwtToken', 'existing-token');
    
    // Create new service instance to test initialization
    const newService = new AuthService();
    
    expect(newService.token()).toBe('existing-token');
  });
});