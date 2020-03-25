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

import { SkillBundleService } from "../skill-bundle.service";
import { User, SkillSet, SkillGroup, SkillGrouping } from "../../core/dtos";
import { ActivatedRoute } from "@angular/router";
import { DialogManageUserSkillSetsComponent } from "../../user/components/dialog-manage-user-skill-sets.component";
import { SkillFiltersService } from "../skill-filters.service";
import { MatDialog } from "@angular/material/dialog";
import { DialogChangeSkillSetComponent } from "./dialog-change-skill-set.component";
import { UserService } from "../../user/user.service";
import { NotificationService } from "../../core/notification.service";
import { SkillService } from "../skill.service";
import { SkillAssessmentService } from "../skill-assessment.service";
import { DialogChangeSkillGroupingComponent } from "./dialog-change-skill-grouping.component";

@Component({
	templateUrl: "view-skills.component.html",
	providers: [SkillFiltersService] // Use a new instance of the filters.
})
export class ViewSkillsComponent implements OnInit {
	user: User;
	skillSets: SkillSet[];
	skillGroupings: SkillGrouping[];
	skillGrouping: SkillGrouping;
	skillSet: SkillSet;
	skillGroups: SkillGroup[];
	
	constructor(
		private skillBundleService: SkillBundleService,
		private skillFiltersService: SkillFiltersService,
		private skillAssessmentService: SkillAssessmentService,
		private skillService: SkillService,
		private notificationService: NotificationService,
		private userService: UserService,
		private route: ActivatedRoute,
		private dialog: MatDialog) { }

	ngOnInit() {
		this.skillBundleService.resetBundle();
		this.route.data.forEach((data: { currentUser: User }) => {
			this.user = data.currentUser;
			this.loadSkillSets();
		});
	}
	
	onSelectUserSkillSetClick() {
		this.dialog.open(DialogManageUserSkillSetsComponent, {
		  data: this.user
		}).afterClosed().subscribe(_ => {
		  this.setSkillSet();
		});
	}

	onChangeSkillSetClick() {
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
	
	onChangeSkillGroupingClick() {		
		this.dialog.open(DialogChangeSkillGroupingComponent, {
		  data: {skillGroupings: this.skillGroupings, selectedSkillGrouping: this.skillGrouping}
		}).afterClosed().subscribe(skillGrouping => {
		  if(skillGrouping && this.skillGrouping.id !== skillGrouping.id) {
				this.skillGrouping = skillGrouping;				
				this.skillService.selectedSkillGroupings[this.skillSet.id] = this.skillGrouping;
				this.loadSkillGroups();
		  }
		});
	}

	private loadSkillSets() {
		this.userService.getSkillSets(this.user)
		  .subscribe(skillSets => {
				this.skillSets = skillSets;
				// Can't do anything if we don't have any skill sets.
				if(skillSets.length === 0) return;
		
				// If we don't have a selected skillset then pick the first one.
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
	  
	private setSkillSet() {
		// Can't do anything if we don't have any skill sets.
		if(this.skillSets.length === 0) return;

		// If we don't have a selected skillset then pick the first one.
		this.skillSet = this.skillSets.find(s => s.selected);
		if(!this.skillSet){
			this.skillSet = this.skillSets[0];
			this.skillSet.selected = true;
		}
		this.loadSkillGroups();
	}
  
	private loadSkillGroups() {
		this.skillGroups = undefined;
		this.skillService.getSkillGroupings(this.skillSet.id)
			.subscribe(skillGroupings => {
				this.skillGroupings = skillGroupings;
				// Can't do anything if we don't have any skill groupings.
				if(skillGroupings.length === 0) return;
		
				// Get the selected grouping.
				this.skillGrouping = this.skillService.selectedSkillGroupings[this.skillSet.id];
				if(!this.skillGrouping) {
					this.skillGrouping = this.skillService.getDefaultSkillGrouping(skillGroupings);
					this.skillService.selectedSkillGroupings[this.skillSet.id] = this.skillGrouping;
				}
				this.skillService.getSkillGroups(this.skillSet.id, this.skillGrouping.id)
				.subscribe(skillGroups => {
					this.skillAssessmentService.setUserSkillAssessmentsForSkillGroups(this.user.id, this.skillSet.id, skillGroups)
						.subscribe(_ => {
							this.skillGroups = skillGroups
						},
						(error: any) => this.notificationService.addDanger(`There was an error trying to set your skill levels, please try again.
							${error}`));
				},
				(error: any) => this.notificationService.addDanger(`There was an error trying to load the skill groups, please try again.
					${error}`)); 
			},
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the skill groupings, please try again.
        ${error}`));    
  }
}