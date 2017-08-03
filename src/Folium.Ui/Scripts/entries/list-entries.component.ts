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
import { Component, OnInit, Input } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { MdDialog } from '@angular/material';

import { SkillSet, Entry, EntrySummary, Placement } from "./../dtos";
import { EntriesService } from "./entries.service";
import { NotificationService } from "./../common/notification.service";
import { DialogDeleteConfirmComponent } from "./../common/dialog-delete-confirm.component";
import { Observable } from "rxjs/Observable";
import { PlacementsService } from "../placements/placements.service";

@Component({
  selector: "list-entries",
  templateUrl: "html/entries/list.component.html"
})
export class ListEntriesComponent implements OnInit {
  // Used to hang the dummy filters off.
  filters = {};
  entries: EntrySummary[];
  canLoadPages: boolean = false;
  isNewEntryOpen: boolean = false; 

  @Input()
  placement: Placement;

  private skillSet: SkillSet;
  private pageSize: number = 20;
  private page: number = 0;
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private entriesService: EntriesService,
    private placementsService: PlacementsService,
	  private notificationService: NotificationService,
    private dialog: MdDialog) { }

  ngOnInit() {
    this.route.data.forEach((data: { skillSet: SkillSet }) => {
		  this.skillSet = data.skillSet;
		  this.loadEntries();
    });
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
  
  onViewEntryClose(entry: EntrySummary) {
		entry.viewing = false;
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
  hasActiveElement() {
    return this.isNewEntryOpen || (this.entries && this.entries.find(e => e.editing || e.viewing) != undefined)
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
    this.entriesService.removeEntry(entry.id)
      .subscribe(() => {
        this.notificationService.addSuccess("Entry deleted");
        this.entries = this.entries.filter((p => p.id !== entry.id)); // remove the placement from the array.
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to delete the entry, please try again.
      ${error}`));
  }

  private closeActiveEntry() {
    // Reset all entries to not viewing or editing, which causes any active cards to close.
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
      entrie$ = this.entriesService.getEntries(this.skillSet.id, this.page, this.pageSize);
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