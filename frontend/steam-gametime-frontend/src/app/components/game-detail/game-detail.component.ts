import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NgIf, NgFor, DatePipe, DecimalPipe } from '@angular/common';
import DOMPurify from 'dompurify';
import { SteamService } from '../../services/steam.service';
import { GameDetailsDto, AchievementDto } from '../../models/game-details.dto';

@Component({
  selector: 'app-game-detail',
  standalone: true,
  imports: [NgIf, NgFor, DatePipe, DecimalPipe, RouterModule],
  templateUrl: './game-detail.component.html',
  styleUrl: './game-detail.component.css'
})
export class GameDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private steam = inject(SteamService);

  gameDetails?: GameDetailsDto;
  loading = true;
  error = false;

  get achievedCount(): number {
    return this.gameDetails?.achievements?.filter(a => a.achieved).length || 0;
  }

  get totalAchievements(): number {
    return this.gameDetails?.achievements?.length || 0;
  }

  get completionPercentage(): number {
    if (this.totalAchievements === 0) return 0;
    return Math.round((this.achievedCount / this.totalAchievements) * 100);
  }

  ngOnInit() {
    const appId = Number(this.route.snapshot.paramMap.get('appId'));
    if (!appId || isNaN(appId)) {
      this.router.navigate(['/']);
      return;
    }

    this.loadGameDetails(appId);
  }

  private loadGameDetails(appId: number) {
    this.loading = true;
    this.error = false;

    this.steam.getGameDetails(appId).subscribe({
      next: (details) => {
        this.gameDetails = details;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading game details:', err);
        this.error = true;
        this.loading = false;
      }
    });
  }

  goBack() {
    this.router.navigate(['/']);
  }

  formatUnlockTime(unlockTime?: string): Date | null {
    return unlockTime ? new Date(unlockTime) : null;
  }

  getAchievementIcon(achievement: AchievementDto): string {
    return achievement.achieved && achievement.icon ? achievement.icon : (achievement.iconGray || '');
  }

  sanitizeHtml(html: string): string {
    return DOMPurify.sanitize(html, {
      ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'i', 'b', 'ul', 'ol', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'a'],
      ALLOWED_ATTR: ['href', 'target', 'rel'],
      ALLOW_DATA_ATTR: false
    });
  }

  getSanitizedDetailedDescription(): string {
    const description = this.gameDetails?.detailedDescription ? this.sanitizeHtml(this.gameDetails.detailedDescription) : '';
    return this.decodeHtml(description);
  }

  getSanitizedAboutTheGame(): string {
    const aboutGame = this.gameDetails?.aboutTheGame ? this.sanitizeHtml(this.gameDetails.aboutTheGame) : '';
    return this.decodeHtml(aboutGame);
  }

  // Decode HTML entities like &amp; to &
  decodeHtml(html: string): string {
    const txt = document.createElement('textarea');
    txt.innerHTML = html;
    return txt.value;
  }

  // Check if game has any info to show in the table
  hasGameInfo(): boolean {
    return !!(this.gameDetails?.developers?.length || 
             this.gameDetails?.publishers?.length || 
             this.gameDetails?.genres?.length || 
             this.gameDetails?.website ||
             this.gameDetails?.currentPlayers);
  }

  // Get Steam library hero image (wider format, better for full-width display)
  getLibraryHeroImage(): string | null {
    if (!this.gameDetails?.appId) return null;
    return `https://steamcdn-a.akamaihd.net/steam/apps/${this.gameDetails.appId}/library_hero.jpg`;
  }

  // Fallback to regular header image if library hero fails
  onHeroImageError(event: Event) {
    const img = event.target as HTMLImageElement;
    if (this.gameDetails?.headerImage) {
      img.src = this.gameDetails.headerImage;
    } else {
      img.style.display = 'none';
    }
  }
}