import { PlatformsDto } from './platforms.dto';

export interface GameDetailsDto {
  appId: number;
  name?: string;
  headerImage?: string;
  shortDescription?: string;
  detailedDescription?: string;
  aboutTheGame?: string;
  website?: string;
  developers?: string[];
  publishers?: string[];
  genres?: string[];
  currentPlayers?: number;
  achievements: AchievementDto[];
  platforms?: PlatformsDto;
  supportsLinux?: boolean;
}

export interface AchievementDto {
  apiName: string;
  displayName?: string;
  description?: string;
  achieved: boolean;
  unlockTime?: string;
  icon?: string;
  iconGray?: string;
  globalPercent?: number;
}