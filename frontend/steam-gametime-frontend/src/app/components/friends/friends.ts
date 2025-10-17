import { Component, OnInit, inject } from '@angular/core';
import { NgIf, NgFor, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SteamService } from '../../services/steam.service';
import { FriendsLeaderboardDto, FriendHoursRow } from '../../models/friends-leaderboard.dto';
import { GameHoursDto } from '../../models/game-hours.dto';

@Component({
  selector: 'app-friends',
  imports: [NgIf, NgFor, DecimalPipe, FormsModule],
  templateUrl: './friends.html',
  styleUrl: './friends.css'
})
export class Friends implements OnInit {
  private steam = inject(SteamService);
  
  leaderboard?: FriendsLeaderboardDto;
  loading = true;
  error = false;
  
  selectedAppId?: number;
  gameSearch = '';
  availableGames: GameHoursDto[] = [];
  
  ngOnInit() {
    this.loadFriendsLeaderboard();
    this.loadAvailableGames();
  }
  
  loadFriendsLeaderboard(appId?: number) {
    this.loading = true;
    this.error = false;
    
    this.steam.getFriendsLeaderboard(appId).subscribe({
      next: (data) => {
        this.leaderboard = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading friends leaderboard:', err);
        this.error = true;
        this.loading = false;
      }
    });
  }
  
  loadAvailableGames() {
    // Load user's games for the dropdown filter
    this.steam.getGames({ pageSize: 1000 }).subscribe({
      next: (data) => {
        this.availableGames = (data.items || []).sort((a, b) => 
          (a.name || '').toLowerCase().localeCompare((b.name || '').toLowerCase())
        );
      },
      error: (err) => console.error('Error loading games:', err)
    });
  }
  
  onGameFilterChange() {
    if (this.selectedAppId) {
      this.loadFriendsLeaderboard(this.selectedAppId);
    } else {
      this.loadFriendsLeaderboard();
    }
  }
  
  get filteredGames() {
    if (!this.gameSearch) return this.availableGames;
    return this.availableGames.filter(game => 
      game.name?.toLowerCase().includes(this.gameSearch.toLowerCase())
    );
  }
  
  get visibleFriends() {
    // Only show friends who have the game (hours > 0) or show all for total library
    return this.leaderboard?.rows.filter(friend => 
      !friend.privateOrUnavailable && 
      (this.leaderboard?.appId ? friend.hoursTotal > 0 : true)
    ) || [];
  }
}
