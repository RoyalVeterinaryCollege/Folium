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
    MdInputModule, 
    MdAutocompleteModule,
    MdButtonModule,
    MdMenuModule,
    MdChipsModule  } from "@angular/material";

import { ButtonsModule, ModalModule } from "ngx-bootstrap";

import { entriesRouting } from "./entries.routing";
import { SkillsCoreModule }     from "./../skills/skills.module";
import { ListEntriesComponent } from "./list-entries.component";
import { ViewEntryComponent } from "./view-entry.component";
import { EditEntryComponent } from "./edit-entry.component";
import { EntriesComponent } from "./entries.component";
import { FmCommonModule } from "../common/common.module";

@NgModule({
    imports: [
        CommonModule,
		FormsModule,
        ReactiveFormsModule,
        
		MdAutocompleteModule,
        MdButtonModule,
        MdChipsModule,
		MdInputModule,
        MdMenuModule,
        
        ButtonsModule.forRoot(),
        ModalModule.forRoot(),

        FmCommonModule,
        SkillsCoreModule
    ],
    declarations: [
        EditEntryComponent,
        EntriesComponent,
        ListEntriesComponent,
		ViewEntryComponent
    ],
    providers: [
    ],
    exports: [
        ListEntriesComponent
    ]
})
export class EntriesCoreModule { }

/* I have split these modules as we do not want the routing included
* in other modules that include this as it causes issues with the "" path
* being matched. Not sure if this is a bug or by design?!
*/

@NgModule({
    imports:      [
        entriesRouting,
        EntriesCoreModule
    ]
})
export class EntriesModule {}