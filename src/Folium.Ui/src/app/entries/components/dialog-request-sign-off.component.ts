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
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Component, OnInit, ViewChild, ElementRef, Inject, OnDestroy } from "@angular/core"
import { MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialog } from '@angular/material/dialog';

import { Observable, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

import { UserOption, User, EntrySignOffRequest, Entry, EntrySignOffGroup, EntrySummary } from "../../core/dtos";
import { UserService } from "../../user/user.service";
import { EntriesService } from "../entries.service";
import { NotificationService } from "../../core/notification.service";
import { DialogConfirmComponent } from '../../core/components/dialog-confirm.component';

@Component({
	selector: "dialog-request-sign-off",
	templateUrl: "dialog-request-sign-off.component.html"
})
export class DialogRequestSignOffComponent implements OnInit, OnDestroy {
	previousUserSearchOptions: UserOption[] = []; // Store the previous options when the user has no search content as if they type the same letter again the distinctUntilChanged option on the observable means it will not fire.
	userOptions: UserOption[] = [];
	signOffUsersToRequest: User[] = [];
	signOffUsers: User[] = [];
	searchRequest$ = new Subject<string>();
	message: string;	
	usersToRequestQuery: string = "";
	
	@ViewChild("userToRequestSignOffInput", { static: false })
  usersToRequestInput: ElementRef;
	
  private cachedQuery: string;
  private entry: Entry | EntrySummary;
	private user: User;
  private allTutors: User[] = [];

	constructor(
		private userService: UserService,
		private entriesService: EntriesService,
		private sanitizer: DomSanitizer,
		private notificationService: NotificationService,
		@Inject(MAT_DIALOG_DATA) private data: any,
    private dialogRef: MatDialogRef<DialogRequestSignOffComponent>,
    private dialog: MatDialog) { 
			this.user = data.user;
			this.entry = data.entry;
			this.search(this.searchRequest$.asObservable())
				.subscribe(results => {
					this.previousUserSearchOptions = [...results];
					if(this.usersToRequestQuery.trim().length > 0) {
						this.userOptions = results;
					} else {
						this.userOptions = [];
					}
				});
	}

	ngOnInit(): void {
		this.user.courses.forEach(course => {
			this.userService.getUsersTutors(this.user.id, course.courseId)
				.subscribe(tutors => {
					this.allTutors.push(... tutors);
				});
		});
		this.loadSignOffUsers();
	}	

	highlightMatch(name: string): SafeHtml {
		let replaced = name.replace(new RegExp(`(${this.usersToRequestQuery})`,"ig"), "<strong>$1</strong>")
		return this.sanitizer.bypassSecurityTrustHtml(replaced);
	}
	
	onUserSelected(event: MatAutocompleteSelectedEvent) {		
		let selectedUser = event.option.value as UserOption;
    if (selectedUser.isGroup) {
      selectedUser.group.forEach(user => {				
        if (this.canAddUserToRequestList(user)) {
					this.signOffUsersToRequest.push(user);
				}
			});
    } else {
      if (this.canAddUserToRequestList(selectedUser.user)) {
        this.signOffUsersToRequest.push(selectedUser.user);
			}
		}
		this.userOptions = [];
		this.usersToRequestQuery = "";
    // The above didn't seem to work, so having to do this.
    this.usersToRequestInput.nativeElement.value = "";
	}
	
	onUserInput() {
		if(this.cachedQuery && this.previousUserSearchOptions && this.cachedQuery.toUpperCase() === this.usersToRequestQuery.toUpperCase()) {
			this.userOptions = [...this.previousUserSearchOptions];
			return;
		}
		if (this.usersToRequestQuery.trim().length > 0) {
			this.searchRequest$.next(this.usersToRequestQuery);
		} else {
			this.userOptions = [];
		}
	}

	getUserLabel(user: User): string {
    return user.firstName ? `${user.firstName} ${user.lastName}` : user.email;
	}
	
  onRemoveUserClick(user: User) {
    this.entriesService.removeSignOffUser(this.entry.id, user.id)
			.subscribe((response: any) => {
        this.signOffUsers = this.signOffUsers.filter(c => c.id !== user.id);
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to remove the user, please try again.
				${error}`));
	}
	
	onTutorClick(tutor: User) {
		if(this.canAddUserToRequestList(tutor)) {
			this.signOffUsersToRequest.push(tutor);
		}
	}
	
  onRemoveUserToRequestClick(user: User) {
    if (this.signOffUsersToRequest.some(u => u.id == user.id)) {
      this.signOffUsersToRequest.splice(this.signOffUsersToRequest.indexOf(user), 1);
		}
	}

  onMakeRequest() {
    if (this.signOffUsers.length == 0) {
      // New request to sign-off, check they are happy.
      let dialogRef = this.dialog.open(DialogConfirmComponent, {
        data: {
          body: `${this.entry.entryType.template.signOff.text}
              <p>
                Just so you know:
                <ul>
                  <li>This entry will automatically be shared with any users you request to sign-off.</li>
                  <li>Sign-off only has to be completed by one person.</li>
                  <li>Once the Entry is signed off it will become read-only and only comments will be allowed.</li>
                </ul>
              </p>
              <p>Are you sure you want to request 'Sign-off' for this Entry?</p>`
        },
      });
      dialogRef.afterClosed().subscribe(result => {
        if (result === "true") {
          this.requestSignOff();
        }
      });
    } else {
      // Just adding users.
      this.requestSignOff();
    }
  }

	requestSignOff() {
    let signOffRequest = new EntrySignOffRequest();
    signOffRequest.entryId = this.entry.id;
    signOffRequest.message = this.message;
    signOffRequest.authorisedUserIds = this.signOffUsersToRequest.map(c => c.id);

    this.entriesService.requestEntrySignOff(signOffRequest)
			.subscribe((response: any) => {
				this.notificationService.addSuccess(`Sign-off of this entry was successfully requested.`);				
				this.signOffUsers = this.signOffUsers.concat(this.signOffUsersToRequest);
        this.dialogRef.close(true /* sign-off requested */);
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to request the sign-off, please try again.
				${error}`));
	}
	
	onCancelRequest() {
		this.signOffUsersToRequest = [];
		this.message = "";		
	}

  canAnyoneSignOff() {
    return this.entry.entryType.template && this.entry.entryType.template.signOff.allowedBy == EntrySignOffGroup.Anyone;
  }

	get tutors(): User[] {
    return this.allTutors.filter(user => this.canAddUserToRequestList(user) ? user : null);
	}

	ngOnDestroy(): void {
		this.dialogRef.close(this.signOffUsers.length > 0 /* sign-off requested */);
	}

	private search(terms: Observable<string>) {		
		return terms.pipe(
			debounceTime(400),
			distinctUntilChanged(),
			switchMap(term => {
				this.cachedQuery = term;
				return this.userService.getUsers(term)
			})
		);;
	}

  private loadSignOffUsers() {
    this.entriesService.getSignOffUsers(this.entry.id)
			.subscribe((users: User[]) => {
				this.signOffUsers = users;
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the existing users who can sign-off this entry, please try again.
				${error}`));
	}

	private canAddUserToRequestList(user: User): boolean {
		return !this.signOffUsersToRequest.find(u => u.email === user.email)
			&& !this.signOffUsers.find(u => u.email === user.email)
			&& user.email !== this.user.email
	}
}
