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
import { Component, OnInit, ViewChild, ElementRef } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { FormGroup, FormBuilder, Validators } from "@angular/forms";
import { DomSanitizer, SafeHtml } from "@angular/platform-browser";
import { MatAutocompleteSelectedEvent, MatDatepicker, MatTable, MatTableDataSource, MatSort, MatPaginator, MatDialog } from "@angular/material";

import { Observable } from "rxjs";
import { startWith, map } from "rxjs/operators";

import { ScrollToService } from '@nicky-lenaers/ngx-scroll-to';

import { Angular5Csv } from 'angular5-csv/Angular5-csv';

import { User, ReportOnOption, SkillSet, EntryType, EntryEngagementReportCriteria, EntryEngagementReportResultSet, EntryEngagementUser } from "../../core/dtos";
import { ReportsService } from "../reports.service";
import { SkillService } from "../../skills/skill.service";
import { NotificationService } from "../../core/notification.service";
import { Utils } from "../../core/utils";
import { EntriesService } from "../../entries/entries.service";
import { DialogMessageUsersComponent } from "./dialog-message-users.component";

enum EntryEngagementTypes {
  Entry_Engaged = 'Entry Engaged',
  Entry_NonEngaged = 'Entry Non-Engaged',
  SharedEntry_Engaged = 'Shared Entry Engaged',
  SharedEntry_NonEngaged = 'Shared Entry Non Engagued',
  TutorSharedEntry_Engaged = 'Tutor Shared Entry Engaged',
  TutorSharedEntry_NonEngaged = 'Tutor Shared Entry Non Engagued',
  CommentedEntry_Engaged = 'Commented Entry Engaged',
  CommentedEntry_NonEngaged = 'Commented Entry Non Engagued'
}

@Component({
  templateUrl: "view-entry-engagement.component.html",
})
export class ViewEntryEngagementComponent implements OnInit {
  currentUser: User;
	reportForm: FormGroup;
  reportOnOptions: ReportOnOption[] = [];  
  filteredReportOnOptions: Observable<any[]>;
  filteredReportOnOptionsLength: number;
  skillSets: SkillSet[];
  filteredSkillSets: Observable<any[]>;
  filteredSkillSetsLength: number;
  entryTypes: EntryType[] = [];  
  filteredEntryTypes: Observable<any[]>;
  filteredEntryTypesLength: number;
  maxOptionsToDisplay = 50;
  minDate: Date;
  touchUi = false; // Used for the mat-datepicker to load in a mobile friendly format.
  resultSet: EntryEngagementReportResultSet;
  resultSummary: EntryEngagementSummary;
  resultTimelines: EntryEngagementTimeLine[];
	fetchingReportData: boolean = false;
  reportsService: ReportsService;
  userList: MatTableDataSource<EntryEngagementUser>;
  entryEngagementTypes = EntryEngagementTypes; // Used to reference enum from the template. 
  userToView: number; // The user to view in more detail.

	@ViewChild("reportOnInput")
  reportOnInput: ElementRef;
  
	@ViewChild("skillSetInput")
  skillSetsInput: ElementRef;

	@ViewChild("entryTypesInput")
  entryTypesInput: ElementRef;

  @ViewChild("userListTable")
  userListTable: MatTable<EntryEngagementUser>;
  
	@ViewChild("scrollToUserList")
  scrollToUserList: ElementRef;
  
  private userListPaginator: MatPaginator;
  
  @ViewChild(MatSort) 
  set matSort(matSort: MatSort) {
    if(this.userList) {
      this.userList.sort = matSort;
    }
  }

  @ViewChild(MatPaginator) 
  set matPaginator(matPaginator: MatPaginator) {
    this.userListPaginator = matPaginator;
    if(this.userList) {
      this.userList.paginator = matPaginator;
    }
  }

  constructor(
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer,
    reportsService: ReportsService,
    private entriesService: EntriesService,
    private skillService: SkillService,
    private formBuilder: FormBuilder,
    private notificationService: NotificationService,
    private scrollToService: ScrollToService,
		private dialog: MatDialog) { 
      this.reportsService = reportsService;
    }
  
