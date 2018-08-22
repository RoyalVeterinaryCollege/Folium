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
import { Component, Inject } from "@angular/core"
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material';
import { FormGroup, FormBuilder, Validators } from "@angular/forms";

import { User } from "../../core/dtos";
import { NotificationService } from "../../core/notification.service";
import { ReportsService } from "../reports.service";

@Component({
	templateUrl: "dialog-message-users.component.html"
})
export class DialogMessageUsersComponent {
	messageForm: FormGroup;
	user: User;
	toUsers: User[];
	processing = false;
	summaryUsersOnly = true;
	
	get summaryUserCount():number {
        return this.toUsers.length > 6 ? 6 : this.toUsers.length;
	}
	
	constructor(
		@Inject(MAT_DIALOG_DATA) data: any,
		private dialogRef: MatDialogRef<DialogMessageUsersComponent>,
		private reportsService: ReportsService,
		private formBuilder: FormBuilder,
		private notificationService: NotificationService) {
			this.user = data.user;
			this.toUsers = data.toUsers;
	}

	ngOnInit() {
		this.messageForm = this.formBuilder.group({
			body: ["", Validators.required]
		});
	}
	
	onCancelClick() {
		this.dialogRef.close();
	}
	
	onRemoveUserClick(user: User) {
		if(this.toUsers.length > 1 && this.toUsers.includes(user)){
			this.toUsers.splice(this.toUsers.indexOf(user), 1);
		}
	}

	onSendClick() {
		let body = this.messageForm.value.body;

		this.processing = true;
		this.reportsService.sendMessage(this.toUsers.map(u => u.id), body)
			.subscribe(_ => {
				this.notificationService.addSuccess('Message successfully sent.')
				this.dialogRef.close();
			},
		  (error: any) => {
			this.notificationService.addDanger(`There was an error trying to send the message..
			${error}`);
			this.processing = false;
		  }
		);
	}
	
	onShowAllUsersClick() {
		this.summaryUsersOnly = false;
	}

	getUserLabel(user: User): string {
		return user.firstName ? `${user.firstName} ${user.lastName}` : user.email;
	}
}
