import { Component, OnInit, inject } from '@angular/core';
import { NgIf, NgClass, NgFor, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { SteamService } from '../../services/steam.service';
import { ProfileDto } from '../../models/profile.dto';
import { FriendsListDto, FriendSummaryDto } from '../../models/friends-list.dto';

@Component({
  selector: 'app-profile',
  imports: [NgIf, NgClass, NgFor, DatePipe, RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  private steam = inject(SteamService);
  private router = inject(Router);
  
  profile?: ProfileDto;
  friendsList?: FriendsListDto;
  loading = true;
  friendsLoading = false;
  error = false;
  
  ngOnInit() {
    this.loadProfile();
    this.loadFriendsList();
  }
  
  loadProfile() {
    this.loading = true;
    this.error = false;
    
    this.steam.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading profile:', err);
        this.error = true;
        this.loading = false;
      }
    });
  }
  
  goBack() {
    this.router.navigate(['/']);
  }
  
  getStatusText(): string {
    if (!this.profile?.personaState) return 'Offline';
    
    switch (this.profile.personaState) {
      case 0: return 'Offline';
      case 1: return 'Online';
      case 2: return 'Busy';
      case 3: return 'Away';
      case 4: return 'Snooze';
      case 5: return 'Looking to Trade';
      case 6: return 'Looking to Play';
      default: return 'Unknown';
    }
  }
  
  getStatusClass(): string {
    if (!this.profile?.personaState) return 'offline';
    
    switch (this.profile.personaState) {
      case 1: return 'online';
      case 2: return 'busy';
      case 3: return 'away';
      default: return 'offline';
    }
  }
  
  getVisibilityText(): string {
    switch (this.profile?.communityVisibilityState) {
      case 1: return 'Private Profile';
      case 3: return 'Public Profile';
      default: return 'Unknown Privacy Setting';
    }
  }
  
  getCountryFlag(): string {
    if (!this.profile?.countryCode) return '';
    return `https://flagcdn.com/w20/${this.profile.countryCode.toLowerCase()}.png`;
  }
  
  formatDate(dateString?: string): Date | null {
    return dateString ? new Date(dateString) : null;
  }
  
  loadFriendsList() {
    this.friendsLoading = true;
    
    this.steam.getFriendsList(false).subscribe({ // exclude self from friends list
      next: (friendsList) => {
        this.friendsList = friendsList;
        this.friendsLoading = false;
      },
      error: (err) => {
        console.error('Error loading friends list:', err);
        this.friendsLoading = false;
      }
    });
  }
  
  get friendsCount(): number {
    return this.friendsList?.rows?.length || 0;
  }
  
  get onlineFriendsCount(): number {
    // Count all friends who are not offline (0) as "online"
    return this.friendsList?.rows?.filter(f => f.personaState && f.personaState > 0).length || 0;
  }
  
  getFriendStatusClass(friend: FriendSummaryDto): string {
    // If they're in-game, use a special in-game class (we'll style it like online but brighter)
    if (friend.gameId || friend.gameName) {
      return 'ingame';
    }
    
    if (!friend.personaState) return 'offline';
    
    switch (friend.personaState) {
      case 1: return 'online';
      case 2: return 'busy';
      case 3: return 'away';
      default: return 'offline';
    }
  }
  
  getFriendStatusText(friend: FriendSummaryDto): string {
    // First check if they're actually in-game (has gameId or gameName)
    if (friend.gameId || friend.gameName) {
      return friend.gameName ? `Playing ${friend.gameName}` : 'In-Game';
    }
    
    if (!friend.personaState) return 'Offline';
    
    switch (friend.personaState) {
      case 0: return 'Offline';
      case 1: return 'Online';
      case 2: return 'Busy';
      case 3: return 'Away';
      case 4: return 'Snooze';
      case 5: return 'Looking to Trade';
      case 6: return 'Looking to Play';
      default: return 'Online';  // Default active states to 'Online'
    }
  }
}
