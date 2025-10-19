import { Component, OnInit, inject, AfterViewInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NgIf, NgFor, DatePipe, DecimalPipe } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import DOMPurify from 'dompurify';
import { SteamService } from '../../services/steam.service';
import { GameDetailsDto, AchievementDto } from '../../models/game-details.dto';

interface NavigationSection {
  id: string;
  title: string;
  isMain: boolean;
  subItems?: NavigationItem[];
}

interface NavigationItem {
  id: string;
  title: string;
  level: number;
}

@Component({
  selector: 'app-game-detail',
  standalone: true,
  imports: [NgIf, NgFor, DatePipe, DecimalPipe, RouterModule],
  templateUrl: './game-detail.component.html',
  styleUrl: './game-detail.component.css'
})
export class GameDetailComponent implements OnInit, AfterViewInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private steam = inject(SteamService);
  private sanitizer = inject(DomSanitizer);

  gameDetails?: GameDetailsDto;
  loading = true;
  error = false;
  navigationSections: NavigationSection[] = [];
  showNavigation = false;

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

  ngAfterViewInit() {
    // Extract navigation after view is initialized
    setTimeout(() => {
      this.extractNavigationItems();
    }, 100);
  }

  private loadGameDetails(appId: number) {
    this.loading = true;
    this.error = false;

    this.steam.getGameDetails(appId).subscribe({
      next: (details) => {
        this.gameDetails = details;
        this.loading = false;
        // Extract navigation after game details are loaded and DOM is updated
        setTimeout(() => {
          this.extractNavigationItems();
        }, 300);
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
      ALLOWED_ATTR: ['href', 'target', 'rel', 'id'],
      ALLOW_DATA_ATTR: false
    });
  }

  getSanitizedDetailedDescription(): SafeHtml {
    if (!this.gameDetails?.detailedDescription) return '';
    
    let description = this.sanitizeHtml(this.gameDetails.detailedDescription);
    description = this.addNavigationIds(description);
    const decodedDescription = this.decodeHtml(description);
    
    // Bypass Angular's sanitization to preserve our navigation IDs
    return this.sanitizer.bypassSecurityTrustHtml(decodedDescription);
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

  // Add unique IDs to headers in the description for navigation
  private addNavigationIds(html: string): string {
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = html;
    
    const headers = tempDiv.querySelectorAll('h1, h2, h3, h4, h5, h6');
    
    headers.forEach((header, index) => {
      const text = header.textContent || '';
      const id = `section-${index}-${text.toLowerCase().replace(/[^a-z0-9]/g, '-').substring(0, 30)}`;
      header.setAttribute('id', id);
    });
    
    return tempDiv.innerHTML;
  }

  // Extract navigation items from the description content
  private extractNavigationItems(): void {
    this.navigationSections = [];
    
    // Add Game Title section
    if (this.gameDetails?.name) {
      this.navigationSections.push({
        id: 'game-title',
        title: this.gameDetails.name,
        isMain: true
      });
    }
    
    // Add About This Game section with sub-items from description headers
    if (this.gameDetails?.detailedDescription) {
      // Process the description content with IDs added
      let description = this.sanitizeHtml(this.gameDetails.detailedDescription);
      description = this.addNavigationIds(description);
      const processedDescription = this.decodeHtml(description);
      
      // Now extract headers from the processed content
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = processedDescription;
      
      const headers = tempDiv.querySelectorAll('h1, h2, h3, h4, h5, h6');
      const subItems: NavigationItem[] = [];
      
      headers.forEach((header) => {
        const text = header.textContent?.trim();
        const id = header.getAttribute('id');
        const level = parseInt(header.tagName.substring(1));
        
        if (text && id) {
          subItems.push({
            id,
            title: text,
            level
          });
        }
      });
      
      this.navigationSections.push({
        id: 'about-game',
        title: 'About This Game',
        isMain: true,
        subItems: subItems.length > 0 ? subItems : undefined
      });
    }
    
    // Add Achievements section if there are achievements
    if (this.totalAchievements > 0) {
      this.navigationSections.push({
        id: 'achievement-progress',
        title: 'Achievements',
        isMain: true
      });
    }
    
    this.showNavigation = this.navigationSections.length > 1; // Show if more than just title
  }

  // Smooth scroll to a section
  scrollToSection(sectionId: string): void {
    const element = document.getElementById(sectionId);
    
    if (element) {
      // Get the navigation panel height dynamically
      const navPanel = document.querySelector('.navigation-pane') as HTMLElement;
      const navHeight = navPanel ? navPanel.offsetHeight : 0;
      
      // Add extra padding to ensure content is clearly visible
      const headerOffset = navHeight + 20;
      
      const elementPosition = element.getBoundingClientRect().top;
      const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

      window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
      });
    }
  }
}