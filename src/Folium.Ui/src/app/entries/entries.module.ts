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
    MatDialogModule,  
    MatIconModule} from "@angular/material";

import { ButtonsModule } from "ngx-bootstrap/buttons";
import { ModalModule } from "ngx-bootstrap/modal";

import { FmEntriesRoutingModule } from "./entries-routing.module";
import { FmSkillsCoreModule }     from "../skills/skills.module";
import { EntriesViewerComponent } from "./components/entries-viewer.component";
import { EntryViewerComponent } from "./components/entry-viewer.component";
import { EntryEditorComponent } from "./components/entry-editor.component";
import { ViewEntriesComponent } from "./components/view-entries.component";
import { DialogShareEntryComponent } from "./components/dialog-share-entry.component";
import { FmCoreModule } from "../core/core.module";
import { FmUserModule } from "../user/user.module";
import { CommentsComponent, OrderByCommentDatePipe } from "./components/comments.component";
import { ViewEntryComponent } from "./components/view-entry.component";

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
        MatIconModule,
		MatInputModule,
        MatMenuModule,
        
        ButtonsModule.forRoot(),
        ModalModule.forRoot(),

        FmCoreModule,
        FmUserModule,
        FmSkillsCoreModule
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
export class FmEntriesCoreModule { }

/* I have split these modules as we do not want the routing included
* in other modules that include this.
*/

@NgModule({
    imports: [
        FmEntriesCoreModule,
        FmEntriesRoutingModule
    ]
})
export class FmEntriesModule {}