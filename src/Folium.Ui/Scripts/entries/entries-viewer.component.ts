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
import { Component, OnInit, Input, OnDestroy } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { MatDialog } from '@angular/material';

import { Subscription } from "rxjs/Subscription";

import { Entry, EntrySummary, Placement, User } from "./../dtos";
import { EntriesService } from "./entries.service";
import { NotificationService } from "./../common/notification.service";
import { DialogDeleteConfirmComponent } from "./../common/dialog-delete-confirm.component";
import { Observable } from "rxjs/Observable";
import { PlacementsService } from "../placements/placements.service";
import { DialogShareEntryComponent } from "./dialog-share-entry.component";
import { UserService } from "../user/user.service";

@Component({
  selector: "entries-viewer",
  templateUrl: "html/entries/entries-viewer.component.html"
})
export class EntriesViewerComponent implements OnInit, OnDestroy {
  // Used to hang the dummy filters off.
  filters = {};
  entries: EntrySummary[];
  canLoadPages: boolean = false;
  isNewEntryOpen: boolean = false; 

  @Input()
  placement: Placement;
  
  @Input()
  user: User;

  private pageSize: number = 20;
  private page: number = 0;
  private signedInUser$: Subscription;
  private signedInUser: User;
  
  constructor(
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private entriesService: EntriesService,
    private placementsService: PlacementsService,
    private notificationService: NotificationService,
    private dialog: MatDialog) { }

  ngOnInit() {
    this.loadEntries();
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => this.signedInUser = user);
  }

  onNewEntryClick() {
    this.closeActiveEntry();
    this.isNewEntryOpen = true;
  }

  onNewEntryClose(newEntry: Entry) {
		this.isNewEntryOpen = false;
    if(newEntry) {
      let entrySummary = new EntrySummary(newEntry);
      this.upsertEntry(entrySummary);
    }
  }

  loadMoreEntries() {
	  if (!this.canLoadPages) return;
	  this.loadEntries();
  }

  onSelectEntryClick(event: Event, entry: EntrySummary) {
    this.closeActiveEntry();
    entry.viewing = true;
    event.preventDefault();
  }
  
  onViewEntryClose(entry: EntrySummary, updatedEntry: EntrySummary) {
    entry.viewing = false;
    if(updatedEntry) {
      // Share status could have changed.
      this.upsertEntry(updatedEntry);
    }
  }

  onShareEntryClick(entry: EntrySummary) {
		let dialogRef = this.dialog.open(DialogShareEntryComponent, {
  		data: { entryId: entry.id, user: this.user },
		});
		dialogRef.afterClosed().subscribe((result: boolean) => {
			entry.shared = result;
		});
  }
  
  onEditEntryClick(entry: EntrySummary) {
    this.closeActiveEntry();
    entry.editing = true;
  }
  
  onEditEntryClose(entry: EntrySummary, updatedEntry: Entry) {
		entry.editing = false;
    if(updatedEntry) {
      let entrySummary = new EntrySummary(updatedEntry);
      this.upsertEntry(entrySummary);
    }
  }

  onDeleteEntryClick(entry: EntrySummary) {
    let dialogRef = this.dialog.open(DialogDeleteConfirmComponent);
    dialogRef.afterClosed().subscribe(result => {
      if(result === "true") {
        this.deleteEntry(entry);
      }
    });
  }

  // Determines if there is a active-element currently open.
  get hasActiveElement(): boolean {
    return this.isNewEntryOpen || (this.entries && this.entries.find(e => e.editing || e.viewing) != undefined)
  }
  
  canModifyEntry(entry: Entry): boolean {
    return entry.author.id === this.signedInUser.id
  }
  
  get viewingOwnEntries(): boolean {
    return this.signedInUser.id === this.user.id;
  }

  ngOnDestroy() {
	  this.signedInUser$.unsubscribe();
  }

  private upsertEntry(entry: EntrySummary) {
    if(this.entries.findIndex(p => p.id == entry.id) >= 0){
      // Edit.
      this.entries = this.entries.map(e => e.id == entry.id ? entry : e);
    } else {
      // New.
      this.entries = [entry].concat(this.entries);
    }
  }

  private deleteEntry(entry: EntrySummary) {
    this.entriesService.removeEntry(entry)
      .subscribe(() => {
        this.notificationService.addSuccess("Entry deleted");
        this.entries = this.entries.filter((p => p.id !== entry.id)); // remove the placement from the array.
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to delete the entry, please try again.
      ${error}`));
  }

  private closeActiveEntry() {
    // Reset all entries to not viewing or editing, which causes any active cards to close.
    if(!this.entries) return;
    this.entries.forEach(entry => {
      entry.viewing = false;
      entry.editing = false;
    })
  }

  private loadEntries() {
    this.page++;
    let entrie$: Observable<EntrySummary[]>;
    if(this.placement) {
      entrie$ = this.placementsService.getEntries(this.placement, this.page, this.pageSize);
    } else {
      entrie$ = this.entriesService.getEntries(this.user.id, this.page, this.pageSize);
    }
	  entrie$
      .subscribe((entries: EntrySummary[]) => {
        if(!this.entries) {
          this.entries = [];
        }
        this.entries = this.entries.concat(entries);
        this.canLoadPages = entries.length === this.pageSize;
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the entries, please try again.
      ${error}`));
  }
}