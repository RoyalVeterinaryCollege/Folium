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
import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";

import { SkillSet } from "./../dtos";
import { SkillService } from "./../skills/skill.service";
import { SkillSetSelectionService } from "./selection.service";
import { NotificationService } from "../common/notification.service";

@Component({
  selector: "skill-set-selector",
  templateUrl: "html/skill-set/selector.component.html",
})
export class SkillSetSelectorComponent implements OnInit {
  skillSets: SkillSet[];
  selectedSkillSet: SkillSet;

  constructor(
    private router: Router,
    private skillService: SkillService,
	  private skillSetSelectionService: SkillSetSelectionService,
	  private notificationService: NotificationService) { }

  getSkillSets() {
    this.skillService.getSkillSets()
	  .subscribe(s => {
        this.skillSets = s;
      },
	  (error: any) => this.notificationService.addDanger(`There was an error trying to load the skills set, please try again.
      ${error}`));
  }

  ngOnInit() {
    this.getSkillSets();
    this.selectedSkillSet = this.skillSetSelectionService.skillSet;
  }

  onSkillSetClick(event: Event, skillSet: SkillSet) {
    event.preventDefault();
    if (this.skillSetSelectionService.skillSet && this.skillSetSelectionService.skillSet.id !== skillSet.id) {
      this.skillSetSelectionService.skillSet = skillSet;
      this.selectedSkillSet = skillSet;
    }
  }
}