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
import { MatDialog } from '@angular/material';

import { Subscription } from "rxjs/subscription";

import {  SkillFilterFacet,
          SkillGroup,
          SelfAssessmentScale,
          SelfAssessment,
          SkillSet,
          User
        } from "../dtos";
import { SkillFiltersService } from "./skill-filters.service";
import { SkillBundleService } from "./skill-bundle.service";
import { SkillService } from "./skill.service";
import { SkillAssessmentService } from "./skill-assessment.service";
import { NotificationService } from "../common/notification.service";
import { UserService } from "../user/user.service";
import { DialogManageUserSkillSetsComponent } from "../user/dialog-manage-user-skill-sets.component";
import { DialogChangeSkillSetComponent } from "./dialog-change-skill-set.component";

@Component({
  templateUrl: "html/skills/skills-browser.component.html",
  selector: "skills-browser",
  providers: [SkillFiltersService] // Use a new instance of the filters.
})
export class SkillsBrowserComponent implements OnInit {
  filters: SkillFilterFacet[] = [];
  filterCount: number = 0;
  skillSets: SkillSet[];

  private filterFacetUpdated$: Subscription;

  constructor(
	  private skillFiltersService: SkillFiltersService,
		private skillBundleService: SkillBundleService,
		private skillService: SkillService,
		private userService: UserService,
		private skillAssessmentService: SkillAssessmentService,
		private notificationService: NotificationService,
    private dialog: MatDialog) { }

  @Input()
  autoSave: boolean = false;

  @Input()
  user: User;

  @Input()
  skillGroups: SkillGroup[];

  @Input()
  skillSet: SkillSet;
  
  @Output()
  selfAssessmentChange = new EventEmitter<SelfAssessment>();
  
  ngOnInit() {
	  this.filterFacetUpdated$ = this.skillFiltersService.onFilterFacetUpdated.subscribe(f => this.onFilterFacetUpdated());
	  this.filters = this.skillFiltersService.filterFacets;

    if(!this.skillSet){
      this.loadSkillSets()
    } else {
      if(!this.skillGroups){
        // If we haven't been supplied any skills then load them.
        this.loadSkills();
      }
    }
  }

	onSelectUserSkillSetClick(){
    this.dialog.open(DialogManageUserSkillSetsComponent, {
      data: this.user
    }).afterClosed().subscribe(_ => {
      this.loadSkillSets();
    });
  }
  
  onChangeSkillSetClick(){
    this.dialog.open(DialogChangeSkillSetComponent, {
      data: this.skillSets
    }).afterClosed().subscribe(_ => {
      let selectedSkillSet = this.skillSets.find(s => s.selected);
      if(selectedSkillSet.id !== this.skillSet.id) {
        this.skillSet = selectedSkillSet;
        this.skillFiltersService.clearFilterFacets();
        this.loadSkills();
      }
    });
  }

  onRemoveFilterFacetClick(filter: SkillFilterFacet) {
    this.skillFiltersService.removeFilterFacet(filter);
  }

  onRemoveAllFilterFacetsClick() {
    this.skillFiltersService.clearFilterFacets();
  }
  
  private loadSkillSets() {
    this.userService.getSkillSets(this.user)
      .subscribe(skillSets => {
        this.skillSets = skillSets;
        // Can't do anything if we don't have any skill sets.
        if(skillSets.length === 0) return;

        // If we don't have aselected skillset then pick the first one.
        this.skillSet = this.skillSets.find(s => s.selected);
        if(!this.skillSet){
          this.skillSet = this.skillSets[0];
          this.skillSet.selected = true;
        }
        this.loadSkills();
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the skill sets, please try again.
        ${error}`)); 
  }
  
  private loadSkills() {
    this.skillService.getSkillGroups(this.skillSet.id)
      .subscribe(skillGroups => {
        this.skillAssessmentService.setSkillAssessmentsForSkillGroups(this.skillSet.id, skillGroups)
          .subscribe(_ => {
            this.skillGroups = skillGroups
          });
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the skills, please try again.
        ${error}`)); 
  }

  private onFilterFacetUpdated(){
    // A filter has updated.
    this.filters = this.skillFiltersService.filterFacets;
    this.filterCount = this.filters.length;
  }
}