  ngOnInit() {
    this.route.data.forEach((data: { currentUser: User }) => {
      this.currentUser = data.currentUser;
    });

    this.reportForm = this.formBuilder.group({
      who: [new Array<ReportOnOption>(), Validators.required],
      whoQuery: ["", Validators.required],
      skillSet: [undefined as SkillSet],
      skillSetQuery: [""],
      entryTypes: [new Array<EntryType>()],
      entryTypeQuery: [""],
      from: undefined as Date,
      to: undefined as Date
    });
    
    this.reportsService.getUserOptions().subscribe(results => {
      this.reportOnOptions = results;
      this.filteredReportOnOptions = this.reportForm.get("whoQuery").valueChanges.pipe(
        startWith(null),  
        map((name) => {          
          let options = (name === null || (typeof(name) === "object")) ? this.reportOnOptions.slice() : this.filterReportOn(name);
          this.filteredReportOnOptionsLength = options.length;
          if(this.filteredReportOnOptionsLength > this.maxOptionsToDisplay) {
            options = options.slice(0, this.maxOptionsToDisplay);
          }
          return options;
        })
      );
    });
    
    this.reportsService.getEntryEngagementMinDate().subscribe(result => {
      this.minDate = result;
    });
    
    this.skillService.getSkillSets().subscribe(skillSets => {
      this.skillSets = skillSets;
      this.filteredSkillSets = this.reportForm.get("skillSetQuery").valueChanges.pipe(
        startWith(null), 
        map((name) => {          
          let filteredSkillSets = (name === null || (typeof(name) === "object")) ? this.skillSets.slice() : this.filterSkillSets(name);
          this.filteredSkillSetsLength = filteredSkillSets.length;
          if(this.filteredSkillSetsLength > this.maxOptionsToDisplay) {
            filteredSkillSets = filteredSkillSets.slice(0, this.maxOptionsToDisplay);
          }
          return filteredSkillSets;
        })
      );
    })
  }
  
	highlightReportOnMatch(name: string): SafeHtml {    
    let query = this.reportOnInput ? this.reportOnInput.nativeElement.value : "";
		let replaced = query ? name.replace(new RegExp(`(${query})`,"ig"), "<strong>$1</strong>") : name;
		return this.sanitizer.bypassSecurityTrustHtml(replaced);
  }
  
  filterReportOn(name: string) {
    return this.reportOnOptions.filter(option =>
        option.name.toLowerCase().indexOf(name.toLowerCase()) >= 0);
  }

	highlightSkillSetsMatch(name: string): SafeHtml {    
    let query = this.skillSetsInput ? this.skillSetsInput.nativeElement.value : "";
		let replaced = query ? name.replace(new RegExp(`(${query})`,"ig"), "<strong>$1</strong>") : name;
		return this.sanitizer.bypassSecurityTrustHtml(replaced);
  }
  
  filterSkillSets(name: string) {
    return this.skillSets.filter(set =>
        set.name.toLowerCase().indexOf(name.toLowerCase()) >= 0);
  }

	highlightEntryTypeMatch(name: string): SafeHtml {    
    let query = this.entryTypesInput ? this.entryTypesInput.nativeElement.value : "";
		let replaced = query ? name.replace(new RegExp(`(${query})`,"ig"), "<strong>$1</strong>") : name;
		return this.sanitizer.bypassSecurityTrustHtml(replaced);
  }
  
  filterEntryTypes(name: string) {
    return this.entryTypes.filter(entryType =>
      entryType.name.toLowerCase().indexOf(name.toLowerCase()) >= 0);
  }
	
	onReportOnOptionSelected(event: MatAutocompleteSelectedEvent) {
		let who = event.option.value as ReportOnOption;
    if(this.canAddToReportOnList(who)) {
      let currentWho = this.reportForm.value.who;
      this.reportForm.patchValue({
        who: currentWho ? currentWho.concat(who) : new Array<ReportOnOption>(who)
      });
      this.reportOnInput.nativeElement.value = '';
    }
  }
  
	onRemoveReportOnOptionClick(who: ReportOnOption) {
    this.reportForm.patchValue({
      who: this.reportForm.value.who.filter(c => c.name !== who.name)
    });
	}
  
