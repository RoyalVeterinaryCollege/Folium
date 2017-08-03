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
import { Component, Input, Output, EventEmitter, OnInit } from "@angular/core";
import { FormBuilder, FormGroup, Validators, FormGroupDirective, NgForm } from '@angular/forms';

import { MatDatepicker } from "@angular/material";

import { Placement, User } from "./../dtos";
import { PlacementsService } from "./placements.service";
import { NotificationService } from "./../common/notification.service";

@Component({
  selector: "placement-editor",
  templateUrl: "html/placements/placement-editor.component.html"  
})
export class PlacementEditorComponent implements OnInit {
  @Input()
  placement: Placement;
  
  @Input()
  user: User;
  
	@Output() 
  onSaved = new EventEmitter<Placement>();
  
  @Output() 
  onClose = new EventEmitter<Placement>();

  placementForm: FormGroup;
	placementSaved: boolean = false;
	savePending: boolean = false;
	autoSaveEnabled: boolean = true; // autosave can be disabled if an error occurs.
  touchUi = false; // Used for the mat-datepicker to load in a mobile friendly format.
    
  constructor(
	  private placementsService: PlacementsService,
	  private notificationService: NotificationService,
	  private formBuilder: FormBuilder) { 
      this.placement = new Placement();
		  this.autoSave = this.autoSave.bind(this); //autoSave can be called from a child component.
  }

  ngOnInit(){
    this.initialiseFormControls();
    this.touchUi = window.innerHeight <= 600;
  }

	onCloseClick(event: Event) {		
		event.preventDefault();
		this.close();
  }

	onCreatePlacementClick() {
    this.savePlacement();
  }

	onSaveClick() {		
		this.autoSaveEnabled = true; // enable the autosave.
		this.savePlacement();
	}

	openPicker(picker:MatDatepicker<Date>){
		if(!this.touchUi) picker.open();
	}

	autoSave() {
		// Save the entry if we are editing.
		if(this.isEdit && this.autoSaveEnabled) {
			this.savePlacement();
		}
  }
  
	ngOnDestroy() {
		this.autoSave();
		this.close();
  }
  
  private get isEdit() {
	  return this.placement.id !== undefined;
  }

	private savePlacement() {
		const formValues = this.placementForm.value;

		// Only save if there are any changes.
		if(!this.hasFormModelUpdates(formValues)) return;
		this.savePending = true;

		let placement = this.extractPlacementFromForm(formValues)
		
		let response = this.isEdit ? this.placementsService.updatePlacement(placement) : this.placementsService.createPlacement(placement);
		response.subscribe((updatePlacement: Placement) => {
      if(!this.isEdit){
        this.onClose.emit(updatePlacement);
      }
      this.placement = updatePlacement;
			this.initialiseFormControls();
			this.placement.lastUpdatedAt = new Date();
			this.onSaved.emit(this.placement);
			this.savePending = false;
      this.placementSaved = true;
		},
		(error: any) => {
			this.notificationService.addDanger(`There was an error trying to save the placement, autosave has been disabled, you will need to manually save any changes.
			${error}`);
			this.savePending = false;
			this.autoSaveEnabled = false;
			this.placementSaved = false;
		});
  }
  
	private hasFormModelUpdates(formValues?: any): boolean {
    if(!formValues) {      
      formValues = this.placementForm.value;
    }
		// Check for changes in the form model compared to the data model.
		return this.placement.title !== formValues.title ||
			this.placement.start.getTime() !== this.getUTCDate(formValues.start).getTime() ||
			this.placement.end.getTime() !==  this.getUTCDate(formValues.end).getTime();
  }
    
  private initialiseFormControls() {
	   this.placementForm = this.formBuilder.group({
		  title: [this.placement.title, Validators.required],
		  start: [this.placement.start, Validators.required],
		  end: [this.placement.end, Validators.required]
	  });
		this.placementForm.valueChanges.subscribe(changes => {
      if(this.isEdit && this.hasFormModelUpdates()) { this.savePending = true }
    });
  }
  
	private extractPlacementFromForm(formValues: any): Placement {		
		// Transfer from the form.
		const placement = new Placement();placement.id = this.placement.id;
	  placement.userId = this.user.id
	  placement.title = formValues.title;
	  placement.start = this.getUTCDate(formValues.start);
    placement.end = this.getUTCDate(formValues.end);
    placement.entryCount = this.placement.entryCount;
    placement.lastUpdatedAt = this.placement.lastUpdatedAt;

		return placement;
  }
  
  private getUTCDate(date:any): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  }
  
  // raise the close event.
	private close() {
		if(!this.isEdit) {
			this.onClose.emit();
		} else{
			// Raise the close event with the latest form updates.		
			const formValues = this.placementForm.value;
			let placement = this.extractPlacementFromForm(formValues);		
			this.onClose.emit(placement);
		}
	}
}