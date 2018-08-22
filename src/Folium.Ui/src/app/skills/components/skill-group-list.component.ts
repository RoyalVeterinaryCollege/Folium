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
          Input,
          OnInit,
          OnDestroy,
          OnChanges,
          SimpleChanges,
          SimpleChange
        } from "@angular/core";

import { Subscription } from "rxjs";

import {  SkillGroup,
          SkillFilterFacet,
          User
        } from "../../core/dtos";
import { SkillService } from "../skill.service";
import { SkillFiltersService } from "../skill-filters.service";
import { NotificationService } from "../../core/notification.service"
import { SkillBundleService } from "../skill-bundle.service";
import { SkillAssessmentService } from "../skill-assessment.service";

@Component({
  templateUrl: "skill-group-list.component.html",
  selector: "skill-group-list",
  //changeDetection: ChangeDetectionStrategy.OnPush
})
export class SkillGroupListComponent implements OnInit, OnDestroy, OnChanges {

  filters: SkillFilterFacet[] = [];
  searchTerms: string[] = [];

  private filterFacetUpdated$: Subscription;
  private searchTermsChanged$: Subscription;
  private bundleFilterUpdated$: Subscription;

  constructor(
    private skillService: SkillService,
    private skillAssessmentService: SkillAssessmentService,
    private skillFiltersService: SkillFiltersService,
	  //private changeDetectorRef: ChangeDetectorRef,
	  private notificationService: NotificationService,
    private skillBundleService: SkillBundleService) { }

  @Input()
  autoSave: boolean = false;

  @Input()
  user: User;

  @Input()
  readOnly: boolean = false;
  
  @Input()
  bundleView: boolean = false;

  @Input()
  skillSetId: number;
  
  @Input()
  skillGroups: SkillGroup[];

  ngOnInit() {
    this.filterFacetUpdated$ = this.skillFiltersService.onFilterFacetUpdated.subscribe(f => this.onFilterFacetUpdated());
    this.searchTermsChanged$ = this.skillFiltersService.onSearchTermsChanged.subscribe(s => this.onSearchTermsChanged(s));
    this.bundleFilterUpdated$ = this.skillBundleService.onBundleChange.subscribe(c => {
      if(this.readOnly || this.bundleView) {
        this.applyAssessmentBundleFilter(); // re-apply the bundle filter when the bundle changes,
      }
    });

    this.filters = this.skillFiltersService.filterFacets;
    this.searchTerms = this.skillFiltersService.searchTerms;

    if (this.bundleView) {
      this.applyAssessmentBundleFilter();
    }
    if(this.bundleView && this.skillBundleService.bundleSize < 20) {
      this.expandAllSkillGroups();
    } else {
      this.collapseAllSkillGroups();
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    // The skillset id can change, update the self assessment scales when it does.
    const skillSetId: SimpleChange = changes.skillSetId;
    if (skillSetId && (skillSetId.previousValue !== skillSetId.currentValue)) {
      //this.changeDetectorRef.markForCheck(); // TODO: Check if we need todo this!
    }
  }

  getSkillTotalFromGroup(skillGroup: SkillGroup) {
    if (!skillGroup) return -1;
    let total = skillGroup.skills.filter(s => !s.assessment.hidden).length;
    skillGroup.childGroups.forEach(group => {
      total += this.getSkillTotalFromGroup(group);
    });
    return total;
  }

  getSkillTotalFromGroups() {
    // Sum up all totals from the groups.
    return this.skillGroups
      ? this.skillGroups.map(g => this.getSkillTotalFromGroup(g)).reduce((x, y) => x + y, 0)
      : -1;
  }
  
  getAverageSelfAssessmentFromGroup(skillGroup: SkillGroup) {
    return this.skillAssessmentService.getAverageSelfAssessmentFromGroup(skillGroup);
  }

  onToggleSkillGroup(event: Event, skillGroup: SkillGroup) {
    this.toggleSkillGroup(skillGroup, !skillGroup.expanded);
	  event.stopPropagation();
    //this.changeDetectorRef.markForCheck();
  }
  
  onExpandSkillGroup(event: Event, skillGroup: SkillGroup) {
    this.toggleSkillGroup(skillGroup, true);
	  event.stopPropagation();
    //this.changeDetectorRef.markForCheck();
  }

  trackSkillGroup(index, skillGroup: SkillGroup) {
    return skillGroup.id;
  }

  ngOnDestroy() {
    this.filterFacetUpdated$.unsubscribe();
    this.searchTermsChanged$.unsubscribe();
    this.bundleFilterUpdated$.unsubscribe();
  }

  private collapseAllSkillGroups() {
    if (!this.skillGroups) return;
    this.skillGroups.forEach(skillGroup => this.toggleSkillGroup(skillGroup, false));
    //this.changeDetectorRef.markForCheck();
  }

  private expandAllSkillGroups() {
    if (!this.skillGroups) return;
    this.skillGroups.forEach(skillGroup => this.toggleSkillGroup(skillGroup, true));
    //this.changeDetectorRef.markForCheck();
  }

  private toggleSkillGroup(skillGroup: SkillGroup, expanded: boolean) {
    if (!skillGroup) return;
    skillGroup.expanded = expanded;
  }

  private onFilterFacetUpdated() {
    // A filter has updated.
    this.filters = this.skillFiltersService.filterFacets;

    this.collapseAllSkillGroups();
  }

  private onSearchTermsChanged(searchTerms: string[]) {
    // A search term has updated.
    this.searchTerms = this.skillFiltersService.searchTerms;

    this.collapseAllSkillGroups();
    //this.changeDetectorRef.markForCheck();
  }

  private applyAssessmentBundleFilter() {
    // Generate the bundle filter and apply it to the current filters.
    let bundleFilter: SkillFilterFacet = {
      id: -1,
      skillFilterId: -1,
      name: "bundled items",
      matchedSkillIds: this.skillBundleService.skillIds,
      selected: true
    };
    this.filters = [bundleFilter];
  }
}