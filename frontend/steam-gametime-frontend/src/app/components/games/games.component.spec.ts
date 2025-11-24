import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GamesComponent } from './games.component';
import { SteamService } from '../../services/steam.service';
import { GameHoursDto } from '../../models/game-hours.dto';
import { PlatformsDto } from '../../models/platforms.dto';

describe('GamesComponent', () => {
  let component: GamesComponent;
  let fixture: ComponentFixture<GamesComponent>;
  let mockSteamService: jasmine.SpyObj<SteamService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockSteamService = jasmine.createSpyObj('SteamService', ['getGames', 'getPlatforms']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [GamesComponent],
      providers: [
        { provide: SteamService, useValue: mockSteamService },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GamesComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load games on init', () => {
    const mockGames = { items: [], total: 0, page: 1, pageSize: 10 };
    mockSteamService.getGames.and.returnValue(of(mockGames));

    component.ngOnInit();

    expect(mockSteamService.getGames).toHaveBeenCalled();
    expect(component.busy).toBe(false);
    expect(component.page).toEqual(mockGames);
  });

  it('should handle error when loading games', () => {
    mockSteamService.getGames.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');

    component.ngOnInit();

    expect(component.busy).toBe(false);
    expect(console.error).toHaveBeenCalled();
  });

  it('should convert minutes to hours correctly', () => {
    expect(component.hours(60)).toBe(1);
    expect(component.hours(90)).toBe(1.5);
    expect(component.hours(123)).toBe(2.1);
  });

  it('should navigate to game detail', () => {
    const appId = 12345;

    component.navigateToGame(appId);

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/game', appId]);
  });

  it('should return library image URL for game', () => {
    const game: GameHoursDto = { appId: 12345, name: 'Test Game', hoursTotal: 10, hours2Weeks: 1 };

    const imageUrl = component.getImageUrl(game);

    expect(imageUrl).toBe('https://steamcdn-a.akamaihd.net/steam/apps/12345/library_600x900.jpg');
  });

  it('should return null image URL when no appId', () => {
    const game: GameHoursDto = { appId: 0, name: 'Test Game', hoursTotal: 10, hours2Weeks: 1 };

    const imageUrl = component.getImageUrl(game);

    expect(imageUrl).toBeNull();
  });

  it('should get game initials correctly', () => {
    expect(component.getGameInitials('Counter Strike')).toBe('CS');
    expect(component.getGameInitials('Portal')).toBe('P');
    expect(component.getGameInitials('Grand Theft Auto V')).toBe('GT');
    expect(component.getGameInitials('')).toBe('GA');
    expect(component.getGameInitials(undefined)).toBe('GA');
  });

  it('should reload games', () => {
    const mockGames = { items: [], total: 0, page: 1, pageSize: 10 };
    mockSteamService.getGames.and.returnValue(of(mockGames));

    component.reload();

    // Initially busy should be true and page should be undefined
    // But after the observable completes, busy becomes false and page is set
    expect(mockSteamService.getGames).toHaveBeenCalledWith({
      page: component.pageIndex,
      pageSize: component.pageSize,
      q: undefined,
      sort: component.sort
    });
    
    // After subscription completes
    expect(component.busy).toBe(false);
    expect(component.page).toEqual(mockGames);
  });

  it('should check Linux support correctly', () => {
    const appId = 12345;
    component.linuxSupportCache.set(appId, true);

    expect(component.hasLinuxSupport(appId)).toBe(true);
    expect(component.hasLinuxSupport(99999)).toBe(false);
  });

  it('should track loading platforms', () => {
    const appId = 12345;
    component.loadingPlatforms.add(appId);

    expect(component.isLoadingPlatform(appId)).toBe(true);
    expect(component.isLoadingPlatform(99999)).toBe(false);
  });

  it('should return all games when linuxOnly is false', () => {
    const mockGames: GameHoursDto[] = [
      { appId: 1, name: 'Game 1', hoursTotal: 10, hours2Weeks: 1 },
      { appId: 2, name: 'Game 2', hoursTotal: 20, hours2Weeks: 2 }
    ];
    component.page = { items: mockGames, total: 2, page: 1, pageSize: 10 };
    component.linuxOnly = false;

    const filtered = component.getFilteredGames();

    expect(filtered.length).toBe(2);
  });

  it('should filter games when linuxOnly is true', () => {
    const mockGames: GameHoursDto[] = [
      { appId: 1, name: 'Linux Game', hoursTotal: 10, hours2Weeks: 1 },
      { appId: 2, name: 'Windows Game', hoursTotal: 20, hours2Weeks: 2 },
      { appId: 3, name: 'Another Linux Game', hoursTotal: 15, hours2Weeks: 0 }
    ];
    component.page = { items: mockGames, total: 3, page: 1, pageSize: 10 };
    component.linuxOnly = true;
    component.linuxSupportCache.set(1, true);
    component.linuxSupportCache.set(2, false);
    component.linuxSupportCache.set(3, true);

    const filtered = component.getFilteredGames();

    expect(filtered.length).toBe(2);
    expect(filtered[0].appId).toBe(1);
    expect(filtered[1].appId).toBe(3);
  });

  it('should load platform data when filter changes', () => {
    const mockGames: GameHoursDto[] = [
      { appId: 1, name: 'Game 1', hoursTotal: 10, hours2Weeks: 1 }
    ];
    component.page = { items: mockGames, total: 1, page: 1, pageSize: 10 };
    component.linuxOnly = true;

    spyOn(component, 'loadPlatformDataForGames');

    component.onLinuxFilterChange();

    expect(component.loadPlatformDataForGames).toHaveBeenCalled();
  });

  it('should process platform data queue', (done) => {
    const mockPlatforms = { appId: 1, windows: true, mac: false, linux: true };
    mockSteamService.getPlatforms.and.returnValue(of(mockPlatforms));
    
    component.platformLoadQueue = [1];
    component.isProcessingQueue = false;

    component['processQueue']();

    setTimeout(() => {
      expect(component.linuxSupportCache.get(1)).toBe(true);
      expect(component.loadingPlatforms.has(1)).toBe(false);
      done();
    }, 200);
  });

  it('should handle platform loading error gracefully', (done) => {
    mockSteamService.getPlatforms.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');
    
    component.platformLoadQueue = [1];
    component.isProcessingQueue = false;

    component['processQueue']();

    setTimeout(() => {
      expect(component.linuxSupportCache.get(1)).toBe(false);
      expect(console.error).toHaveBeenCalled();
      done();
    }, 600);
  });
});
