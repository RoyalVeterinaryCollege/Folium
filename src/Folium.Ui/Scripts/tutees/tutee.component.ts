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
import {
	Component,
	OnInit
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { MatDialog } from "@angular/material";

import { Subscription } from "rxjs/Subscription";

import { UserService } from "../user/user.service";
import { User, TuteeGroup, Placement, SkillSet, SkillGroup } from "../dtos";
import { NotificationService } from "../common/notification.service";
import { SkillFiltersService } from "../skills/skill-filters.service";
import { SkillService } from "../skills/skill.service";
import { DialogChangeSkillSetComponent } from "../skills/dialog-change-skill-set.component";
import { SkillAssessmentService } from "../skills/skill-assessment.service";

@Component({
    templateUrl: "html/tutees/tutee.component.html",
    selector: "tutee",
	providers: [SkillFiltersService] // Use a new instance of the filters.
})
export class TuteeComponent implements OnInit {	
	user: User;
	placement: Placement;
    tuteeGroups: TuteeGroup[];
	tuteeContent = TuteeContent;
	currentContent: TuteeContent;
	skillSets: SkillSet[] = [];
	skillSet: SkillSet;
	skillGroups: SkillGroup[];
	courseId: number;

	private paramsSubscription$: Subscription;

	constructor(
		private skillFiltersService: SkillFiltersService,
		private skillAssessmentService: SkillAssessmentService,
		private skillService: SkillService,
		private userService: UserService,
		private route: ActivatedRoute,
		private notificationService: NotificationService,
	    private dialog: MatDialog) { }

	ngOnInit() {
		this.paramsSubscription$ = this.route.paramMap.subscribe(params => {
			// Load the placement.
			this.loadUser(+params.get('id'));
			this.courseId = +params.get('courseId');
		});	  
	}
	
	loadSkills() {
		this.currentContent = undefined;
		this.userService.getSkillSets(this.user)
			.subscribe(skillSets => {
				this.skillSets = skillSets.filter(s => s.courseIds.includes(this.courseId));
				this.currentContent = TuteeContent.Skills;
				// Can't do anything if we don't have any skill sets.
				if(skillSets.length === 0) return;
		
				// If we don't have aselected skillset then pick the first one.
				this.skillSet = this.skillSets.find(s => s.selected);
				if(!this.skillSet){
					this.skillSet = this.skillSets[0];
					this.skillSet.selected = true;
				}
				this.loadSkillGroups();
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the skill sets, please try again.
			${error}`)); 
	}

	loadEntries() {
		this.currentContent = TuteeContent.Entries;
	}
		
	loadPlacements() {
		this.currentContent = TuteeContent.Placements;
	}

	onViewPlacement(placement: Placement){
		this.placement = placement;
		this.currentContent = TuteeContent.Placement;
	}
	
	onChangeSkillSetClick(){
		this.dialog.open(DialogChangeSkillSetComponent, {
			data: this.skillSets
		}).afterClosed().subscribe(_ => {
			let selectedSkillSet = this.skillSets.find(s => s.selected);
			if(selectedSkillSet.id !== this.skillSet.id) {
				this.skillSet = selectedSkillSet;
				this.skillFiltersService.clearFilterFacets();
				this.loadSkillGroups();
		  	}
		});
	  }

	ngOnDestroy() {
		this.paramsSubscription$.unsubscribe();
	}
  
	private loadUser(id: number) {
	  return this.userService.getUser(id)
		.subscribe((user: User) => {
			this.user = user;
			this.loadSkills();
		},
		(error: any) => this.notificationService.addDanger(`There was an error trying to load the tutee, please try again.
			${error}`));  
	}
  
	private loadSkillGroups() {
		this.skillService.getSkillGroups(this.skillSet.id)
		  .subscribe(skillGroups => {
			this.skillAssessmentService.setSkillAssessmentsForSkillGroups(this.user.id, this.skillSet.id, skillGroups)
			  .subscribe(_ => {
				this.skillGroups = skillGroups
			  });
		  },
		  (error: any) => this.notificationService.addDanger(`There was an error trying to load the skills, please try again.
			${error}`)); 
	  }
  
}
enum TuteeContent {Entries, Skills, Placements, Placement, Reports}