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
import { MatDialogModule } from "@angular/material/dialog";

import { AlertModule } from "ngx-bootstrap/alert";
import { ModalModule } from "ngx-bootstrap/modal";

import { DialogDeleteConfirmComponent } from "./components/dialog-delete-confirm.component";
import { DialogHelpComponent } from "./components/dialog-help.component";
import { FormAutoSaveDirective } from "./directives/form-autosave.directive";
import { NotificationsComponent } from "./components/notifications.component";
import { ModalSignInComponent } from "./components/modal-sign-in.component";
import { ActiveElementComponent } from "./components/active-element.component";
import { TinyMceDirective } from "./directives/tinymce.directive";
import { SecureImagePipe } from "./pipes/secure-image.pipe";
import { HttpClientModule } from "@angular/common/http";
import { FormatBytesPipe } from "./pipes/format-bytes.pipe";
import { SafeHtmlPipe } from "./pipes/safe-html.pipe";
import { NlToBrPipe } from "./pipes/nl-to-br.pipe";
import { DialogConfirmComponent } from "./components/dialog-confirm.component";


@NgModule({
    imports: [        
        AlertModule.forRoot(),
        HttpClientModule,
        ModalModule.forRoot(),
        MatDialogModule,

        CommonModule
    ],
    declarations: [
        ActiveElementComponent,
        DialogConfirmComponent,
        DialogDeleteConfirmComponent,
        DialogHelpComponent,
        FormatBytesPipe,
        FormAutoSaveDirective,
        ModalSignInComponent,
        NlToBrPipe,
        NotificationsComponent,
        SafeHtmlPipe,
        SecureImagePipe,
        TinyMceDirective
    ],
    exports: [
        ActiveElementComponent,
        DialogConfirmComponent,
        DialogDeleteConfirmComponent,
        DialogHelpComponent,
        FormatBytesPipe,
        FormAutoSaveDirective,
        ModalSignInComponent,
        NlToBrPipe,
        NotificationsComponent,
        SafeHtmlPipe,
        SecureImagePipe,
        TinyMceDirective
    ],
    entryComponents: [ DialogConfirmComponent, DialogDeleteConfirmComponent, DialogHelpComponent ]
})

export class FmCoreModule {}
