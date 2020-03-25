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
import { MatAutocompleteModule } from "@angular/material/autocomplete";
import { MatButtonModule } from "@angular/material/button";
import { MatChipsModule } from "@angular/material/chips";
import { MatNativeDateModule, DateAdapter } from "@angular/material/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatDialogModule } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";
import { MatPaginatorModule } from "@angular/material/paginator";
import { MatSelectModule } from "@angular/material/select";
import { MatSortModule } from "@angular/material/sort";
import { MatTableModule } from "@angular/material/table";
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ScrollToModule } from "@nicky-lenaers/ngx-scroll-to";
import { ModalModule } from "ngx-bootstrap/modal";

import { ViewSelfAssessmentEngagementComponent } from "./components/view-self-assessment-engagement.component";
import { ViewReportsComponent } from "./components/view-reports.component";
import { FmCoreModule } from "../core/core.module";
import { FmSkillsCoreModule } from "../skills/skills.module";
import { ViewEntryEngagementComponent } from "./components/view-entry-engagement.component";
import { ViewPlacementEngagementComponent } from "./components/view-placement-engagement.component";
import { FmTuteesCoreModule } from "../tutees/tutees.module";
import { DialogMessageUsersComponent } from "./components/dialog-message-users.component";
import { FmUserModule } from "../user/user.module";
import { FmReportsRoutingModule } from "./reports-routing.module";

@NgModule({
    imports: [
        CommonModule,
		FormsModule,
        ReactiveFormsModule,
        RouterModule,
        
		MatAutocompleteModule,
        MatButtonModule,
        MatChipsModule,
        MatDatepickerModule,
        MatDialogModule,
        MatIconModule,
		MatInputModule,
        MatMenuModule,
        MatNativeDateModule,
        MatPaginatorModule,
        MatSelectModule,
        MatSortModule,
        MatTableModule,
        
        NgxChartsModule,

        FmCoreModule,
        FmSkillsCoreModule,
        FmTuteesCoreModule,
        FmUserModule,

		ModalModule.forRoot(),	
        ScrollToModule.forRoot()
    ],
    declarations: [
        DialogMessageUsersComponent,
        ViewEntryEngagementComponent,
        ViewPlacementEngagementComponent,
        ViewReportsComponent,
        ViewSelfAssessmentEngagementComponent
    ],
    exports: [
    ],
    entryComponents: [ DialogMessageUsersComponent ]
})
export class FmReportsCoreModule { 
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
        FmReportsCoreModule,
        FmReportsRoutingModule
    ]
})
export class FmReportsModule {}