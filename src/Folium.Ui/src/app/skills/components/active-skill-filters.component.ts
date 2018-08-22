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
          OnDestroy
        } from "@angular/core";

import { Subscription } from "rxjs";

import {  SkillFilterFacet } from "../../core/dtos";
import { SkillFiltersService } from "../skill-filters.service";

@Component({
  templateUrl: "active-skill-filters.component.html",
  selector: "active-skill-filters"
})
export class ActiveSkillFiltersComponent implements OnInit, OnDestroy {
  filters: SkillFilterFacet[] = [];

  private filterFacetUpdated$: Subscription;

  constructor(
	  private skillFiltersService: SkillFiltersService) { }
  
  ngOnInit() {
	  this.filterFacetUpdated$ = this.skillFiltersService.onFilterFacetUpdated.subscribe(f => this.onFilterFacetUpdated());
	  this.filters = this.skillFiltersService.filterFacets;
  }

  onRemoveFilterFacetClick(filter: SkillFilterFacet) {
    this.skillFiltersService.removeFilterFacet(filter);
  }

  onRemoveAllFilterFacetsClick() {
    this.skillFiltersService.clearFilterFacets();
  }
  
  ngOnDestroy() {
    this.filterFacetUpdated$.unsubscribe();
  }

  private onFilterFacetUpdated(){
    // A filter has updated.
    this.filters = this.skillFiltersService.filterFacets;
  }
}