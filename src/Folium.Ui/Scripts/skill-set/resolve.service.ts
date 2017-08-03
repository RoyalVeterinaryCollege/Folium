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
import { Injectable,  } from "@angular/core";
import { Router, Resolve } from "@angular/router";
import { SkillSet } from "./../dtos";
import { SkillSetSelectionService } from "./selection.service";
import { SkillService } from "../skills/skill.service";
import { NotificationService } from "../common/notification.service";

@Injectable()
export class SkillSetResolve implements Resolve<SkillSet> {

    constructor(
        private skillSetSelectionService: SkillSetSelectionService,
        private router: Router,
		private skillService: SkillService,
		private notificationService: NotificationService) { }

    resolve(): any {
        if (!this.skillSetSelectionService.skillSet) {
            // skillset has not been set.
            // Get the skill sets and select the first one.
			return this.skillService.getSkillSets()
			  .map(skillSets => {
                if (skillSets.length > 0) {
                  this.skillSetSelectionService.skillSet = skillSets[0];
                  return this.skillSetSelectionService.skillSet;
                }
				this.notificationService.addDanger("You do not have any skill sets.");
				// No skill sets.
				return undefined;
              },
			  (error: any) => this.notificationService.addDanger(`There was an error trying to load the skills set, please try again.
      ${error}`));
        }

        return this.skillSetSelectionService.skillSet;
    }
}