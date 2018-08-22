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
         OnInit} from "@angular/core";

import { SkillService } from "../skill.service";
import { SkillFiltersService } from "../skill-filters.service";

@Component({
  selector: "skill-search",
  template: `  
  <div class="input-group form-group mb-0">
    <label class="sr-only" for="searchTerm">Search Term</label>
    <input class="form-control" type="text" autocomplete="off" [(ngModel)]="searchTerm" placeholder="Search..." id="searchTerm" [attr.value]="searchTerm" (keyup.enter)="addSearch()">
    <span *ngIf="searchTerm" class="input-group-append">
      <button class="btn btn-secondary" type="button" (click)="clearSearch()"><i class="fa fa-times" aria-hidden="true"></i><span class="sr-only">Clear</span></button>
    </span>
    <span class="input-group-append">
      <button class="btn btn-secondary" type="button" (click)="addSearch()"><i class="fa fa-search" aria-hidden="true"></i><span class="sr-only">Search</span></button>
    </span>
  </div>`
})
export class SkillSearchComponent implements OnInit {
  searchTerm = "";

  constructor(
    private skillService: SkillService,
    private skillFiltersService: SkillFiltersService) { }

  ngOnInit() {
    this.searchTerm = this.skillFiltersService.searchTerms.join(" ");
  }

  addSearch() {
    this.skillFiltersService.addSearch(this.searchTerm);
  }

  clearSearch() {
    this.searchTerm = "";
    this.skillFiltersService.clearSearch();
  }
}