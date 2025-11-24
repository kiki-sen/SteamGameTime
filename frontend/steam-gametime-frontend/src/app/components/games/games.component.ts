import { Component, ElementRef, ViewChild, HostListener, inject, OnInit, AfterViewInit, OnDestroy } from '@angular/core';
import { NgIf, NgFor, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { SteamService } from '../../services/steam.service';
import { PageResultDto } from '../../models/pageresult.dto';
import { GameHoursDto } from '../../models/game-hours.dto';
import { PlatformsDto } from '../../models/platforms.dto';

@Component({
  selector: 'app-games',
  standalone: true,
  imports: [DecimalPipe, FormsModule, NgIf, NgFor, RouterModule],
  templateUrl: './games.component.html',
  styleUrls: ['./games.component.css'] // <-- fix typo: styleUrls (plural)
})
export class GamesComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('grid') gridRef!: ElementRef<HTMLDivElement>;
  private steam = inject(SteamService);
  private router = inject(Router);

  page?: PageResultDto<GameHoursDto>;
  busy = false;

  pageIndex = 1;
  pageSize = 10000;        // Large number to get all games
  q = '';
  sort = 'hoursTotal:desc';
  linuxOnly = false;

  // Platform data caching and loading
  linuxSupportCache = new Map<number, boolean>();
  loadingPlatforms = new Set<number>();
  platformLoadQueue: number[] = [];
  isProcessingQueue = false;
  loadingProgress = 0;
  totalToLoad = 0;

  private resizeObs?: ResizeObserver;
  private readonly minCardWidth = 140; // Steam capsule width for 8 columns


  ngOnInit() {
    this.reload();
  }

  ngAfterViewInit() {
    // If grid is present now, set up observer and recalc.
    if (this.gridRef?.nativeElement) {
      this.attachResizeObserver();
      this.recalcPageSize(true);
    }
  }

  // Also handle cases where #grid appears later (e.g., SSR or conditional rendering)
  ngAfterViewChecked() {
    if (!this.resizeObs && this.gridRef?.nativeElement) {
      this.attachResizeObserver();
      this.recalcPageSize(true);
    }
  }

  private attachResizeObserver() {
    const el = this.gridRef.nativeElement;
    this.resizeObs = new ResizeObserver(() => this.recalcPageSize());
    this.resizeObs.observe(el);
  }

  @HostListener('window:resize')
  onResize() {
    this.recalcPageSize();
  }

  private recalcPageSize(forceReload = false) {
    // No pagination needed - just reload if forced
    if (forceReload && !this.page) {
      this.reload();
    }
  }

  reload() {
    this.busy = true;
    this.page = undefined; // Clear current games to show loading state
    this.steam.getGames({
      page: this.pageIndex,
      pageSize: this.pageSize,
      q: this.q || undefined,
      sort: this.sort
    }).subscribe({
      next: res => { 
        this.page = res; 
        this.busy = false;
        // Always start loading platform data opportunistically
        this.loadPlatformDataForGames();
      },
      error: err => { console.error(err); this.busy = false; }
    });
  }

  onLinuxFilterChange() {
    if (this.linuxOnly) {
      this.loadPlatformDataForGames();
    }
  }

  loadPlatformDataForGames() {
    if (!this.page?.items) return;

    // Queue games that need platform data
    const gamesToCheck = this.page.items.filter(
      g => !this.linuxSupportCache.has(g.appId) && !this.loadingPlatforms.has(g.appId)
    );

    if (gamesToCheck.length === 0) return;

    this.platformLoadQueue.push(...gamesToCheck.map(g => g.appId));
    this.totalToLoad = this.platformLoadQueue.length;
    this.loadingProgress = 0;

    if (!this.isProcessingQueue) {
      this.processQueue();
    }
  }

  private processQueue() {
    if (this.platformLoadQueue.length === 0) {
      this.isProcessingQueue = false;
      this.totalToLoad = 0;
      this.loadingProgress = 0;
      return;
    }

    this.isProcessingQueue = true;
    const appId = this.platformLoadQueue.shift()!;
    this.loadingPlatforms.add(appId);

    this.steam.getPlatforms(appId).subscribe({
      next: (platforms: PlatformsDto) => {
        this.linuxSupportCache.set(appId, platforms.linux);
        this.loadingPlatforms.delete(appId);
        this.loadingProgress++;

        // Throttle requests to avoid rate limiting (150ms delay)
        setTimeout(() => this.processQueue(), 150);
      },
      error: (err) => {
        console.error(`Failed to load platforms for ${appId}:`, err);
        this.loadingPlatforms.delete(appId);
        this.linuxSupportCache.set(appId, false); // Assume no Linux support on error
        this.loadingProgress++;

        // Continue with longer delay on error
        setTimeout(() => this.processQueue(), 500);
      }
    });
  }

  getFilteredGames(): GameHoursDto[] {
    if (!this.page?.items) return [];
    if (!this.linuxOnly) return this.page.items;

    // Only show games with confirmed Linux support
    return this.page.items.filter(g => this.linuxSupportCache.get(g.appId) === true);
  }

  isLoadingPlatform(appId: number): boolean {
    return this.loadingPlatforms.has(appId);
  }

  hasLinuxSupport(appId: number): boolean {
    return this.linuxSupportCache.get(appId) === true;
  }

  ngOnDestroy() {
    this.platformLoadQueue = [];
    this.isProcessingQueue = false;
  }

  hours(mins: number) { return Math.round((mins / 60) * 10) / 10; }

  navigateToGame(appId: number) {
    this.router.navigate(['/game', appId]);
  }

  private imageFailures = new Set<number>();

  // Get appropriate image URL with fallback logic
  getImageUrl(game: GameHoursDto): string | null {
    if (!game.appId) return null;
    
    // If library image failed, use header image
    if (this.imageFailures.has(game.appId)) {
      return `https://steamcdn-a.akamaihd.net/steam/apps/${game.appId}/header.jpg`;
    }
    
    // Try library image first
    return `https://steamcdn-a.akamaihd.net/steam/apps/${game.appId}/library_600x900.jpg`;
  }

  // Handle image load errors
  onImageError(event: Event, game: GameHoursDto) {
    const img = event.target as HTMLImageElement;
    
    // If this was a library image failure, try header image
    if (img.src.includes('library_600x900') && game.appId) {
      this.imageFailures.add(game.appId);
      img.src = `https://steamcdn-a.akamaihd.net/steam/apps/${game.appId}/header.jpg`;
      return;
    }
    
    // If header image also failed, hide the image
    img.style.display = 'none';
  }

  // Get game name initials for placeholder
  getGameInitials(name: string | undefined): string {
    if (!name) return 'GA';
    return name
      .split(' ')
      .filter(word => word.length > 0)
      .slice(0, 2)
      .map(word => word[0].toUpperCase())
      .join('') || 'GA';
  }
}
