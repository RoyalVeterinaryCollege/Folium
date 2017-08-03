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
import { MatDialogModule, MatChipsModule, MAT_DATE_FORMATS } from "@angular/material";

import { ModalModule } from "ngx-bootstrap";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { HAMMER_GESTURE_CONFIG } from "@angular/platform-browser";
import { MATERIAL_COMPATIBILITY_MODE } from '@angular/material';

import { appRouting,
         appRoutingProviders } from "./app.routing";
import { SkillService } from "./skills/skill.service";
import { SkillBundleService }     from "./skills/skill-bundle.service";
import { SkillAssessmentService }     from "./skills/skill-assessment.service";
import { EntriesService } from "./entries/entries.service";
import { AppComponent }   from "./app.component";
import { MatGestureConfig }     from "./skills/assessment-slider.component";
import { HomeComponent } from "./home/home.component";
import { UserService } from "./user/user.service";
import { PlacementsService } from "./placements/placements.service";
import { SecurityService } from "./common/security.service";
import { FmCommonModule } from "./common/common.module";
import { AuthGuard } from "./common/auth-guard.service";
import { NotificationService } from "./common/notification.service";
import { ResponseService } from "./common/response.service";
import { GlobalErrorHandler } from "./common/global-error.handler";
import { HttpService } from "./common/http.service";
import { FOLIUM_DATE_FORMATS } from "./common/date-formats";
import { CurrentUserResolve } from "./user/user-resolve.service";
import { FmUserModule } from "./user/user.module";
import { Slim } from "./slim/slim.angular2";

export function onInitApp(securityService: SecurityService) {
	// NOTE: this factory needs to return a function (that then returns a promise)
	return () => securityService.isAuthenticated;
}

@NgModule({
    imports: [
        BrowserAnimationsModule,
        BrowserModule,
        HttpModule,
        
        MatDialogModule,
        
        appRouting,
        FmCommonModule,
        FmUserModule
    ],
    declarations: [
        AppComponent,
        HomeComponent
    ],
    providers: [
        AuthGuard,
        CurrentUserResolve,
        EntriesService,
        NotificationService,        
        PlacementsService,
        ResponseService,
        SecurityService,
        SkillAssessmentService,
        SkillBundleService,
        SkillService,
		UserService,
		{
			'provide': APP_INITIALIZER, // Calls a function when the app starts, used to ensure the user is authenticated.
			'useFactory': onInitApp,
			'deps': [SecurityService],
			'multi': true,
		},
        { provide: ErrorHandler, useClass: GlobalErrorHandler },
        { provide: HAMMER_GESTURE_CONFIG, useClass: MatGestureConfig },
        { provide: Http, useClass: HttpService },
        { provide: MAT_DATE_FORMATS, useValue: FOLIUM_DATE_FORMATS},
        { provide: MATERIAL_COMPATIBILITY_MODE, useValue: true },
	],
    bootstrap:    [ AppComponent ]
})

export class AppModule {}