	onSkillSetSelected(event: MatAutocompleteSelectedEvent) {
    let skillSet = event.option.value as SkillSet;
    this.reportForm.patchValue({
      skillSet: skillSet,
      entryTypes: new Array<EntryType>()
    });
    this.skillSetsInput.nativeElement.value = '';
    // New Skill Set, so get the entry tyes for this skill set.
    this.getEntryTypes();
  }
  
	onRemoveSkillSetClick() {
    this.reportForm.patchValue({
      skillSet: undefined as SkillSet,
      entryTypes: new Array<EntryType>()
    });
  }

	onEntryTypeSelected(event: MatAutocompleteSelectedEvent) {
		let entryType = event.option.value as EntryType;
    if(this.canAddToEntryTypesList(entryType)) {
      let entryTypes = this.reportForm.value.entryTypes;
      this.reportForm.patchValue({
        entryTypes: entryTypes ? entryTypes.concat(entryType) : new Array<EntryType>(entryType)
      });
      this.entryTypesInput.nativeElement.value = '';
    }
  }
  
	onRemoveEntryTypeClick(entryType: EntryType) {
    this.reportForm.patchValue({
      entryTypes: this.reportForm.value.entryTypes.filter(c => c.name !== entryType.name)
    });
  }
  
  onGenerateReportClick() {
    const formValues = this.reportForm.value;

    let criteria = this.extractCriteriaFromForm(formValues);

    this.resultSet = undefined;
    this.resultSummary = undefined;
    this.resultTimelines = undefined;
    this.fetchingReportData = true;

    this.reportsService.getEntryEngagementReport(criteria)
		  .subscribe((results: EntryEngagementReportResultSet) => {
        this.resultSet = results
        this.sortUsersBySurname();
        let processedResults = this.processResults();
        this.resultSummary = processedResults[0];
        this.resultTimelines = processedResults[1];
        this.userList = new MatTableDataSource(this.resultSet.users);
        this.fetchingReportData = false;
        this.scrollToService.scrollTo({
          target: 'timeline-container',
          offset: -50
        });
      },
      (error: any) => {
        this.notificationService.addDanger(`There was an error trying to get the report data, please try again.
        ${error}`);
        this.fetchingReportData = false;
      }
    );
  }

  onUserClick(userId: number, modal: any) {
    this.userToView = userId;
    modal.show()
  }
  
	openPicker(picker:MatDatepicker<Date>){
		if(!this.touchUi) picker.open();
  }

  onPieChartClick(chart:string, data: {name: string, value: number} | HTMLElement) {
    // data.extra - we can't do this as the legend click event does not hold the extra field. https://github.com/swimlane/ngx-charts/blob/4d5e1767eae8397dfae79227609dc4969e7e54fa/src/common/legend/advanced-legend.component.ts#L39
    if (data instanceof HTMLElement) {
      // We want to catch when the total value legend is clicked on, as we don't have an event for it ootb.
      if(data.className && 
        (data.className.indexOf("total-value") >= 0 || data.className.indexOf("total-label") >= 0)) {
        this.userList.data = this.resultSet.users;
        this.resultSummary.activeEngagementType = null;
      }
    } else {
      switch(chart) {
        case 'entry': {
          this.userList.data = (data.name === "Engaged")
            ? this.resultSet.users.filter(u => u.totalEntries > 0) 
            : this.resultSet.users.filter(u => !u.totalEntries || u.totalEntries === 0);
          this.resultSummary.activeEngagementType = (data.name === "Engaged")
            ? EntryEngagementTypes.Entry_Engaged
            : EntryEngagementTypes.Entry_NonEngaged;
          break;
        }
        case 'shared_entry': {
          this.userList.data = (data.name === "Engaged")
            ? this.resultSet.users.filter(u => u.totalShares > 0)
            : this.resultSet.users.filter(u => !u.totalShares || u.totalShares === 0);
            this.resultSummary.activeEngagementType = (data.name === "Engaged")
              ? EntryEngagementTypes.SharedEntry_Engaged
              : EntryEngagementTypes.SharedEntry_NonEngaged;
          break;
        }
        case 'tutor_shared_entry': {
          this.userList.data = (data.name === "Engaged")
            ? this.resultSet.users.filter(u => u.totalShareWithTutor > 0)
            : this.resultSet.users.filter(u => !u.totalShareWithTutor || u.totalShareWithTutor === 0);
            this.resultSummary.activeEngagementType = (data.name === "Engaged")
              ? EntryEngagementTypes.TutorSharedEntry_Engaged
              : EntryEngagementTypes.TutorSharedEntry_NonEngaged;
          break;
        }
        case 'commented_entry': {
          this.userList.data = (data.name === "Engaged")
            ? this.resultSet.users.filter(u => u.totalComments > 0)
            : this.resultSet.users.filter(u => !u.totalComments || u.totalEntries === 0);
            this.resultSummary.activeEngagementType = (data.name === "Engaged")
              ? EntryEngagementTypes.CommentedEntry_Engaged
              : EntryEngagementTypes.CommentedEntry_NonEngaged;
          break;
        }
      }
    }
    this.userToView = null;
    this.userListPaginator.firstPage();
    this.userListTable.renderRows();
    // We can't use the scroll service here as it doesn't seem to work!
    this.scrollToUserList.nativeElement.scrollIntoView({ behavior: "smooth", block: "start" });
  }

