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
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { 
    MatInputModule, 
    MatDatepickerModule, 
    MatNativeDateModule, 
    DateAdapter,  
    MatButtonModule,
    MatMenuModule,
    MatChipsModule } from "@angular/material";

import { placementsRouting } from "./placements.routing";
import { PlacementsViewerComponent, OrderByPlacementDatePipe } from "./placements-viewer.component";
import { ViewPlacementComponent } from "./view-placement.component";
import { PlacementEditorComponent } from "./placement-editor.component";
import { EntriesCoreModule } from "../entries/entries.module";
import { FmCommonModule } from "../common/common.module";

@NgModule({
    imports: [
        CommonModule,
		FormsModule,
        ReactiveFormsModule,
        
        MatButtonModule,
        MatChipsModule,
        MatDatepickerModule,
		MatInputModule,
        MatMenuModule,
        MatNativeDateModule,
        
        EntriesCoreModule, 
        FmCommonModule,       
        placementsRouting,
    ],
    declarations: [
        PlacementEditorComponent,
        PlacementsViewerComponent,
        OrderByPlacementDatePipe,
		ViewPlacementComponent
    ]
})
export class PlacementsModule { 
  constructor(private dateAdapter:DateAdapter<Date>) {
    dateAdapter.setLocale('en-GB');
  }
}