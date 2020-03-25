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
import { Component, Inject, OnInit } from "@angular/core"
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatRadioChange } from '@angular/material/radio';

import { SkillGrouping } from "../../core/dtos";

@Component({
	template: `
	<div mat-dialog-title>
		<h1>Change the Skill grouping</h1> 
	</div>
	<div mat-dialog-content class="row">
		<div class="col">
			<mat-radio-group [(ngModel)]="selectedSkillGrouping" (change)="onSkillGroupingChange($event)">
				<div *ngFor="let skillGrouping of skillGroupings" >
				<mat-radio-button labelPosition="after" [value]="skillGrouping">{{skillGrouping.name}}</mat-radio-button>
				</div>
			</mat-radio-group>
		</div>
	</div>`
})
export class DialogChangeSkillGroupingComponent implements OnInit {

	selectedSkillGrouping: SkillGrouping;
	skillGroupings: SkillGrouping[]

	constructor(
		@Inject(MAT_DIALOG_DATA) data: any,
		private dialogRef: MatDialogRef<DialogChangeSkillGroupingComponent>) {
			this.selectedSkillGrouping = data.selectedSkillGrouping;
			this.skillGroupings = data.skillGroupings;
	}

	ngOnInit(): void {
	}

	onSkillGroupingChange(event: MatRadioChange) {
		let skillGrouping = event.value as SkillGrouping;		
		this.dialogRef.close(skillGrouping);
	}
}
