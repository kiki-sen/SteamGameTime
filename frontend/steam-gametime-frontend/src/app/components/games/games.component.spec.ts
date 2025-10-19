import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GamesComponent } from './games.component';
import { SteamService } from '../../services/steam.service';
import { GameHoursDto } from '../../models/game-hours.dto';

describe('GamesComponent', () => {
  let component: GamesComponent;
  let fixture: ComponentFixture<GamesComponent>;
  let mockSteamService: jasmine.SpyObj<SteamService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockSteamService = jasmine.createSpyObj('SteamService', ['getGames']);
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
});