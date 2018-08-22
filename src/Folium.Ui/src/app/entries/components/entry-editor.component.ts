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
import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, ViewChild } from "@angular/core";
import { FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { MatDialog } from '@angular/material';

import { Subscription, Observable, Subject, forkJoin } from "rxjs";
import { debounceTime, startWith, flatMap, tap, map } from "rxjs/operators";

import { Entry, Skill, Where, EntryType, EntrySummary, SkillGroup, Placement, User, SkillSet, SkillGrouping, SelfAssessments } from "../../core/dtos";
import { EntriesService } from "../entries.service";
import { SkillBundleService } from "../../skills/skill-bundle.service";
import { SkillService } from "../../skills/skill.service";
import { NotificationService } from "../../core/notification.service";
import { DialogHelpComponent } from "../../core/components/dialog-help.component";
import { SkillAssessmentService } from "../../skills/skill-assessment.service";
import { ModalDirective } from "ngx-bootstrap/modal";
import { DialogDeleteConfirmComponent } from "../../core/components/dialog-delete-confirm.component";
import { DialogShareEntryComponent } from "./dialog-share-entry.component";
import { DialogManageUserSkillSetsComponent } from "../../user/components/dialog-manage-user-skill-sets.component";
import { UserService } from "../../user/user.service";
import { SkillFiltersService } from "../../skills/skill-filters.service";
import { DialogChangeSkillGroupingComponent } from "../../skills/components/dialog-change-skill-grouping.component";

@Component({
	selector: "entry-editor",
	templateUrl: "entry-editor.component.html",
  	providers: [SkillBundleService, SkillFiltersService] // Use a new instance of the skills bundle and filters.
})
export class EntryEditorComponent implements OnInit, OnDestroy {
	@Input()
	entrySummary: EntrySummary;
	
	@Input()
	placement: Placement;

	@Input()
	user: User;

	@Output() 
	onSaved = new EventEmitter<Entry>();
	
	@Output() 
	onClose = new EventEmitter<Entry>();
	
	@Output()
	onRemoveEntry = new EventEmitter<EntrySummary>();
	
	entry: Entry = new Entry();
	entryForm: FormGroup;
	bundleSize: number = 0;
	whereData: Where[];
	filteredWhereData: Observable<Where[]>;
	entryTypes: EntryType[];
	entryTypeSet: boolean; // An undefined EntryType is valid, so we need to track if one has been selected.
	skillSets: SkillSet[];
	skillGroupings: SkillGrouping[];
	skillGrouping: SkillGrouping;
	
	selectedSkillSet: SkillSet;
	skillGroups: SkillGroup[];
	isSkillsModalShown: boolean = false;
	entrySaved: boolean = false;
	savePending: boolean = false;
	autoSaveEnabled: boolean = true; // autosave can be disabled if an error occurs.
	removed: boolean = false;

	private saveRequested$: Subject<any> = new Subject<any>();

	private skillsBundleChanges$: Subscription;

	constructor(
		private entriesService: EntriesService,
		private skillService: SkillService,
		private userService: UserService,
		private skillBundleService: SkillBundleService,
		private skillFiltersService: SkillFiltersService,
		private skillAssessmentService: SkillAssessmentService,
		private notificationService: NotificationService,
		private formBuilder: FormBuilder,
    	private dialog: MatDialog) {
		this.autoSave = this.autoSave.bind(this); //autoSave can be called from a child component.
	}

	@ViewChild("skillsModal")
	skillsModal: ModalDirective;
	
	ngOnInit() {
		this.loadEntry();
		this.skillsBundleChanges$ = this.skillBundleService.onBundleChange.subscribe(_ => this.onBundleChange());
		this.skillsBundleChanges$ = this.skillBundleService.onSkillAssessmentChange.subscribe(skill => this.onSelfAssessmentChange(skill));
		// We want to debounce the save requests fo we don't send too many.
		this.saveRequested$.pipe(
			debounceTime(500)
		).subscribe(request => {
			this.doSaveEntry();
		});
	}

	filterWhereData(name) {
		return name ? this.whereData.filter(where => where.name.toLowerCase().indexOf(name.toLowerCase()) === 0)
					: this.whereData;
	}

	showSkillsModal() {
		this.isSkillsModalShown = true;
	}
	
	onSkillsModalHidden() {
		this.isSkillsModalShown = false;
		// Clear any search terms.
		this.skillFiltersService.clearSearch();
	}

	onCloseClick() {
		this.close();
	}
	
	onSkillSetClick(skillSet: SkillSet) {
		this.selectSkillSet(skillSet);
	}

	onSelectUserSkillSetClick(){
    	this.dialog.open(DialogManageUserSkillSetsComponent, {
			data: this.user
		}).afterClosed().subscribe(_ => {
			this.loadSkillSets();
		});
	}
	
	onChangeSkillGroupingClick() {		
		this.dialog.open(DialogChangeSkillGroupingComponent, {
		  data: {skillGroupings: this.skillGroupings, selectedSkillGrouping: this.skillGrouping}
		}).afterClosed().subscribe(skillGrouping => {
		  if(skillGrouping && this.skillGrouping.id !== skillGrouping.id) {
				this.entriesService.changeEntrySkillGrouping(this.entry.id, skillGrouping.id)
					.subscribe(_ => {
						this.skillGrouping = skillGrouping;
						this.entry.skillGroupingId = skillGrouping.id;
						this.skillGroups = undefined;
						this.getSkillGroups(this.selectedSkillSet.id)
							.subscribe(skillsGroup => this.skillGroups = skillsGroup,
								(error: any) => this.notificationService.addDanger(`There was an error trying to load the skills, please try again.
								${error}`));
					},
					(error: any) => this.notificationService.addDanger(`There was an error trying to change the skill grouping, please try again.
					${error}`));
		  }
		});
	}

	onEntryTypeClick(entryType: EntryType){
		let getSkillGroupsComplete = function(skillsGroup: SkillGroup[], scope) {
			scope.entry.entryType = entryType;
			scope.entryTypeSet = true;
			scope.skillGroups = skillsGroup;
			// Inialise the rest of the form now we have a template.
			scope.initialiseDynamicFormControls();
			// Save the entry.
			scope.saveEntry();
		};
		
		this.entry.skillGroupingId = entryType ? entryType.template.skillGroupingId : undefined; // Set this here as it is needed when we fetch the skill groups.
		if(entryType && entryType.template.skillBundleIds) {
			let skillIds: number[] = [];
			let task$: Observable<number[]>[] = [];				
			entryType.template.skillBundleIds.forEach(skillBundleId => {
				task$.push(this.skillService.getSkillBundle(this.entry.skillSetId, skillBundleId));
			});
			forkJoin<Observable<number[]>>(...task$).subscribe(results => { 
				results.forEach(result => skillIds = skillIds.concat());
				// Make sure the skill ids are unique.
				skillIds = skillIds.filter((v, i, a) => a.indexOf(v) === i);
				// Load the skill groups.
				this.getSkillGroups(this.selectedSkillSet.id).subscribe((groups) => {
					this.setBundleItems(groups, skillIds);
					getSkillGroupsComplete(groups, this);
				},
				(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry type, please try again. [skill groups load error]
				${error}`));
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry type, please try again. [skill bundle load error]
			${error}`));
		}
		else {
			// Load the skill groups.
			this.getSkillGroups(this.selectedSkillSet.id).subscribe((groups) =>
				getSkillGroupsComplete(groups, this),
				(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry type, please try again. [skill groups load error]
				${error}`));
		}
	}

	onHelpClick(helpText: string){
    	this.dialog.open(DialogHelpComponent, {
			data: helpText,
		});
	}
	
	autoSave() {
		// Save the entry if we are editing.
		if(this.isEdit && this.autoSaveEnabled) {
			this.saveEntry();
		}
	}
	
	onSaveClick() {		
		this.autoSaveEnabled = true; // enable the autosave.
		this.saveEntry();
	}

	onTitleFocus() {
		if(this.entryForm.value.title === 'Untitled Entry') {
			this.entryForm.patchValue({title:''})
		}
	}

	onTitleBlur() {
		if(this.entryForm.value.title === '') {
			this.entryForm.patchValue({title:'Untitled Entry'})
		}
	}

	removeEntry() {
		let dialogRef = this.dialog.open(DialogDeleteConfirmComponent);
		dialogRef.afterClosed().subscribe(result => {
			if(result === "true") {
				this.removed = true;
				this.onRemoveEntry.emit(this.entrySummary);
			}
		});
	}
	
	shareEntry() {
		let dialogRef = this.dialog.open(DialogShareEntryComponent, {
			data: { entryId: this.entry.id, user: this.user },
		});
		dialogRef.afterClosed().subscribe((result: boolean) => {
			this.entry.shared = result;
			this.entrySummary.shared = result;
		});
	}

	ngOnDestroy() {
		this.skillsBundleChanges$.unsubscribe();
		if(!this.removed) {
			this.autoSave();
			this.close();
		}
	}

	private loadEntry() {
		if(this.entrySummary) {
			this.entriesService.getEntry(this.entrySummary.id)
				.subscribe((entry: Entry) => {
					this.entry = entry;
					this.entryTypeSet = true;
					this.loadSkillSets();
					this.initialiseForm();
				},
				(error: any) => this.notificationService.addDanger(`There was an error trying to load the entry, please try again.
				${error}`));
		} else {
			this.loadSkillSets();
			this.initialiseForm();
		}
	}
	
	private loadSkillSets() {
		this.skillSets = undefined;
		this.userService.getSkillSets(this.user)
			.subscribe(skillSets => {
				this.skillSets = skillSets;
				// If editing an entry then set the skillset in use, otherwise try and use the currently selected one.
				if(this.isEdit) {
					this.selectedSkillSet = this.skillSets.find(set => set.id == this.entry.skillSetId);
					if(!this.selectedSkillSet) {
						// The user may no longer have this skill set in their list, get all the skill sets.
						this.skillService.getSkillSets()
							.subscribe(allSkillsSets => {
								this.selectedSkillSet = allSkillsSets.find(set => set.id == this.entry.skillSetId);								
								if(!this.selectedSkillSet) {									
									this.notificationService.addDanger(`The skill set for this entry [${this.entry.skillSetId}] is no longer valid.`);
								} else {
									this.getSkillGroups(this.selectedSkillSet.id)
										.subscribe(skillsGroup => this.skillGroups = skillsGroup);
								}
							},
							(error: any) => this.notificationService.addDanger(`There was an error trying to load all skill sets, please try again.
								${error}`)); 
					} else {
						this.getSkillGroups(this.selectedSkillSet.id)
							.subscribe(skillsGroup => this.skillGroups = skillsGroup);
					}
				} else {
					// try and use the skill set that is selected.
					this.selectedSkillSet = this.skillSets.find(set => set.selected);
					if (this.selectedSkillSet) {
						this.selectSkillSet(this.selectedSkillSet);
					}
				}
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load your skill sets, please try again.
				${error}`)); 
	}

	private getSkillGroups(skillSetId: number) : Observable<SkillGroup[]> {		
		// First load the skill groupings.
		return this.skillService.getSkillGroupings(skillSetId).pipe(
			flatMap(skillGroupings => {
				this.skillGroupings = skillGroupings;
				// Do we have a skill grouping for this entry?
				if(this.entry && this.entry.skillGroupingId) {
					// Use the skill grouping set on the entry.
					this.skillGrouping = this.skillGroupings.find(t => t.id === this.entry.skillGroupingId);
				}
				if(!this.skillGrouping) {
					this.skillGrouping = this.skillService.getDefaultSkillGrouping(this.skillGroupings);
				}
				return this.skillService
					.getSkillGroups(skillSetId, this.skillGrouping.id).pipe(
						flatMap(skillGroups => {
							return this.skillAssessmentService.setUserSkillAssessmentsForSkillGroups(this.user.id, skillSetId, skillGroups).pipe(
								tap(_ => {
									if(this.entry) {
										this.setBundleItems(skillGroups, this.entry.assessmentBundle);
									}
								})
							);
						})
					);
			})
		);
	}
	
	private selectSkillSet(skillSet: SkillSet) {
		this.skillSets.forEach(set => set.selected = false);
		skillSet.selected = true;
		this.selectedSkillSet = skillSet;
		this.entryTypes = undefined;
		this.entriesService.getEntryTypes(this.skillSets.map(s => s.id)) // this will only cause a round trip once as it will be cached on the service.
			.subscribe(entryTypes => {
				this.entryTypes = entryTypes.filter(t => t.skillSetId == skillSet.id)
			});
	}

	private saveEntry() {
		this.saveRequested$.next(); // Just request a save, we want to throttle them.
	}

	private doSaveEntry() {
		const formValues = this.entryForm.value;

		// Only save if there are any changes.
		if(!this.hasFormModelUpdates(formValues)) return;
		this.savePending = true;

		let entry = this.extractEntryFromForm(formValues);
		
		let response = this.isEdit ? this.entriesService.updateEntry(entry) : this.entriesService.createEntry(entry);
		response.subscribe((updatedEntry: Entry) => {
			updatedEntry.entryType = this.entry.entryType;
			this.entry = updatedEntry;
			this.onSaved.emit(this.entry);
			this.initialiseFormControls();
			this.entry.lastUpdatedAt = new Date();
			this.savePending = false;
			this.entrySaved = true;
			if(!this.entrySummary) {
				this.entrySummary = new EntrySummary(this.entry);
			}
		},
		(error: any) => {
			this.notificationService.addDanger(`There was an error trying to save the entry, autosave has been disabled, you will need to manually save any changes.
			${error}`);
			this.savePending = false;
			this.autoSaveEnabled = false;
			this.entrySaved = false;
		});
	}

	private hasFormModelUpdates(formValues: any): boolean {
		// Check for changes in the form model compared to the data model.
		return this.entry.title !== formValues.title ||
			this.entry.where !== formValues.where ||
			JSON.stringify(this.entry.description) !== JSON.stringify(this.extractDynamicFormControls(formValues)) ||
			JSON.stringify(this.entry.assessmentBundle) !== JSON.stringify(this.skillBundleService.getSelfAssessmentsInBundle(this.skillGroups)); 
	}

	private onBundleChange() {
		this.bundleSize = this.skillBundleService.bundleSize;
		this.saveEntry();
	}

	private setBundleItems(skillGroups: SkillGroup[], bundle: SelfAssessments|number[]) {
		if(bundle instanceof SelfAssessments) {
			this.skillBundleService.setBundleItems(skillGroups, bundle);
		} else {
			this.skillBundleService.setBundleItems(skillGroups, bundle, this.user.id);
		}		
		this.bundleSize = this.skillBundleService.bundleSize;
	}

	private onSelfAssessmentChange(skill: Skill) {
		if(this.isEdit) {
			// If we are editing the entry then set the assessment time to the same as the entry.
			skill.assessment.activeSelfAssessment.createdAt = this.entry.when;
		}
		this.saveEntry();
	}
	
	private get isEdit() {
		return this.entry.id !== undefined;
	}

	private initialiseForm() {
		// Load autocomplete data if we don't have a placement.
		if(!this.placement) {
			this.entriesService.getUserPlaces()
				.subscribe((data) => {
					this.whereData = data;
					this.filteredWhereData = this.entryForm.controls.where.valueChanges.pipe(
						startWith(this.entry.where),
						map(val => val ? this.filterWhereData(val) : this.whereData.slice())
					);
				});
		}
		
		this.initialiseFormControls();
	}

	private initialiseFormControls() {
		if (this.entryForm) {
			// Patch any changes, such as default values.
			this.entryForm.patchValue({
				title:this.entry.title,
				where: this.entry.where
			})
		} else {
			this.entryForm = this.formBuilder.group({
				title: this.entry.title,
				where: this.placement ? this.placement.fullyQualifiedTitle : this.entry.where
			});
			// The rest of the form is dynamic, made up from the template, initialise it.
			this.initialiseDynamicFormControls();
		}
	}

	private initialiseDynamicFormControls() {
		if(!this.entryTypeSet) return;
		if(this.entry.entryType) {
			let index = 0;
			this.entry.entryType.template.inputs.forEach(field => {
				this.entryForm.addControl("template_control_" + index, new FormControl(this.entry.description ? this.entry.description[index] : ""));
				index++;
			}); 
		} else {			
			this.entryForm.addControl("description", new FormControl(this.entry.description ? this.entry.description : ""));
		}
		this.entryForm.valueChanges.subscribe(changes => this.savePending = true);
	}

	private extractEntryFromForm(formValues: any): Entry {		
		// Transfer from the form.
		const entry = new Entry();
		entry.id = this.entry.id;
		entry.title = formValues.title;
		entry.where = formValues.where;
		entry.when = this.entry.when;
		entry.skillSetId = this.isEdit ? this.entry.skillSetId : this.selectedSkillSet.id;
		entry.skillGroupingId = this.skillGrouping.id;
		entry.author = this.user;
		entry.shared = this.entry.shared;
		if(this.entry.entryType) {
			entry.entryType = new EntryType();
			entry.entryType.id = this.entry.entryType.id;
			entry.entryType.name = this.entry.entryType.name;
		}

		// Assign the dynamic values.
		entry.description = this.extractDynamicFormControls(formValues);

		// Assign the skills bundle onto the entry to save.
		entry.assessmentBundle = this.skillBundleService.getSelfAssessmentsInBundle(this.skillGroups);

		// Set the date if this is a new entry.
		entry.when = this.isEdit ? entry.when : new Date(Date.now());

		return entry;
	}

	private extractDynamicFormControls(formValues: any): any {
		if(this.entry.entryType) {
			let index = 0;
			let values: string[] = [];
			this.entry.entryType.template.inputs.forEach(field => {
				values.push(formValues["template_control_" + index]);
				index++;
			}); 
			return values;
		} else {
			return formValues["description"];
		}
	}

	// raise the close event.
	private close() {
		if(!this.isEdit) {
			this.onClose.emit();
		} else{
			// Raise the close event with the latest form updates.		
			const formValues = this.entryForm.value;
			let entry = this.extractEntryFromForm(formValues)		
			this.onClose.emit(entry);
		}
	}
}