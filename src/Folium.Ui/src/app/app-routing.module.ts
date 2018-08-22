import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent } from "./home/home.component";
import { AuthGuard } from "./core/auth-guard.service";

const appRoutes: Routes = [
  {
      path: "",
      redirectTo: "/home",
      pathMatch: "full",
      canActivate: [AuthGuard]
  },
  {
      path: "home",
      component: HomeComponent,
      canActivate: [AuthGuard]
  }/*,  
  // Had to remove the lazy loading routes as IE11 was throwing "Error: Loading chunk 1 failed".
  // This looks like this bug: https://github.com/webpack/webpack/issues/5964 which says it has been fixed in 4.2.0, but
  // using Angular CLI v6.1.2, I still get the problem.
  // This results in a larger js file, as they are not seperated in chunks, so a slower load time :( 
  {
      path: "tutees",
      loadChildren: "./tutees/tutees.module#FmTuteesModule",
      canActivate: [AuthGuard]
  },
  {
      path: "placements",
      loadChildren: "./placements/placements.module#FmPlacementsModule",
      canActivate: [AuthGuard]
  },
  {
      path: "entries",
      loadChildren: "./entries/entries.module#FmEntriesModule",
      canActivate: [AuthGuard]
  },
  {
      path: "skills",
      loadChildren: "./skills/skills.module#FmSkillsModule",
      canActivate: [AuthGuard]
  },
  {
      path: "reports",
      loadChildren: "./reports/reports.module#FmReportsModule",
      canActivate: [AuthGuard]
  }*/
];

@NgModule({
  imports: [RouterModule.forRoot(appRoutes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
