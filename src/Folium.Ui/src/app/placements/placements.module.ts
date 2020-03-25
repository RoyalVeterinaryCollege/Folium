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
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatChipsModule } from "@angular/material/chips";
import { MatNativeDateModule, DateAdapter } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";

import { FmPlacementsRoutingModule } from "./placements-routing.module";
import { PlacementsViewerComponent, OrderByPlacementDatePipe } from "./components/placements-viewer.component";
import { PlacementViewerComponent } from "./components/placement-viewer.component";
import { ViewPlacementComponent } from "./components/view-placement.component";
import { ViewPlacementsComponent } from "./components/view-placements.component";
import { PlacementEditorComponent } from "./components/placement-editor.component";
import { FmEntriesCoreModule } from "../entries/entries.module";
import { FmCoreModule } from "../core/core.module";

@NgModule({
    imports: [
        CommonModule,
		FormsModule,
        ReactiveFormsModule,
        RouterModule,
        
        MatButtonModule,
        MatChipsModule,
        MatDatepickerModule,
        MatIconModule,
		MatInputModule,
        MatMenuModule,
        MatNativeDateModule,
        
        FmEntriesCoreModule, 
        FmCoreModule
    ],
    declarations: [
        PlacementEditorComponent,
        PlacementsViewerComponent,
        PlacementViewerComponent,
        OrderByPlacementDatePipe,
        ViewPlacementComponent,
        ViewPlacementsComponent
    ],
    exports: [
        PlacementsViewerComponent,
        PlacementViewerComponent
    ]
})
export class FmPlacementsCoreModule { 
    constructor(private dateAdapter:DateAdapter<Date>) {
        dateAdapter.setLocale('en-GB');
    }
}

/* I have split these modules as we do not want the routing included
* in other modules that include this as it causes issues with the "" path
* being matched. Not sure if this is a bug or by design?!
*/

@NgModule({
    imports:      [
        FmPlacementsCoreModule,
        FmPlacementsRoutingModule
    ]
})
export class FmPlacementsModule {}