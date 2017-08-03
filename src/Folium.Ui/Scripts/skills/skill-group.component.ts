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
import { Component, Input, ChangeDetectionStrategy, Output, EventEmitter } from "@angular/core";

import { Skill, SkillGroup, SelfAssessmentScale, SelfAssessment } from "../dtos";

@Component({
  selector: "skill-group",
  templateUrl: "html/skills/skill-group.component.html"
})
export class SkillGroupComponent {
  @Input()
  skillGroup: SkillGroup;

  @Input()
  selfAssessmentScales: SelfAssessmentScale[];

  @Input()
  expanded: boolean;

  @Input()
  readOnly: boolean;

  @Input()
  autoSave: boolean;

  @Output()
  selfAssessmentChange = new EventEmitter<SelfAssessment>();

  constructor() { }

  hasSkillsToDisplay(skills: Skill[]) {
    return skills.some(s => s.assessment.hidden === false);
  }

  trackSkillGroup(index, skillGroup: SkillGroup) {
    return skillGroup.id;
  }
}