	emailUsers() {		
		this.dialog.open(DialogMessageUsersComponent, {
      width: '800px',
      height: '600px',
		  data: {user: this.currentUser, toUsers: this.userList.data.slice(0)}
		});
  }

  downloadUsers(){
    var options = { 
      showLabels: true, 
      headers: ['id', 'First Name', 'Surname', 'Email', 'Entries', 'Shared', 'Shared With Tutor', 'Have Comments']
    };
  
    new Angular5Csv(this.userList.data, 'Entry Engagement', options);
  }

	private canAddToReportOnList(reportOn: ReportOnOption): boolean {
    let who = this.reportForm.value.who;
		return who ? !who.find(w => w.name === reportOn.name) : true;
  }

  private canAddToEntryTypesList(entrytype: EntryType): boolean {
    let entryTypes = this.reportForm.value.entryTypes;
		return entryTypes ? !entryTypes.find(w => w.name === entrytype.name) : true;
  }
  
  private getEntryTypes() {    
    this.entryTypes = [];
    this.entriesService.getEntryTypes(this.reportForm.value.skillSet.id).subscribe(results => {
      this.entryTypes = results;
      this.filteredEntryTypes = this.reportForm.get("entryTypeQuery").valueChanges.pipe(
        startWith(null),  
        map((name) => {
          let filteredEntryTypes = (name === null || (typeof(name) === "object")) ? this.entryTypes.slice() : this.filterEntryTypes(name);
          this.filteredEntryTypesLength = filteredEntryTypes.length;
          if(this.filteredEntryTypesLength > this.maxOptionsToDisplay) {
            filteredEntryTypes = filteredEntryTypes.slice(0, this.maxOptionsToDisplay);
          }
          return filteredEntryTypes;
        })
      );
    });
  }

  private extractCriteriaFromForm(formValues: any): EntryEngagementReportCriteria {
		// Transfer from the form.
    const criteria = new EntryEngagementReportCriteria();
    criteria.who = formValues.who;
    criteria.entryTypeIds = formValues.entryTypes ? formValues.entryTypes.map(e => e.id) : null;
    criteria.from = formValues.from;
    criteria.to = formValues.to;

		return criteria;
  }

