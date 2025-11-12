import { Component, OnInit, inject, HostListener } from '@angular/core';
import { NgIf, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SteamService } from '../../services/steam.service';
import { AuthService } from '../../services/auth.service';
import { AuthApiService } from '../../services/auth-api.service';
import { ProfileDto } from '../../models/profile.dto';

@Component({
  selector: 'app-profile-widget',
  imports: [NgIf, NgClass, RouterLink],
  templateUrl: './profile-widget.html',
  styleUrl: './profile-widget.css'
})
export class ProfileWidget implements OnInit {
  private steam = inject(SteamService);
  private auth = inject(AuthService);
  private authApi = inject(AuthApiService);
  
  profile?: ProfileDto;
  loading = true;
  showDropdown = false;
  
  ngOnInit() {
    this.loadProfile();
  }
  
  loadProfile() {
    this.steam.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading profile:', err);
        this.loading = false;
      }
    });
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
  
  toggleDropdown() {
    this.showDropdown = !this.showDropdown;
  }
  
  closeDropdown() {
    this.showDropdown = false;
  }
  
  logout() {
    const refreshToken = this.auth.refreshToken();
    this.authApi.logout(refreshToken).subscribe({
      next: () => {
        this.auth.clear();
        this.closeDropdown();
      },
      error: () => {
        this.auth.clear();
        this.closeDropdown();
      }
    });
  }
  
  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    const container = target.closest('.profile-widget-container');
    if (!container) {
      this.closeDropdown();
    }
  }
}
