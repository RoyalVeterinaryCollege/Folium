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
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDialogModule } from "@angular/material/dialog";
import { MatExpansionModule } from "@angular/material/expansion";

import { ModalModule, BsDropdownModule } from "ngx-bootstrap";

import { DialogUserEditorComponent } from "./components/dialog-user-editor.component";
import { UserMenuComponent } from "./components/user-menu.component";
import { UserPicDirective } from "./user-pic.directive";
import { Slim } from "../slim/slim.angular2";
import { FormsModule } from "@angular/forms";
import { DialogManageUserSkillSetsComponent } from "./components/dialog-manage-user-skill-sets.component";
import { ManageUserSkillSetsComponent } from "./components/manage-user-skill-sets.component";
import { UserPhotoEditorComponent } from "./components/user-photo-editor.component";
import { UserCardComponent } from "./components/user-card.component";

@NgModule({
    imports: [             
        ModalModule.forRoot(),        
        BsDropdownModule.forRoot(),

        MatCheckboxModule,
        MatDialogModule,
        MatExpansionModule,
        
        CommonModule,
        FormsModule
    ],
    declarations: [        
        DialogManageUserSkillSetsComponent,
        ManageUserSkillSetsComponent,
        DialogUserEditorComponent,
        Slim,
        UserCardComponent,
        UserMenuComponent,
        UserPhotoEditorComponent,
        UserPicDirective
    ],
    exports: [
        DialogManageUserSkillSetsComponent,
        ManageUserSkillSetsComponent,
        DialogUserEditorComponent,
        Slim,
        UserCardComponent,
        UserMenuComponent,
        UserPicDirective,
    ],
	entryComponents: [ DialogManageUserSkillSetsComponent, DialogUserEditorComponent ]
})
export class FmUserModule {}