  private processResults(): [EntryEngagementSummary, EntryEngagementTimeLine[]] {
    let userIndex: { [id:number] : EntryEngagementUser; } = {}; // Index of the Users.
    let engagedUsers = new Set<number>(); // Used as a quick lookup for users who have already done at least 1 entry.
    let engagementByDate: { [date:string] : number[]; } = { }; // The users who have done at least 1 entry by date.
    let minEngagementDateByUser: { [userId:number] : Date; } = {}; // Track the min engagement dates, as the ordering of engagment is not garenteed to be in date order.
    let cumulativeEngagementByDate: { [date:string] : number; } = { }; // The cumulative count of users who have done at least 1 entry by date.
    let timelines:EntryEngagementTimeLine[] = []    

    // Process each data item.
    this.resultSet.dataSet.forEach(item => {
      if(!engagedUsers.has(item.userId)) {
        // This user hasn't been seen before, so this is a new engagement which needs to be added.
        minEngagementDateByUser[item.userId] = new Date(item.when.toDateString());
        // Insert the user into the userIndex.
        userIndex[item.userId] = this.resultSet.users.find(u => u.id === item.userId);
        // Add the user to the set of engaged users.
        engagedUsers.add(item.userId);
      } else {
        // Get the current min engagement date and check if we have a new min, if so, overwrite it.
        let engagementDate = minEngagementDateByUser[item.userId];
        if(engagementDate > item.when) {
          minEngagementDateByUser[item.userId] = new Date(item.when.toDateString());
        }
      }
      // Record the stats for the user.
      let user = userIndex[item.userId];
      user.totalEntries = (user.totalEntries ? user.totalEntries : 0) + 1;
      user.totalShares = (user.totalShares ? user.totalShares : 0) + (item.sharedCount > 0 ? 1 : 0);
      user.totalShareWithTutor = (user.totalShareWithTutor ? user.totalShareWithTutor : 0) + (item.sharedWithTutorCount > 0 ? 1 : 0);
      user.totalComments = (user.totalComments ? user.totalComments : 0) + (item.commentCount > 0 ? 1 : 0);
      
      // Process the overall engagment on this date, for the timeline.
      let engagementOnThisDate = engagementByDate[item.when.toDateString()];
      // Check if there has already been engagment on this date, and either increment or set to 1.
      if(engagementOnThisDate) {
        // Check they haven't already engaged on this day.
        if(engagementOnThisDate.indexOf(item.userId) === -1) {
          engagementOnThisDate.push(item.userId);
        }
      } else {
        engagementByDate[item.when.toDateString()] = [item.userId];
      }
    });

    let allDates = Utils.getDateArray(
        this.resultSet.criteria.from ? this.resultSet.criteria.from : this.minDate, 
        this.resultSet.criteria.to ? this.resultSet.criteria.to : new Date());
    // Transfer the collected data into the correct shape.
    let cumulativeEngagementByDateDataPoints = new Array<DataPoint>();
    // First we need to loop through all the min dates and cumulate the engagement by date.
    let dates = Object.keys(minEngagementDateByUser).map(e => minEngagementDateByUser[e]);
    dates = dates.sort((a,b) => a.getTime() - b.getTime());
    let prevDate: Date = this.resultSet.criteria.from ? this.resultSet.criteria.from : this.minDate;
    dates.forEach((date, i) => {
      if(prevDate && (prevDate.getTime() !== date.getTime())){
        // This is a different date, store the cumulative figure.
        cumulativeEngagementByDate[prevDate.toDateString()] = i;
      }
      prevDate = date;
    });
    // Store the last date, if we have any data!
    if(dates.length > 0) {
      cumulativeEngagementByDate[prevDate.toDateString()] = dates.length;
      let toDate = this.resultSet.criteria.to ? this.resultSet.criteria.to : new Date();
      if(prevDate < toDate){
        cumulativeEngagementByDate[toDate.toDateString()] = dates.length;
      }
    }
    // Need to have an entry for every date.
    let currentCumulativeValue = 0;
    allDates.forEach(date => {
      if(cumulativeEngagementByDate[date.toDateString()]) {
        // We have a value for this date, up the cumulative.
        currentCumulativeValue = cumulativeEngagementByDate[date.toDateString()];
      }
      cumulativeEngagementByDateDataPoints.push(new DataPoint(date, currentCumulativeValue));
    });
    timelines.push(new EntryEngagementTimeLine("Cumulative Engagment", cumulativeEngagementByDateDataPoints))

    let engagementByDateDataPoints = new Array<DataPoint>();
    // Need to have an entry for every date.
    allDates.forEach(date => {
      let value = engagementByDate[date.toDateString()];
      engagementByDateDataPoints.push(new DataPoint(date, value ? value.length : 0));
    });
    timelines.push(new EntryEngagementTimeLine("Engagment", engagementByDateDataPoints));
    let totalUsersEngagedWithSharing = 0;
    let totalUsersEngagedSharingWithTutor = 0;
    let totalUsersHaveComments = 0;
    this.resultSet.users.forEach(u => {
      // Set any undefined fields to 0.
      u.totalEntries = u.totalEntries ? u.totalEntries : 0; 
      u.totalShares = u.totalShares ? u.totalShares : 0; 
      u.totalShareWithTutor = u.totalShareWithTutor ? u.totalShareWithTutor : 0; 
      u.totalComments = u.totalComments ? u.totalComments : 0; 
      totalUsersEngagedWithSharing = totalUsersEngagedWithSharing + (u.totalShares > 0 ? 1 : 0);
      totalUsersEngagedSharingWithTutor = totalUsersEngagedSharingWithTutor + (u.totalShareWithTutor > 0 ? 1 : 0);
      totalUsersHaveComments = totalUsersHaveComments + (u.totalComments > 0 ? 1 : 0);
    });
    
    return [new EntryEngagementSummary(this.resultSet.users.length, engagedUsers.size, totalUsersEngagedWithSharing, totalUsersEngagedSharingWithTutor, totalUsersHaveComments), timelines];
  }

