import { Routes } from '@angular/router';
import { GamesComponent } from './components/games/games.component';
import { GameDetailComponent } from './components/game-detail/game-detail.component';
import { Friends } from './components/friends/friends';
import { Profile } from './components/profile/profile';
import { AppComponent } from './app.component';


export const routes: Routes = [
{ path: '', component: GamesComponent },
{ path: 'friends', component: Friends },
{ path: 'profile', component: Profile },
{ path: 'game/:appId', component: GameDetailComponent },
{ path: 'auth/callback', component: AppComponent },
{ path: '**', redirectTo: '' }
];
