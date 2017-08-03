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
import { NgModule, APP_INITIALIZER, ErrorHandler }       from "@angular/core";
import { BrowserModule  } from "@angular/platform-browser";
import { RouterModule } from "@angular/router";
import { HttpModule, Http } from "@angular/http";
import { FormsModule,
         ReactiveFormsModule } from "@angular/forms";
import { MdDialogModule, MdChipsModule, MD_DATE_FORMATS } from "@angular/material";

import { ModalModule, BsDropdownModule } from "ngx-bootstrap";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { HAMMER_GESTURE_CONFIG } from "@angular/platform-browser";

import { appRouting,
         appRoutingProviders } from "./app.routing";
import { SkillSetResolve } from "./skill-set/resolve.service";
import { SkillSetSelectionService } from "./skill-set/selection.service";
import { SkillService } from "./skills/skill.service";
import { SkillBundleService }     from "./skills/skill-bundle.service";
import { SkillAssessmentService }     from "./skills/skill-assessment.service";
import { EntriesService } from "./entries/entries.service";
import { AppComponent }   from "./app.component";
import { MdGestureConfig }     from "./skills/assessment-slider.component";
import { HomeComponent } from "./home/home.component";
import { Slim } from "./slim/slim.angular2";
import { UserService } from "./user/user.service";
import { UserPicDirective } from "./user/user-pic.directive";
import { UserMenuComponent } from "./user/user-menu.component";
import { EditUserComponent } from "./user/user-edit.component";
import { UserCardComponent } from "./user/user-card.component";
import { PlacementsService } from "./placements/placements.service";
import { SecurityService } from "./common/security.service";
import { FmCommonModule } from "./common/common.module";
import { AuthGuard } from "./common/auth-guard.service";
import { NotificationService } from "./common/notification.service";
import { ResponseService } from "./common/response.service";
import { GlobalErrorHandler } from "./common/global-error.handler";
import { HttpService } from "./common/http.service";
import { FOLIUM_DATE_FORMATS } from "./common/date-formats";

export function onInitApp(securityService: SecurityService) {
	// NOTE: this factory needs to return a function (that then returns a promise)
	return () => securityService.isAuthenticated;
}

@NgModule({
    imports: [
        BrowserAnimationsModule,
        BrowserModule,        
        FormsModule,
        HttpModule,
        ReactiveFormsModule,
        
        MdDialogModule,

        BsDropdownModule.forRoot(),
        ModalModule.forRoot(),
        
        appRouting,
        FmCommonModule
    ],
    declarations: [
        AppComponent,
        EditUserComponent,
        HomeComponent,
        Slim,
        UserCardComponent,
        UserMenuComponent,
        UserPicDirective
    ],
    providers: [
        AuthGuard,
        EntriesService,
        NotificationService,        
        PlacementsService,
        ResponseService,
        SecurityService,
        SkillAssessmentService,
        SkillBundleService,
        SkillService,
        SkillSetResolve,
        SkillSetSelectionService,
		UserService,
		{
			'provide': APP_INITIALIZER, // Calls a function when the app starts, used to ensure the user is authenticated.
			'useFactory': onInitApp,
			'deps': [SecurityService],
			'multi': true,
		},
        { provide: ErrorHandler, useClass: GlobalErrorHandler },
        { provide: HAMMER_GESTURE_CONFIG, useClass: MdGestureConfig },
        { provide: Http, useClass: HttpService },
        { provide: MD_DATE_FORMATS, useValue: FOLIUM_DATE_FORMATS}
	],
    bootstrap:    [ AppComponent ]
})

export class AppModule {}