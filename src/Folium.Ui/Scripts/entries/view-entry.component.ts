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
import { Component, OnInit, OnDestroy, Input, EventEmitter, Output } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { MatDialog } from '@angular/material';

import { Subscription } from "rxjs/Subscription";

import { Entry, SkillGroup, SelfAssessmentScale, EntrySummary, User } from "./../dtos";
import { EntriesService } from "./entries.service";
import { SkillService } from "../skills/skill.service";
import { NotificationService } from "../common/notification.service"
import { DialogDeleteConfirmComponent } from "./../common/dialog-delete-confirm.component";
import { SkillAssessmentService } from "../skills/skill-assessment.service";
import { SkillBundleService } from "../skills/skill-bundle.service";

@Component({
  templateUrl: "html/entries/view-entry.component.html"
})
export class ViewEntryComponent implements OnInit, OnDestroy {
	entry: EntrySummary;
	user: User;
	loaded = false;
	
	private paramsSubscription$: Subscription;
	
  constructor(
		private router: Router,
		private route: ActivatedRoute,
		private entriesService: EntriesService,
		private skillService: SkillService,
		private skillAssessmentService: SkillAssessmentService,
		private skillBundleService: SkillBundleService,
		private notificationService: NotificationService,
    private dialog: MatDialog) { }

  ngOnInit() {
    this.route.data.forEach((data: { currentUser: User }) => {
      this.user = data.currentUser;
		});
		this.paramsSubscription$ = this.route.paramMap.subscribe(params => {
			// Load the entry.
			this.loadEntry(params.get('id'));
		});
	}

  onDeleteEntry() {
		// Navigate to users entries.		
	  this.router.navigate(['/entries']);
  }

  loadEntry(id: string) {
		this.entriesService.getEntrySummary(id)
			.subscribe((entry: EntrySummary) => {
				this.entry = entry;
				this.loaded = true;
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry, please try again.
				${error}`));  
  }

	onEditEntry() {
		this.entry.editing = true;
	}

	onEditEntryClose() {		
		this.entry.editing = false;
	}
	
  ngOnDestroy() {
		this.paramsSubscription$.unsubscribe();
	}
}