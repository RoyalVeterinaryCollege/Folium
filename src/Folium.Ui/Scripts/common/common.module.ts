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

import { AlertModule, ModalModule } from "ngx-bootstrap";

import { DialogDeleteConfirmComponent } from "./dialog-delete-confirm.component";
import { DialogHelpComponent } from "./dialog-help.component";
import { FormAutoSaveDirective } from "./form-autosave.directive";
import { NotificationsComponent } from "./notifications.component";
import { SignInComponent } from "./sign-in.component";
import { ActiveElementComponent } from "./active-element.component";
import { TinyMceDirective } from "./tinymce.directive";


@NgModule({
    imports: [        
        AlertModule.forRoot(),        
        ModalModule.forRoot(),

        CommonModule
    ],
    declarations: [
        ActiveElementComponent,
        DialogDeleteConfirmComponent,
        DialogHelpComponent,
        FormAutoSaveDirective,
        NotificationsComponent,
        SignInComponent,
        TinyMceDirective
    ],
    exports: [
        ActiveElementComponent,
        DialogDeleteConfirmComponent,
        DialogHelpComponent,
        FormAutoSaveDirective,
        NotificationsComponent,
        SignInComponent,
        TinyMceDirective
    ],
    entryComponents: [ DialogDeleteConfirmComponent, DialogHelpComponent ]
})

export class FmCommonModule {}