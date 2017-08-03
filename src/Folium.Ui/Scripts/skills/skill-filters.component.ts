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
import { Component,
         Input,
         Output,
         OnInit,
         OnDestroy } from "@angular/core";

import { SkillFilter, SkillSet, SkillFilterFacet } from "../dtos";
import { SkillService } from "./skill.service";
import { SkillFiltersService } from "./skill-filters.service";
import { SkillSetSelectionService } from "../skill-set/selection.service";
import { NotificationService } from "../common/notification.service";

import { Subscription } from "rxjs/subscription";

@Component({
  selector: "skill-filters",
  templateUrl: "html/skills/skill-filters.component.html"
})
export class SkillFiltersComponent implements OnInit, OnDestroy {
  skillFilters: SkillFilter[];

  private skillSet: SkillSet;
  private selectedFacets: SkillFilterFacet[] = [];
  private onFilterUpdated$: Subscription;
  private onSkillSetChanged$: Subscription;

  constructor(
    private skillSetSelectionService: SkillSetSelectionService,
    private skillService: SkillService,
	  private skillFiltersService: SkillFiltersService,
	  private notificationService: NotificationService) { }

  ngOnInit() {
    this.skillSet = this.skillSetSelectionService.skillSet;
    this.selectedFacets = this.skillFiltersService.filterFacets;
    this.onFilterUpdated$ = this.skillFiltersService.onFilterFacetUpdated.subscribe(f => this.onFacetUpdated(f));
    this.onSkillSetChanged$ = this.skillSetSelectionService.onSkillSetChanged.subscribe(s => this.onSkillSetChanged(s));

    // Populate the filters and any selected facets.
    this.getSkillFilters();
  }

  getSelectedFacetCount(skillFilter: SkillFilter) {
    return skillFilter.facets.filter(f => f.selected).length;
  }

  onCheckboxChange(facet: SkillFilterFacet, checked: boolean) {
    facet.selected = checked;
    // Add or remove the facet from the filter list and raise the event.
    if (checked) {
      this.skillFiltersService.addFilterFacet(facet);
    } else {
      this.skillFiltersService.removeFilterFacet(facet);
    }
  }

  ngOnDestroy() {
    this.onFilterUpdated$.unsubscribe();
    this.onSkillSetChanged$.unsubscribe();
  }

  private onSkillSetChanged(s: SkillSet) {
    // Clear any current filters and search terms.
    this.skillFiltersService.clearFilterFacets();
    this.skillSet = s;
    this.getSkillFilters();
  }

  private onFacetUpdated(facet: SkillFilterFacet) {
    // Loop all the facets in the skill filters, to find the one to update.
    for (let skillFilter of this.skillFilters) {
      let index = skillFilter.facets.findIndex(f => f.id === facet.id);
      if (index >= 0) {
        skillFilter.facets[index] = facet;
        // There will only be one matching facet, so once we have found it we can break the loop.
        break;
      }
    }
  }

  private getSkillFilters() {
    this.skillService.getSkillFilters(this.skillSet.id)
		.subscribe(s => {
			this.skillFilters = s;
			// Loop each of the facets and call updated to include it.
			for (let facet of this.selectedFacets) {
			  this.onFacetUpdated(facet);
			}
		},
		(error: any) => this.notificationService.addDanger(`There was an error trying to load the filters, please try again.
      ${error}`));
  }
}