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
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { 
    MatInputModule, 
    MatAutocompleteModule,
    MatButtonModule,
    MatMenuModule,
    MatChipsModule,
    MatDialogModule  } from "@angular/material";

import { ButtonsModule, ModalModule } from "ngx-bootstrap";

import { entriesRouting } from "./entries.routing";
import { SkillsCoreModule }     from "./../skills/skills.module";
import { EntriesViewerComponent } from "./entries-viewer.component";
import { EntryViewerComponent } from "./entry-viewer.component";
import { EntryEditorComponent } from "./entry-editor.component";
import { ViewEntriesComponent } from "./view-entries.component";
import { DialogShareEntryComponent } from "./dialog-share-entry.component";
import { FmCommonModule } from "../common/common.module";
import { FmUserModule } from "../user/user.module";
import { CommentsComponent, OrderByCommentDatePipe } from "./comments.component";
import { ViewEntryComponent } from "./view-entry.component";

@NgModule({
    imports: [
        CommonModule,
		FormsModule,
        ReactiveFormsModule,
        RouterModule,
        
		MatAutocompleteModule,
        MatButtonModule,
        MatChipsModule,
        MatDialogModule,
		MatInputModule,
        MatMenuModule,
        
        ButtonsModule.forRoot(),
        ModalModule.forRoot(),

        FmCommonModule,
        FmUserModule,
        SkillsCoreModule
    ],
    declarations: [
        CommentsComponent,
		DialogShareEntryComponent,
        EntriesViewerComponent,
        EntryEditorComponent,
        EntryViewerComponent,
        OrderByCommentDatePipe,
        ViewEntriesComponent,
        ViewEntryComponent
    ],
    providers: [
    ],
    exports: [
        EntriesViewerComponent
	],
	entryComponents: [DialogShareEntryComponent]
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