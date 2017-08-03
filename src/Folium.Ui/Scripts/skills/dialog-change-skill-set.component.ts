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
import { Component, Inject, Input, Output, OnInit } from "@angular/core"
import { MAT_DIALOG_DATA, MatDialogRef, MatRadioChange } from '@angular/material';

import { SkillSet } from "../dtos";

@Component({
	templateUrl: "html/skills/dialog-change-skill-set.html"
})
export class DialogChangeSkillSetComponent implements OnInit {

	selectedSkillSet: SkillSet;

	constructor(
		@Inject(MAT_DIALOG_DATA) private skillSets: SkillSet[],
		private dialogRef: MatDialogRef<DialogChangeSkillSetComponent>) {
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
