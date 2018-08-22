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

import { ViewPlacementsComponent } from "./components/view-placements.component";
import { ViewPlacementComponent } from "./components/view-placement.component";
import { CurrentUserResolve } from "../user/user-resolve.service";
import { AuthGuard } from "../core/auth-guard.service";

const placementsRoutes: Routes = [
    {
        path: "placements",
        component: ViewPlacementsComponent,
        resolve: {
            currentUser: CurrentUserResolve
        },
        canActivate: [AuthGuard]
    },
	{
        path: "placements/:id",
		component: ViewPlacementComponent,
        resolve: {
            currentUser: CurrentUserResolve
        },
        canActivate: [AuthGuard]
    }
];

@NgModule({
    imports: [
      RouterModule.forChild(placementsRoutes)
    ],
    exports: [
      RouterModule
    ]
  })
export class FmPlacementsRoutingModule { }
