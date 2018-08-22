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
import { NgModule }       from "@angular/core";
import { CommonModule }       from "@angular/common";
import { RouterModule } from "@angular/router";

import { TuteesComponent } from "./tutees.component";
import { FmUserModule } from "../user/user.module";
import { FmEntriesCoreModule } from "../entries/entries.module";
import { FmSkillsCoreModule } from "../skills/skills.module";
import { FmPlacementsCoreModule } from "../placements/placements.module";
import { TuteeViewerComponent } from "./tutee-viewer.component";
import { ViewTuteeComponent } from "./view.tutee.component";
import { FmTuteesRoutingModule } from "./tutees-routing.module";

@NgModule({
    imports:      [
        CommonModule,
        RouterModule,

        FmEntriesCoreModule,
        FmPlacementsCoreModule,
        FmSkillsCoreModule,
        FmUserModule        
    ],
    declarations: [
        TuteeViewerComponent,
        TuteesComponent,
        ViewTuteeComponent
    ],
    exports: [
        TuteeViewerComponent
	],
})
export class FmTuteesCoreModule { }

/* I have split these modules as we do not want the routing included
* in other modules that include this as it causes issues with the "" path
* being matched. Not sure if this is a bug or by design?!
*/

@NgModule({
    imports:      [
        FmTuteesCoreModule,
        FmTuteesRoutingModule
    ]
})
export class FmTuteesModule {}