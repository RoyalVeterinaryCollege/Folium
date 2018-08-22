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
import { Component, OnInit, Input, Output, EventEmitter } from "@angular/core"
import { MatCheckboxChange } from '@angular/material';

import { User, SkillSet } from "../../core/dtos";
import { UserService } from "../user.service";
import { SkillService } from "../../skills/skill.service";
import { NotificationService } from "../../core/notification.service";

@Component({
	selector: "manage-user-skill-sets",
	templateUrl: "manage-user-skill-sets.component.html"
})
export class ManageUserSkillSetsComponent implements OnInit {
	skillSets: SkillSet[];
	userSkillSets: SkillSet[];

	@Input()
	user: User;
	
	@Output() 
	onDone = new EventEmitter();

	constructor(
		private skillService: SkillService,
		private userService: UserService,
		private notificationService: NotificationService) {
	}

	ngOnInit(): void {
		this.loadSkillSets();
	}	
	
	onSkillSetCheckboxChange(event: MatCheckboxChange, skillSet: SkillSet) {
		if(event.checked) {
			// Add the skill set to the users' list.
			this.userService
				.addUserSkillSet(this.user, skillSet.id)
				.subscribe(_ => this.userSkillSets.push(skillSet),
				(error: any) => {
					event.source.checked = false;
					this.notificationService.addDanger(`There was an error trying to add the skill set to your list, please try again.
					${error}`)
				});
		} else {
			// Remove the skill set from the users' list.
			this.userService
				.removeUserSkillSet(this.user, skillSet.id)
				.subscribe(_ => this.userSkillSets = this.userSkillSets.filter(set => set.id !== skillSet.id),
				(error: any) => {
					event.source.checked = true;
					this.notificationService.addDanger(`There was an error trying to remove skill set from your list, please try again.
					${error}`)
				});
		}
	}

	isInUserSkillSet(skillSet: SkillSet) {
		return this.userSkillSets.findIndex(set => set.id === skillSet.id) >= 0;
	}

	onDoneClick(){
		this.onDone.emit();
	}

	private loadSkillSets() {
		this.skillService
			.getSkillSets()
			.subscribe((skillSets: SkillSet[]) => {
				this.skillSets = skillSets;
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the skill sets, please try again.
				${error}`));  
		
		this.userService
			.getSkillSets(this.user)
			.subscribe((skillSets: SkillSet[]) => {
				this.userSkillSets = skillSets;
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the user skill sets, please try again.
				${error}`));  
	}
}
