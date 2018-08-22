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
import { Component, Input } from "@angular/core";

import { Skill, SkillGroup, User } from "../../core/dtos";
import { SkillAssessmentService } from "../skill-assessment.service";

@Component({
  selector: "skill-group",
  template: `
  <skills-list [readOnly]="readOnly" [skills]="skillGroup.skills" [user]="user" [skillSetId]="skillSetId" [expanded]="expanded" [autoSave]="autoSave"></skills-list>
  <div *ngFor="let childSkillGroup of skillGroup.childGroups;trackBy: trackSkillGroup">	
    <div *ngIf="hasSkillsToDisplay(childSkillGroup.skills)">
      <h4>{{ childSkillGroup.name }}</h4>
      <hr class="mt-1 mb-1">
      <skill-group [readOnly]="readOnly" [skillGroup]="childSkillGroup" [user]="user" [skillSetId]="skillSetId" [expanded]="expanded" [autoSave]="autoSave"></skill-group>
    </div>
  </div>`
})
export class SkillGroupComponent {
  @Input()
  skillGroup: SkillGroup;

  @Input()
  user: User;

  @Input()
  skillSetId: number;

  @Input()
  expanded: boolean;

  @Input()
  readOnly: boolean;

  @Input()
  autoSave: boolean;

  constructor(
    private skillAssessmentService: SkillAssessmentService) { }

  hasSkillsToDisplay(skills: Skill[]) {
    return skills.some(s => s.assessment.hidden === false);
  }

  trackSkillGroup(index, skillGroup: SkillGroup) {
    return skillGroup.id;
  }
  
  getAverageSelfAssessmentFromGroup(skillGroup: SkillGroup) {
    return this.skillAssessmentService.getAverageSelfAssessmentFromGroup(skillGroup);
  }
}