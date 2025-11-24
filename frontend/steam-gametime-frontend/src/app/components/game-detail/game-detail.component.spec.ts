import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { GameDetailComponent } from './game-detail.component';
import { SteamService } from '../../services/steam.service';
import { GameDetailsDto, AchievementDto } from '../../models/game-details.dto';
import { PlatformsDto } from '../../models/platforms.dto';

describe('GameDetailComponent', () => {
  let component: GameDetailComponent;
  let fixture: ComponentFixture<GameDetailComponent>;
  let mockSteamService: jasmine.SpyObj<SteamService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;
  let mockDomSanitizer: jasmine.SpyObj<DomSanitizer>;

  const mockGameDetails: GameDetailsDto = {
    appId: 12345,
    name: 'Test Game',
    headerImage: 'test-header.jpg',
    detailedDescription: '<h1>Test Description</h1>',
    aboutTheGame: 'Test about game',
    achievements: [
      { apiName: 'achievement_1', displayName: 'Achievement 1', description: 'Test achievement', achieved: true, icon: 'icon1.jpg', iconGray: 'gray1.jpg' } as AchievementDto,
      { apiName: 'achievement_2', displayName: 'Achievement 2', description: 'Test achievement 2', achieved: false, icon: 'icon2.jpg', iconGray: 'gray2.jpg' } as AchievementDto
    ]
  };

  beforeEach(async () => {
    mockSteamService = jasmine.createSpyObj('SteamService', ['getGameDetails']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockDomSanitizer = jasmine.createSpyObj('DomSanitizer', ['bypassSecurityTrustHtml']);
    
    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue('12345')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [GameDetailComponent],
      providers: [
        { provide: SteamService, useValue: mockSteamService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: DomSanitizer, useValue: mockDomSanitizer }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GameDetailComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load game details on init', () => {
    mockSteamService.getGameDetails.and.returnValue(of(mockGameDetails));

    component.ngOnInit();

    expect(mockSteamService.getGameDetails).toHaveBeenCalledWith(12345);
    expect(component.gameDetails).toEqual(mockGameDetails);
    expect(component.loading).toBe(false);
    expect(component.error).toBe(false);
  });

  it('should handle invalid appId', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue('invalid');

    component.ngOnInit();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should handle error when loading game details', () => {
    mockSteamService.getGameDetails.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');

    component.ngOnInit();

    expect(component.error).toBe(true);
    expect(component.loading).toBe(false);
    expect(console.error).toHaveBeenCalled();
  });

  it('should calculate achieved count correctly', () => {
    component.gameDetails = mockGameDetails;

    expect(component.achievedCount).toBe(1);
  });

  it('should calculate total achievements correctly', () => {
    component.gameDetails = mockGameDetails;

    expect(component.totalAchievements).toBe(2);
  });

  it('should calculate completion percentage correctly', () => {
    component.gameDetails = mockGameDetails;

    expect(component.completionPercentage).toBe(50);
  });

  it('should return 0 completion percentage when no achievements', () => {
    component.gameDetails = { ...mockGameDetails, achievements: [] };

    expect(component.completionPercentage).toBe(0);
  });

  it('should navigate back', () => {
    component.goBack();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should format unlock time correctly', () => {
    const testDate = '2024-01-01T12:00:00Z';
    const result = component.formatUnlockTime(testDate);

    expect(result).toEqual(new Date(testDate));
  });

  it('should return null for empty unlock time', () => {
    const result = component.formatUnlockTime(undefined);

    expect(result).toBeNull();
  });

  it('should get achievement icon correctly', () => {
    const achievedAchievement = mockGameDetails.achievements![0];
    const unachievedAchievement = mockGameDetails.achievements![1];

    expect(component.getAchievementIcon(achievedAchievement)).toBe('icon1.jpg');
    expect(component.getAchievementIcon(unachievedAchievement)).toBe('gray2.jpg');
  });

  it('should check if game has info correctly', () => {
    component.gameDetails = { 
      ...mockGameDetails, 
      developers: ['Test Dev'], 
      publishers: ['Test Pub'] 
    };

    expect(component.hasGameInfo()).toBe(true);
  });

  it('should get library hero image URL', () => {
    component.gameDetails = mockGameDetails;

    const imageUrl = component.getLibraryHeroImage();

    expect(imageUrl).toBe('https://steamcdn-a.akamaihd.net/steam/apps/12345/library_hero.jpg');
  });

  it('should return null library hero image when no appId', () => {
    component.gameDetails = { ...mockGameDetails, appId: 0 };

    const imageUrl = component.getLibraryHeroImage();

    expect(imageUrl).toBeNull();
  });

  it('should get supported platforms correctly', () => {
    component.gameDetails = {
      ...mockGameDetails,
      platforms: { appId: 12345, windows: true, mac: false, linux: true }
    };

    const platforms = component.getSupportedPlatforms();

    expect(platforms).toEqual(['Windows', 'Linux']);
  });

  it('should return empty array when no platforms', () => {
    component.gameDetails = mockGameDetails;

    const platforms = component.getSupportedPlatforms();

    expect(platforms).toEqual([]);
  });

  it('should include platforms in hasGameInfo check', () => {
    component.gameDetails = {
      ...mockGameDetails,
      platforms: { appId: 12345, windows: true, mac: false, linux: false }
    };

    expect(component.hasGameInfo()).toBe(true);
  });

  it('should get all three platforms when all are supported', () => {
    component.gameDetails = {
      ...mockGameDetails,
      platforms: { appId: 12345, windows: true, mac: true, linux: true }
    };

    const platforms = component.getSupportedPlatforms();

    expect(platforms).toEqual(['Windows', 'Mac', 'Linux']);
  });
});
