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
import { MatOptionSelectionChange, MAT_DIALOG_DATA, MatDialogRef, MatAutocompleteSelectedEvent, MatInput } from '@angular/material';

import { Observable } from 'rxjs/Observable';
import { Subject } from "rxjs/Subject";
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/debounceTime';
import 'rxjs/add/operator/distinctUntilChanged';
import 'rxjs/add/operator/switchMap';

import { CollaboratorOption, User, ShareEntryDto } from "./../dtos";
import { UserService } from "../user/user.service";
import { EntriesService } from "./entries.service";
import { NotificationService } from "../common/notification.service";

@Component({
	selector: "dialog-share-entry",
	templateUrl: "html/entries/dialog-share-entry.html"
})
export class DialogShareEntryComponent implements OnInit, OnDestroy {
	previousCollaboratorOptions: CollaboratorOption[] = []; // Store the previous options when the user has no search content as if they type the same letter again the distinctUntilChanged option on the observable means it will not fire.
	collaboratorOptions: CollaboratorOption[] = [];
	collaboratorsToInvite: User[] = [];
	collaborators: User[] = [];
	searchRequest$ = new Subject<string>();
	message: string;	
	collaboratorToInviteQuery: string = "";
	
	@ViewChild("collaboratorToInviteInput")
	collaboratorToInviteInput: ElementRef;
	
	private cachedQuery: string;
	private entryId: string;
	private user: User;
	private allTutors: User[] = [];

	constructor(
		private userService: UserService,
		private entriesService: EntriesService,
		private sanitizer: DomSanitizer,
		private notificationService: NotificationService,
		@Inject(MAT_DIALOG_DATA) private data: any,
		private dialogRef: MatDialogRef<DialogShareEntryComponent>) { 
			this.user = data.user;
			this.entryId = data.entryId;
			this.search(this.searchRequest$.asObservable())
				.subscribe(results => {
					this.previousCollaboratorOptions = [...results];
					if(this.collaboratorToInviteQuery.trim().length > 0) {
						this.collaboratorOptions = results;
					} else {
						this.collaboratorOptions = [];
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
		this.loadCollaborators();
	}	

	highlightMatch(name: string): SafeHtml {
		let replaced = name.replace(new RegExp(`(${this.collaboratorToInviteQuery})`,"ig"), "<strong>$1</strong>")
		return this.sanitizer.bypassSecurityTrustHtml(replaced);
	}
	
	onCollaboratorSelected(event: MatAutocompleteSelectedEvent) {		
		let selectedCollaborator = event.option.value as CollaboratorOption;
		if(selectedCollaborator.isGroup) {
			selectedCollaborator.group.forEach(user => {				
				if(this.canAddUserToInviteList(user)) {
					this.collaboratorsToInvite.push(user);
				}
			});
		} else {
			if(this.canAddUserToInviteList(selectedCollaborator.user)) {
				this.collaboratorsToInvite.push(selectedCollaborator.user);
			}
		}
		this.collaboratorOptions = [];
		this.collaboratorToInviteQuery = "";
		// The above didn't seem to work, so having to do this.
		this.collaboratorToInviteInput.nativeElement.value = "";
	}
	
	onCollaboratorInput() {
		if(this.cachedQuery && this.previousCollaboratorOptions && this.cachedQuery.toUpperCase() === this.collaboratorToInviteQuery.toUpperCase()) {
			this.collaboratorOptions = [...this.previousCollaboratorOptions];
			return;
		}
		if (this.collaboratorToInviteQuery.trim().length > 0) {
			this.searchRequest$.next(this.collaboratorToInviteQuery);
		} else {
			this.collaboratorOptions = [];
		}
	}

	getCollaboratorLabel(collaborator: User): string {
		return collaborator.firstName ? `${collaborator.firstName} ${collaborator.lastName}` : collaborator.email;
	}
	
	onRemoveCollaboratorClick(collaborator: User) {
		this.entriesService.removeCollaborator(this.entryId, collaborator.id)
			.subscribe((response: any) => {
				this.collaborators = this.collaborators.filter(c => c.id !== collaborator.id);
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to remove the collaborator, please try again.
				${error}`));
	}
	
	onTutorClick(tutor: User) {
		if(this.canAddUserToInviteList(tutor)) {
			this.collaboratorsToInvite.push(tutor);
		}
	}
	
	onRemoveCollaboratorToInviteClick(collaborator: User) {
		if(this.collaboratorsToInvite.includes(collaborator)){
			this.collaboratorsToInvite.splice(this.collaboratorsToInvite.indexOf(collaborator), 1);
		}
	}
	
	onSendInvites() {
		let shareEntryDto = new ShareEntryDto();
		shareEntryDto.entryId = this.entryId;
		shareEntryDto.message = this.message;
		shareEntryDto.collaboratorIds = this.collaboratorsToInvite.map(c => c.id);

		this.entriesService.shareEntry(shareEntryDto)
			.subscribe((response: any) => {
				this.notificationService.addSuccess(`The entry was successfully shared.`);				
				this.collaborators = this.collaborators.concat(this.collaboratorsToInvite);
				this.dialogRef.close(true /* entry is shared */);
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to share with the collaborators, please try again.
				${error}`));
	}
	
	onCancelInvite() {
		this.collaboratorsToInvite = [];
		this.message = "";		
	}

	get tutors(): User[] {
		return this.allTutors.filter(user => this.canAddUserToInviteList(user) ? user : null);
	}

	ngOnDestroy(): void {
		this.dialogRef.close(this.collaborators.length > 0 /* entry is shared */);
	}

	private search(terms: Observable<string>) {		
		return terms
			.debounceTime(400)
			.distinctUntilChanged()
			.switchMap(term => {
				this.cachedQuery = term;
				return this.userService.getUsers(term)
			});
	}

	private loadCollaborators() {
		this.entriesService.getCollaborators(this.entryId)
			.subscribe((collaborators: User[]) => {
				this.collaborators = collaborators;
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the collaborators, please try again.
			0	${error}`));  
	}

	private canAddUserToInviteList(user: User): boolean {
		return !this.collaboratorsToInvite.find(u => u.email === user.email)
			&& !this.collaborators.find(u => u.email === user.email)
			&& user.email !== this.user.email
	}
}
