import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SteamService } from './steam.service';
import { environment } from '../../environments/environment';

describe('SteamService', () => {
  let service: SteamService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiBaseUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(SteamService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get profile', () => {
    const mockProfile = { steamId64: '123', personaName: 'Test User', communityVisibilityState: 3 };

    service.getProfile().subscribe(profile => {
      expect(profile).toEqual(mockProfile);
    });

    const req = httpMock.expectOne(`${baseUrl}/steam/profile`);
    expect(req.request.method).toBe('GET');
    req.flush(mockProfile);
  });

  it('should get games with parameters', () => {
    const mockGames = { items: [], total: 0, page: 1, pageSize: 10 };
    const options = { page: 1, pageSize: 10, q: 'test', sort: 'name:asc' };

    service.getGames(options).subscribe(games => {
      expect(games).toEqual(mockGames);
    });

    const req = httpMock.expectOne(request => 
      request.url === `${baseUrl}/steam/games` &&
      request.params.get('page') === '1' &&
      request.params.get('pageSize') === '10' &&
      request.params.get('q') === 'test' &&
      request.params.get('sort') === 'name:asc'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockGames);
  });

  it('should get game details', () => {
    const appId = 12345;
    const mockGameDetails = { appId, name: 'Test Game', achievements: [] };

    service.getGameDetails(appId).subscribe(details => {
      expect(details).toEqual(mockGameDetails);
    });

    const req = httpMock.expectOne(`${baseUrl}/steam/${appId}/gamedetails`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGameDetails);
  });

  it('should get friends leaderboard', () => {
    const mockLeaderboard = { rows: [] };

    service.getFriendsLeaderboard().subscribe(leaderboard => {
      expect(leaderboard).toEqual(mockLeaderboard);
    });

    const req = httpMock.expectOne(`${baseUrl}/steam/friends/leaderboard`);
    expect(req.request.method).toBe('GET');
    req.flush(mockLeaderboard);
  });

  it('should get friends leaderboard with appId', () => {
    const appId = 12345;
    const mockLeaderboard = { rows: [] };

    service.getFriendsLeaderboard(appId).subscribe(leaderboard => {
      expect(leaderboard).toEqual(mockLeaderboard);
    });

    const req = httpMock.expectOne(request => 
      request.url === `${baseUrl}/steam/friends/leaderboard` &&
      request.params.get('appid') === appId.toString()
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockLeaderboard);
  });

  it('should get friends list', () => {
    const mockFriendsList = { rows: [] };

    service.getFriendsList().subscribe(friends => {
      expect(friends).toEqual(mockFriendsList);
    });

    const req = httpMock.expectOne(`${baseUrl}/steam/friends/list`);
    expect(req.request.method).toBe('GET');
    req.flush(mockFriendsList);
  });

  it('should get friends list without self', () => {
    const mockFriendsList = { rows: [] };

    service.getFriendsList(false).subscribe(friends => {
      expect(friends).toEqual(mockFriendsList);
    });

    const req = httpMock.expectOne(request => 
      request.url === `${baseUrl}/steam/friends/list` &&
      request.params.get('includeSelf') === 'false'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockFriendsList);
  });
});