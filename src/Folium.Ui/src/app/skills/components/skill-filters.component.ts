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
         OnInit,
         OnDestroy, 
         OnChanges,
         SimpleChanges,
         SimpleChange} from "@angular/core";

import { Subscription } from "rxjs";

import { SkillFilter, SkillFilterFacet } from "../../core/dtos";
import { SkillService } from "../skill.service";
import { SkillFiltersService } from "../skill-filters.service";
import { NotificationService } from "../../core/notification.service";


@Component({
  selector: "skill-filters",
  templateUrl: "skill-filters.component.html"
})
export class SkillFiltersComponent implements OnInit, OnDestroy, OnChanges {
  skillFilters: SkillFilter[];

	@Input()
  skillSetId: number;

  private selectedFacets: SkillFilterFacet[] = [];
  private onFilterUpdated$: Subscription;

  constructor(
    private skillService: SkillService,
	  private skillFiltersService: SkillFiltersService,
	  private notificationService: NotificationService) { }

  ngOnInit() {
    this.selectedFacets = this.skillFiltersService.filterFacets;
    this.onFilterUpdated$ = this.skillFiltersService.onFilterFacetUpdated.subscribe(f => this.onFacetUpdated(f));

    // Populate the filters and any selected facets.
    this.getSkillFilters();
  }

  ngOnChanges(changes: SimpleChanges) {
    // The skillset id can change, update the filters when it does.
    const skillSetId: SimpleChange = changes.skillSetId;
    if (skillSetId && (skillSetId.previousValue !== skillSetId.currentValue)) {
      this.getSkillFilters();
    }
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
    this.skillFilters = [];
    this.skillService.getSkillFilters(this.skillSetId)
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