export interface FriendHoursRow {
  steamId64: string;
  personaName: string;
  avatarMedium: string;  // 64x64
  isYou: boolean;
  hoursTotal: number;    // lifetime hours (or hours for app if appid provided)
  hours2Weeks?: number;  // last-2-weeks for that app (when appid provided)
  privateOrUnavailable: boolean; // true if profile/stats not accessible
}

export interface FriendsLeaderboardDto {
  appId?: number;        // null => total library hours
  rows: FriendHoursRow[];
}