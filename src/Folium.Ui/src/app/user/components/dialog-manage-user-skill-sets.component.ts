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
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
	template: "<manage-user-skill-sets [user]='user' (onDone)='onDone()'></manage-user-skill-sets>"
})
export class DialogManageUserSkillSetsComponent {

	user: any;
	constructor(
		@Inject(MAT_DIALOG_DATA) user: any,
		private dialogRef: MatDialogRef<DialogManageUserSkillSetsComponent>) {
			this.user = user;
	}

	onDone() {
		this.dialogRef.close();
	}
}
