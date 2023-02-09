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
import { MatDialog } from '@angular/material/dialog';

import { Subscription, Observable } from "rxjs";

import { Entry, EntrySummary, Placement, User } from "../../core/dtos";
import { EntriesService } from "../entries.service";
import { NotificationService } from "../../core/notification.service";
import { DialogDeleteConfirmComponent } from "../../core/components/dialog-delete-confirm.component";
import { PlacementsService } from "../../placements/placements.service";
import { DialogShareEntryComponent } from "./dialog-share-entry.component";
import { UserService } from "../../user/user.service";
import { DialogRequestSignOffComponent } from "./dialog-request-sign-off.component";
import { DialogSignOffComponent } from "./dialog-sign-off.component";
import { NoFilter, Filter, PendingSignOffFilter, SignedOffFilter, SharedFilter, NotSharedFilter } from "../entry-filters";


@Component({
  selector: "entries-viewer",
  templateUrl: "entries-viewer.component.html"
})
export class EntriesViewerComponent implements OnInit, OnDestroy {
  entries: EntrySummary[];
  canLoadPages: boolean = false;
  isNewEntryOpen: boolean = false;
  noFilter = new NoFilter; // We have to set this here so the instance can be used as the default filter and matched on the MatSelect component.
  activeFilter: Filter = this.noFilter;
  sharedEntryFilters: Filter[] = [
    this.noFilter,
    new PendingSignOffFilter,
    new SignedOffFilter,
  ];
  myEntryFilters: Filter[] = [
    this.noFilter,
    new SharedFilter,
    new NotSharedFilter,
    new PendingSignOffFilter,
    new SignedOffFilter,
  ];

  @Input()
  placement: Placement;
  
  @Input()
  user: User;

  @Input()
  sharedEntriesOnly: boolean;

  private pageSize: number = 20;
  private page: number = 1;
  private signedInUser$: Subscription;
  private signedInUser: User;
  
  constructor(
    private userService: UserService,
    private entriesService: EntriesService,
    private placementsService: PlacementsService,
    private notificationService: NotificationService,
    private dialog: MatDialog) { }

  ngOnInit() {
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => {
      this.signedInUser = user;
      this.loadEntries(this.page);
    });
  }

  filterChange() {
    this.page = 1; // Reset the page.
    this.loadEntries(this.page);
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
    this.page++;
    this.loadEntries(this.page);
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
      // Use a timeout for this as it can cause Angular change detection errors when used with the active-element.
      window.setTimeout(() => {
        this.upsertEntry(updatedEntry);
      }, 0);
    }
  }

  onShareEntryClick(entry: EntrySummary) {
		let dialogRef = this.dialog.open(DialogShareEntryComponent, {
  		data: { entry: entry, user: this.user },
		});
    dialogRef.afterClosed().subscribe((result: boolean) => {
      // Share status could have changed.
      if (entry.shared !== result) {
        entry.shared = result;
        this.upsertEntry(entry);
      }
		});
  }
  
  onEditEntryClick(entry: EntrySummary) {
    this.closeActiveEntry();
    entry.editing = true;
  }
  
  onEditEntryClose(entry: EntrySummary, updatedEntry: Entry) {
		entry.editing = false;
    if (updatedEntry) {
      // Use a timeout for this as it can cause Angular change detection errors when used with the active-element.
      window.setTimeout(() => {
        let entrySummary = new EntrySummary(updatedEntry);
        this.upsertEntry(entrySummary);
      }, 0);
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

  // Determines if there is a active filter
  get hasActiveFilter(): boolean {
    return this.activeFilter.filter ? true : false;
  }

  requestSignOff(entry: EntrySummary) {
    let dialogRef = this.dialog.open(DialogRequestSignOffComponent, {
      data: { entry: entry, user: this.user },
    });
    dialogRef.afterClosed().subscribe((result: boolean) => {
      // Status could have changed.
      if (entry.signOffRequested !== result) {
        entry.signOffRequested = result;
        if (result) {
          // If there is a signoff request then it must be shared.
          entry.shared = result;
        }
        this.upsertEntry(entry);
      }
    });
  }

  signOff(entry: EntrySummary) {
    let dialogRef = this.dialog.open(DialogSignOffComponent, {
      data: { entry: entry },
    });
    dialogRef.afterClosed().subscribe((result: boolean) => {
      // Status could have changed.
      if (entry.signedOff !== result) {
        entry.signedOff = result;
        this.upsertEntry(entry);
      }
    });
  }
  canSignOffEntry(entry: EntrySummary): boolean {
    return entry.signOffRequested && !entry.signedOff && entry.isAuthorisedToSignOff;
  }
  canRequestSignOff(entry: EntrySummary): boolean {
    return this.canModifyEntry(entry) && entry.isSignOffCompatible && !entry.signedOff
  }
  isMyEntry(entry: EntrySummary): boolean {
    return entry.author.id === this.signedInUser.id;
  }
  canModifyEntry(entry: EntrySummary): boolean {
    return entry.author.id === this.signedInUser.id && !entry.signedOff;
  }
  get viewingOwnEntries(): boolean {
    return this.signedInUser.id === this.user.id;
  }

  ngOnDestroy() {
	  this.signedInUser$.unsubscribe();
  }

  private upsertEntry(entry: EntrySummary) {
    // Check the entry is valid for the current filter.
    var isValidEntryForFilter = this.activeFilter.isValidEntryForFilter(entry);

    if(this.entries.findIndex(p => p.id == entry.id) >= 0){
      // Edit.
      if (isValidEntryForFilter) {
        this.entries = this.entries.map(e => e.id == entry.id ? entry : e); // update the entry.
      } else {
        this.entries = this.entries.filter((p => p.id !== entry.id)); // remove the entry from the array.
      }
    } else {
      // New.
      if (isValidEntryForFilter) {
        this.entries = [entry].concat(this.entries); // add the entry.
      }
    }
  }

  private deleteEntry(entry: EntrySummary) {
    this.entriesService.removeEntry(entry)
      .subscribe(() => {
        this.notificationService.addSuccess("Entry deleted");
        this.entries = this.entries.filter((p => p.id !== entry.id)); // remove the entry from the array.
        this.closeActiveEntry();
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to delete the entry, please try again.
      ${error}`));
  }

  private closeActiveEntry() {
    // Reset all entries to not viewing or editing, which causes any active cards to close.
    this.isNewEntryOpen = false;
    if(!this.entries) return;
    this.entries.forEach(entry => {
      entry.viewing = false;
      entry.editing = false;
    })
  }

  private loadEntries(page: number) {
    let entrie$: Observable<EntrySummary[]>;
    if(this.placement) {
      entrie$ = this.placementsService.getPlacementEntries(this.placement, page, this.pageSize);
    } else {
      const filter = this.activeFilter.filter;
      // Are we viewing our own or someone elses entries?
      if (this.viewingOwnEntries) {
        // We either get our own entries or ones shared with us.
        entrie$ = this.sharedEntriesOnly ? this.entriesService.getEntriesSharedWithMe(page, this.pageSize, filter) : this.entriesService.getMyEntries(page, this.pageSize, filter);
      } else {
        entrie$ = this.entriesService.getEntriesSharedWithMeBy(this.user.id, page, this.pageSize, filter);
      }
    }
	  entrie$
      .subscribe((entries: EntrySummary[]) => {
        this.entries = page == 1 ? entries : this.entries.concat(entries);
        this.canLoadPages = entries.length === this.pageSize;
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the entries, please try again.
      ${error}`));
  }
}
