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
import { NgModule, APP_INITIALIZER, ErrorHandler }from "@angular/core";
import { BrowserModule  } from "@angular/platform-browser";
import { MAT_DATE_FORMATS } from "@angular/material/core";
import { MatDialogModule } from "@angular/material/dialog";
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';

import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { HAMMER_GESTURE_CONFIG } from "@angular/platform-browser";

import { AppRoutingModule } from './app-routing.module';
import { SkillService } from "./skills/skill.service";
import { SkillBundleService } from "./skills/skill-bundle.service";
import { SkillAssessmentService } from "./skills/skill-assessment.service";
import { EntriesService } from "./entries/entries.service";
import { AppComponent } from "./app.component";
import { MatGestureConfig } from "./skills/components/assessment-slider.component";
import { HomeComponent } from "./home/home.component";
import { UserService } from "./user/user.service";
import { PlacementsService } from "./placements/placements.service";
import { ReportsService } from "./reports/reports.service";
import { SecurityService } from "./core/security.service";
import { FmCoreModule } from "./core/core.module";
import { AuthGuard } from "./core/auth-guard.service";
import { NotificationService } from "./core/notification.service";
import { JsonInterceptor } from "./core/json.interceptor";
import { AuthorizationInterceptor } from "./core/authorization.interceptor";
import { UrlInterceptor } from "./core/url.interceptor";
import { GlobalErrorHandler } from "./core/global-error.handler";
import { FOLIUM_DATE_FORMATS } from "./core/date-formats";
import { CurrentUserResolve } from "./user/user-resolve.service";
import { FmUserModule } from "./user/user.module";
import { FmEntriesRoutingModule } from "./entries/entries-routing.module";
import { FmPlacementsRoutingModule } from "./placements/placements-routing.module";
import { FmReportsRoutingModule } from "./reports/reports-routing.module";
import { FmTuteesRoutingModule } from "./tutees/tutees-routing.module";
import { FmEntriesModule } from "./entries/entries.module";
import { FmPlacementsModule } from "./placements/placements.module";
import { FmSkillsModule } from "./skills/skills.module";
import { FmTuteesModule } from "./tutees/tutees.module";
import { FmReportsModule } from "./reports/reports.module";
import { UppyModule } from "./uppy/uppy.module";

export function onInitApp(securityService: SecurityService) {
	// NOTE: this factory needs to return a function (that then returns a promise)
	return () => securityService.isAuthenticated;
}

@NgModule({
    imports: [
        BrowserAnimationsModule,
        BrowserModule,
        HttpClientModule,
        
        MatDialogModule,
        
        FmCoreModule,
        FmEntriesModule,
        FmPlacementsModule,
        FmReportsModule,
        FmSkillsModule,
        FmTuteesModule,
        FmUserModule,

        UppyModule,

        AppRoutingModule
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
        ReportsService,
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
        { 
            provide: ErrorHandler, 
            useClass: GlobalErrorHandler 
        },
        { 
            provide: HAMMER_GESTURE_CONFIG, 
            useClass: MatGestureConfig 
        },
        { 
            provide: MAT_DATE_FORMATS,
            useValue: FOLIUM_DATE_FORMATS
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: JsonInterceptor,
            multi: true
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthorizationInterceptor,
            multi: true
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: UrlInterceptor,
            multi: true
        }
	],
    bootstrap:    [ AppComponent ]
})

export class FmAppModule { }
