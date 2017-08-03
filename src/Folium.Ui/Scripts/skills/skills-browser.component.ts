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
import {  Component,
          OnInit,
          Input,
          Output,
          EventEmitter
        } from "@angular/core";

import { Subscription } from "rxjs/subscription";

import {  SkillFilterFacet,
          SkillGroup,
          SelfAssessmentScale,
          SelfAssessment
        } from "../dtos";
import { SkillFiltersService } from "./skill-filters.service";
import { SkillBundleService } from "./skill-bundle.service";
import { SkillService } from "./skill.service";
import { SkillAssessmentService } from "./skill-assessment.service";
import { SkillSetSelectionService } from "../skill-set/selection.service";
import { NotificationService } from "../common/notification.service";

@Component({
  templateUrl: "html/skills/skills-browser.component.html",
  selector: "skills-browser",
  providers: [SkillFiltersService] // Use a new instance of the filters.
})
export class SkillsBrowserComponent implements OnInit {

  filters: SkillFilterFacet[] = [];
  filterCount: number = 0;
  private filterFacetUpdated$: Subscription;

  constructor(
	  private skillFiltersService: SkillFiltersService,
		private skillBundleService: SkillBundleService,
		private skillService: SkillService,
		private skillAssessmentService: SkillAssessmentService,
		private skillSetSelectionService: SkillSetSelectionService,
		private notificationService: NotificationService) { }

  @Input()
  autoSave: boolean = false;

  @Input()
  skillGroups: SkillGroup[];
  
  @Output()
  selfAssessmentChange = new EventEmitter<SelfAssessment>();
  
  ngOnInit() {
	  this.filterFacetUpdated$ = this.skillFiltersService.onFilterFacetUpdated.subscribe(f => this.onFilterFacetUpdated());
	  this.filters = this.skillFiltersService.filterFacets;

    if(!this.skillGroups){
      // If we haven't been supplied any skills then load them.
      this.loadSkills();
    }
  }

	loadSkills() {
		this.skillService.getSkillGroups(this.skillSetSelectionService.skillSet.id)
			.subscribe(skillGroups => {
				this.skillAssessmentService.setSkillAssessmentsForSkillGroups(this.skillSetSelectionService.skillSet.id, skillGroups);
				this.skillGroups = skillGroups;
		},
		(error: any) => this.notificationService.addDanger(`There was an error trying to load the skills, please try again.
			${error}`)); 
	}

  onRemoveFilterFacetClick(filter: SkillFilterFacet) {
    this.skillFiltersService.removeFilterFacet(filter);
  }

  onRemoveAllFilterFacetsClick() {
    this.skillFiltersService.clearFilterFacets();
  }

  private onFilterFacetUpdated(){
    // A filter has updated.
    this.filters = this.skillFiltersService.filterFacets;
    this.filterCount = this.filters.length;
  }
}