export interface ProfileDto {
    steamId64: string;
    personaName: string;
    steamLevel?: number;
    avatarSmall?: string;     // 32x32
    avatarMedium?: string;    // 64x64
    avatarFull?: string;      // 184x184
    countryCode?: string;     // e.g., "US"
    communityVisibilityState: number; // 1=Private, 3=Public
    personaState?: number;       // 0=Offline.. etc.
    timeCreatedUtc?: string;
    lastLogOffUtc?: string;
}
