import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ProfileDto } from '../models/profile.dto';
import { GameHoursDto } from '../models/game-hours.dto';
import { GameDetailsDto } from '../models/game-details.dto';
import { PageResultDto } from '../models/pageresult.dto';
import { FriendsLeaderboardDto } from '../models/friends-leaderboard.dto';
import { FriendsListDto } from '../models/friends-list.dto';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SteamService {
    private base = environment.apiBaseUrl;

    constructor(private http: HttpClient) {}

    getProfile(): Observable<ProfileDto> {
        return this.http.get<ProfileDto>(`${this.base}/api/steam/profile`);
    }

    getGames(opts: { page?: number; pageSize?: number; q?: string; sort?: string }) : Observable<PageResultDto<GameHoursDto>> {
        let params = new HttpParams();
        if (opts.page) params = params.set('page', opts.page);
        if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
        if (opts.q) params = params.set('q', opts.q);
        if (opts.sort) params = params.set('sort', opts.sort);        

        return this.http.get<PageResultDto<GameHoursDto>>(`${this.base}/api/steam/games`, { params });
    }

    getGameDetails(appId: number): Observable<GameDetailsDto> {
        return this.http.get<GameDetailsDto>(`${this.base}/api/steam/${appId}/gamedetails`);
    }

    getFriendsLeaderboard(appId?: number): Observable<FriendsLeaderboardDto> {
        let params = new HttpParams();
        if (appId) params = params.set('appid', appId);
        
        return this.http.get<FriendsLeaderboardDto>(`${this.base}/api/steam/friends/leaderboard`, { params });
    }

    getFriendsList(includeSelf: boolean = true): Observable<FriendsListDto> {
        let params = new HttpParams();
        if (!includeSelf) params = params.set('includeSelf', 'false');
        
        return this.http.get<FriendsListDto>(`${this.base}/api/steam/friends/list`, { params });
    }
}