  private sortUsersBySurname() {
    // Order the users by lastname.
    this.resultSet.users.sort((a,b) => {
      let lastNameA = a.lastName.toUpperCase(); // ignore upper and lowercase
      let lastNameB = b.lastName.toUpperCase();
      if (lastNameA < lastNameB) {
        return -1;
      }
      if (lastNameA > lastNameB) {
        return 1;
      }    
      // names must be equal
      return 0;
    });
  }
}

class EntryEngagementSummary {
  engagement: DataPoint[];
  shared: DataPoint[];
  sharedWithTutor: DataPoint[];
  commented: DataPoint[];
  public activeEngagementType: EntryEngagementTypes;

  constructor(totalUsers: number, totalEngagedUsers: number, totalUsersEngagedWithSharing: number, totalUsersEngagedSharingWithTutor: number, totalUsersHaveComments: number) {
    this.engagement = new Array<DataPoint>();
    this.engagement.push(new DataPoint("Engaged", totalEngagedUsers, EntryEngagementTypes.Entry_Engaged));
    this.engagement.push(new DataPoint("Non-Engaged", totalUsers - totalEngagedUsers, EntryEngagementTypes.Entry_NonEngaged));
    this.shared = new Array<DataPoint>();
    this.shared.push(new DataPoint("Engaged", totalUsersEngagedWithSharing, EntryEngagementTypes.SharedEntry_Engaged));
    this.shared.push(new DataPoint("Non-Engaged", totalUsers - totalUsersEngagedWithSharing, EntryEngagementTypes.SharedEntry_NonEngaged));
    this.sharedWithTutor = new Array<DataPoint>();
    this.sharedWithTutor.push(new DataPoint("Engaged", totalUsersEngagedSharingWithTutor, EntryEngagementTypes.TutorSharedEntry_Engaged));
    this.sharedWithTutor.push(new DataPoint("Non-Engaged", totalUsers - totalUsersEngagedSharingWithTutor, EntryEngagementTypes.TutorSharedEntry_NonEngaged));
    this.commented = new Array<DataPoint>();
    this.commented.push(new DataPoint("Engaged", totalUsersHaveComments, EntryEngagementTypes.CommentedEntry_Engaged));
    this.commented.push(new DataPoint("Non-Engaged", totalUsers - totalUsersHaveComments, EntryEngagementTypes.CommentedEntry_NonEngaged));
  }
}
class DataPoint {
  extra: EntryEngagementTypes; // This value is returned on click of the pie item, but not the legend :( https://github.com/swimlane/ngx-charts/blob/4d5e1767eae8397dfae79227609dc4969e7e54fa/src/common/legend/advanced-legend.component.ts#L39
  name: string | Date;
  value: number;
  constructor(name:string | Date, value: number, type: EntryEngagementTypes = null){
    this.name = name;
    this.value = value;
    this.extra = type;
  }
}
class EntryEngagementTimeLine {
  name: string;
  series: DataPoint[];
  constructor(name:string, series: DataPoint[]){
    this.name = name;
    this.series = series;
  }
}