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
import { FormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";

import { AccordionModule,
        ButtonsModule,
        CollapseModule,
		BsDropdownModule,
		ModalModule } from "ngx-bootstrap";

import { MatCheckboxModule, MatChipsModule, MatDialogModule, MatRadioModule } from "@angular/material";

import { skillsRouting } from "./skills.routing";
import { ViewSkillsComponent } from "./view-skills.component";
import { SkillFiltersPipe } from "./skill-filters.pipe";
import { SkillsListComponent } from "./skills-list.component";
import { SkillGroupListComponent } from "./skill-group-list.component";
import { SkillGroupComponent } from "./skill-group.component";
import { SkillFiltersComponent } from "./skill-filters.component";
import { SkillSearchComponent } from "./skill-search.component";
import { AssessmentSliderModule } from "./assessment-slider.component";
import { SkillFiltersService }     from "./skill-filters.service";
import { DialogChangeSkillSetComponent } from "./dialog-change-skill-set.component";
import { FilterSkillsComponent } from "./filter-skills.component";
import { ActiveSkillFiltersComponent } from "./active-skill-filters.component";

@NgModule({
    imports:      [
        CommonModule,
        FormsModule,
        RouterModule,
        
        MatCheckboxModule,
        MatRadioModule,
        MatChipsModule,
        MatDialogModule,

        AccordionModule.forRoot(),        
        BsDropdownModule.forRoot(),
        ButtonsModule.forRoot(),
		CollapseModule.forRoot(),
		ModalModule.forRoot(),		
        
        AssessmentSliderModule
    ],
    declarations: [
        ActiveSkillFiltersComponent,
        DialogChangeSkillSetComponent,
        FilterSkillsComponent,
        SkillFiltersComponent,
        SkillFiltersPipe,
        SkillGroupComponent,
        SkillGroupListComponent,
        SkillSearchComponent,
        SkillsListComponent,
        ViewSkillsComponent
    ],
    providers: [
        SkillFiltersService,
    ],
    exports: [
        ActiveSkillFiltersComponent,
        FilterSkillsComponent,
        SkillGroupListComponent,
        SkillSearchComponent,
    ],
	entryComponents: [DialogChangeSkillSetComponent]
})
export class FmSkillsCoreModule {}

/* I have split these modules as we do not want the routing included
* in other modules that include this as it causes issues with the "" path
* being matched. Not sure if this is a bug or by design?!
*/

@NgModule({
    imports:      [
        FmSkillsCoreModule,
        skillsRouting
    ]
})
export class FmSkillsModule {}