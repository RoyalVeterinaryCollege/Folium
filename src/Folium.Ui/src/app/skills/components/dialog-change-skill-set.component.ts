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
import { MAT_DIALOG_DATA, MatDialogRef, MatRadioChange } from '@angular/material';

import { SkillSet } from "../../core/dtos";

@Component({
	template: `
	<div mat-dialog-title>
		<h1>Select your Skill Set</h1> 
	</div>
	<div mat-dialog-content class="row">
		<div class="col">
			<mat-radio-group [(ngModel)]="selectedSkillSet" (change)="onSkillSetChange($event)">
				<div *ngFor="let skillSet of skillSets" >
				<mat-radio-button labelPosition="after" [value]="skillSet">{{skillSet.name}}</mat-radio-button>
				</div>
			</mat-radio-group>
		</div>
	</div>`
})
export class DialogChangeSkillSetComponent implements OnInit {

	selectedSkillSet: SkillSet;
	skillSets: SkillSet[];

	constructor(
		@Inject(MAT_DIALOG_DATA) skillSets: SkillSet[],
		private dialogRef: MatDialogRef<DialogChangeSkillSetComponent>) {
			this.skillSets = skillSets;
	}

	ngOnInit(): void {
		let selectedSkillSet = this.skillSets.find(s => s.selected);
		if(selectedSkillSet) {
			this.selectedSkillSet = selectedSkillSet;
		}
	}

	onSkillSetChange(event: MatRadioChange) {
		let skillSet = event.value as SkillSet;
		this.skillSets.forEach(s => s.selected = false);
		skillSet.selected = true;
		this.dialogRef.close();
	}
}
