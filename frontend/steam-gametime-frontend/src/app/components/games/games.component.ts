import { Component, ElementRef, ViewChild, HostListener, inject, OnInit, AfterViewInit } from '@angular/core';
import { NgIf, NgFor, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { SteamService } from '../../services/steam.service';
import { PageResultDto } from '../../models/pageresult.dto';
import { GameHoursDto } from '../../models/game-hours.dto';

@Component({
  selector: 'app-games',
  standalone: true,
  imports: [DecimalPipe, FormsModule, NgIf, NgFor, RouterModule],
  templateUrl: './games.component.html',
  styleUrls: ['./games.component.css'] // <-- fix typo: styleUrls (plural)
})
export class GamesComponent implements OnInit, AfterViewInit {
  @ViewChild('grid') gridRef!: ElementRef<HTMLDivElement>;
  private steam = inject(SteamService);
  private router = inject(Router);

  page?: PageResultDto<GameHoursDto>;
  busy = false;

  pageIndex = 1;
  pageSize = 10000;        // Large number to get all games
  q = '';
  sort = 'hoursTotal:desc';

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
      next: res => { this.page = res; this.busy = false; },
      error: err => { console.error(err); this.busy = false; }
    });
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
