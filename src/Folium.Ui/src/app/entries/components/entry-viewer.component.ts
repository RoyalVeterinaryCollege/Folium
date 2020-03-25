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
import { MatDialog } from '@angular/material/dialog';

import { Subscription } from "rxjs";

import { Entry, SkillGroup, EntrySummary, User, EntryFile } from "../../core/dtos";
import { EntriesService } from "../entries.service";
import { SkillService } from "../../skills/skill.service";
import { NotificationService } from "../../core/notification.service"
import { DialogDeleteConfirmComponent } from "../../core/components/dialog-delete-confirm.component";
import { SkillAssessmentService } from "../../skills/skill-assessment.service";
import { SkillBundleService } from "../../skills/skill-bundle.service";
import { DialogShareEntryComponent } from "./dialog-share-entry.component";
import { UserService } from "../../user/user.service";
import { DialogSignOffComponent } from "./dialog-sign-off.component";
import { DialogRequestSignOffComponent } from "./dialog-request-sign-off.component";

@Component({
	selector: "entry-viewer",
  templateUrl: "entry-viewer.component.html",
  providers: [SkillBundleService] // Use a new instance of the skills bundle.
})
export class EntryViewerComponent implements OnInit, OnDestroy {
  skillGroups: SkillGroup[];
	bundleSize: number;
  entry: Entry;
  entryFiles: EntryFile[];
  commentFiles: EntryFile[];
	
  private signedInUser$: Subscription;
  private signedInUser: User;
	
  constructor(
    private userService: UserService,
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
        this.loadEntryFiles();
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry, please try again.
				${error}`));  
  }

	loadSkills() {
		// First load the skill groupings.
		this.skillService.getSkillGroupings(this.entry.skillSetId)
			.subscribe(skillGroupings => {
				// Do we have a skill grouping for this entry?
				var skillGroupingId = this.entry.skillGroupingId ? this.entry.skillGroupingId : this.skillService.getDefaultSkillGrouping(skillGroupings).id;
				this.skillService.getSkillGroups(this.entry.skillSetId, skillGroupingId)
					.subscribe(skillGroups => {
						this.skillAssessmentService.setUserSkillAssessmentsForSkillGroups(this.user.id, this.entry.skillSetId, skillGroups)
							.subscribe(_ => {
								this.skillBundleService.setBundleItems(skillGroups, this.entry.assessmentBundle)
								this.bundleSize = Object.keys(this.entry.assessmentBundle).length;
								this.skillGroups = skillGroups;
							},
							(error: any) => this.notificationService.addDanger(`There was an error trying to set your skill levels, please try again.
								${error}`));
					},
					(error: any) => this.notificationService.addDanger(`There was an error trying to load the skills, please try again.
						${error}`)); 
				},
				(error: any) => this.notificationService.addDanger(`There was an error trying to load the skill groupings, please try again.
					${error}`));		
	}

  private loadEntryFiles() {
    this.entriesService.getEntryFiles(this.entry.id)
      .subscribe(entryFiles => {
        this.entryFiles = entryFiles;
      },
        (error: any) => this.notificationService.addDanger(`There was an error trying to load the entry files, please try again.
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
  		data: { entry: this.entry, user: this.user },
		});
		dialogRef.afterClosed().subscribe((result: boolean) => {
			this.entry.shared = result;
			this.entrySummary.shared = result;
		});
	}

  requestSignOff() {
    let dialogRef = this.dialog.open(DialogRequestSignOffComponent, {
      data: { entry: this.entry, user: this.user },
    });
    dialogRef.afterClosed().subscribe((result: boolean) => {
      this.entry.signOffRequested = result;
      this.entrySummary.signOffRequested = result;
      if (result) {
        // If there is a signoff request then it must be shared.
        this.entry.shared = result;
        this.entrySummary.shared = result;
      }
    });
  }

  signOff() {
    let dialogRef = this.dialog.open(DialogSignOffComponent, {
      data: { entry: this.entry, files: this.entryFiles },
    });
    dialogRef.afterClosed().subscribe((result: boolean) => {
      this.entry.signedOff = result;
      this.entrySummary.signedOff = result;
    });
  }

  get canSignOffEntry(): boolean {
    return this.entry.signOffRequested && !this.entry.signedOff && this.entry.isAuthorisedToSignOff;
  }

  get canModifyEntry(): boolean {
    return this.entry.author.id === this.signedInUser.id && !this.entry.signedOff;
  }

  get isMyEntry(): boolean {
    return this.entry.author.id === this.signedInUser.id;
  }
	
  ngOnDestroy() {
		this.onClose.emit(this.entrySummary);
	  this.signedInUser$.unsubscribe();
  }
}
