export interface FriendSummaryDto {
  steamId64: string;
  personaName: string;
  avatarSmall?: string;
  avatarMedium?: string;
  avatarFull?: string;
  personaState?: number;              // 0..6
  communityVisibilityState: number;   // 1=Private, 3=Public
  friendSinceUtc?: string; // when you became friends (if available)
  steamLevel?: number;
  isYou: boolean;
  gameId?: string;         // currently playing game ID
  gameName?: string;       // currently playing game name
}

export interface FriendsListDto {
  rows: FriendSummaryDto[];
}