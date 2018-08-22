/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/
import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";

import { ViewReportsComponent } from "./components/view-reports.component";
import { ViewSelfAssessmentEngagementComponent } from "./components/view-self-assessment-engagement.component";
import { CurrentUserResolve } from "../user/user-resolve.service";
import { ViewEntryEngagementComponent } from "./components/view-entry-engagement.component";
import { ViewPlacementEngagementComponent } from "./components/view-placement-engagement.component";
import { AuthGuard } from "../core/auth-guard.service";

const reportsRoutes: Routes = [
    {
        path: "reports",
        component: ViewReportsComponent,
        resolve: {
            currentUser: CurrentUserResolve
        },
        canActivate: [AuthGuard]
    },
    {
        path: "reports/self-assessment-engagement",
        component: ViewSelfAssessmentEngagementComponent,
        resolve: {
            currentUser: CurrentUserResolve
        },
        canActivate: [AuthGuard]
    },
    {
        path: "reports/entry-engagement",
        component: ViewEntryEngagementComponent,
        resolve: {
            currentUser: CurrentUserResolve
        },
        canActivate: [AuthGuard]
    },
    {
        path: "reports/placement-engagement",
        component: ViewPlacementEngagementComponent,
        resolve: {
            currentUser: CurrentUserResolve
        },
        canActivate: [AuthGuard]
    }
];

@NgModule({
    imports: [
      RouterModule.forChild(reportsRoutes)
    ],
    exports: [
      RouterModule
    ]
  })
export class FmReportsRoutingModule { }
