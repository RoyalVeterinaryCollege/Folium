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

import { Entry, SkillGroup, SelfAssessmentScale, EntrySummary, User, SkillSet } from "./../dtos";
import { EntriesService } from "./entries.service";
import { SkillService } from "../skills/skill.service";
import { NotificationService } from "../common/notification.service"
import { DialogDeleteConfirmComponent } from "./../common/dialog-delete-confirm.component";
import { SkillAssessmentService } from "../skills/skill-assessment.service";
import { SkillBundleService } from "../skills/skill-bundle.service";
import { DialogShareEntryComponent } from "./dialog-share-entry.component";
import { UserService } from "../user/user.service";

@Component({
	selector: "entry-viewer",
  templateUrl: "html/entries/entry-viewer.component.html",
  providers: [SkillBundleService] // Use a new instance of the skills bundle.
})
export class EntryViewerComponent implements OnInit, OnDestroy {
  skillGroups: SkillGroup[];
	bundleSize: number;
	entry: Entry;
	
  private signedInUser$: Subscription;
  private signedInUser: User;
	
  constructor(
    private userService: UserService,
		private router: Router,
		private route: ActivatedRoute,
		private entriesService: EntriesService,
		private skillService: SkillService,
		private skillAssessmentService: SkillAssessmentService,
		private skillBundleService: SkillBundleService,
		private notificationService: NotificationService,
    private dialog: MatDialog) { }

	@Input()
	entrySummary: EntrySummary;
	
	@Input()
	user: User;

	@Input()
	hideClose: boolean = false;
	
	@Output() 
	onEditEntry = new EventEmitter<EntrySummary>();

	@Output()
	onRemoveEntry = new EventEmitter<EntrySummary>();

	@Output() 
	onClose = new EventEmitter<EntrySummary>();

  ngOnInit() {
		this.loadEntry();
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => this.signedInUser = user);
	}
	
  editEntry() {
		this.onEditEntry.emit(this.entrySummary);
  }

  removeEntry() {
    let dialogRef = this.dialog.open(DialogDeleteConfirmComponent);
    dialogRef.afterClosed().subscribe(result => {
      if(result === "true") {
				this.onRemoveEntry.emit(this.entrySummary);
			}
		});
  }

  loadEntry() {
		this.entriesService.getEntry(this.entrySummary.id)
			.subscribe((entry: Entry) => {
				this.entry = entry;
				this.loadSkills();
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry, please try again.
				${error}`));  
  }

	loadSkills() {
		this.skillBundleService.setBundleItems(this.entry.assessmentBundle);
		this.skillService.getSkillGroups(this.entry.skillSetId)
			.subscribe(skillGroups => {
				this.skillAssessmentService.setSkillAssessmentsForSkillGroups(this.user.id, this.entry.skillSetId, skillGroups, this.entry.assessmentBundle, true /* readOnlyBundle */)
					.subscribe(_ => {
						this.skillGroups = skillGroups;
						this.bundleSize = Object.keys(this.entry.assessmentBundle).length;
					});
		},
		(error: any) => this.notificationService.addDanger(`There was an error trying to load the skills, please try again.
			${error}`)); 
	}
	
	onCloseClick(event: Event) {		
		event.preventDefault();
		this.onClose.emit(this.entrySummary);
	}

	printEntry() {
		window.print();
	}

	shareEntry() {
		let dialogRef = this.dialog.open(DialogShareEntryComponent, {
  		data: { entryId: this.entry.id, user: this.user },
		});
		dialogRef.afterClosed().subscribe((result: boolean) => {
			this.entry.shared = result;
			this.entrySummary.shared = result;
		});
	}
	
	get canModifyEntry(): boolean {
		return this.entry.author.id === this.signedInUser.id
	}
	
  ngOnDestroy() {
		this.onClose.emit(this.entrySummary);
	  this.signedInUser$.unsubscribe();
  }
}