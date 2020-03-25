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
import { MatAutocompleteSelectedEvent } from "@angular/material/autocomplete";
import { MatDatepicker } from "@angular/material/datepicker";
import { MatDialog } from "@angular/material/dialog";
import { MatPaginator } from "@angular/material/paginator";
import { MatSort } from "@angular/material/sort";
import { MatTable, MatTableDataSource } from "@angular/material/table";

import { Observable } from "rxjs";
import { startWith, map } from "rxjs/operators";

import { AngularCsv } from "angular-csv-ext/dist/Angular-csv";

import { User, ReportOnOption, SkillSet, SelfAssessmentEngagementReportCriteria, SelfAssessmentEngagementReportResultSet, SelfAssessmentEngagementReportResult, SelfAssessmentEngagementUser, SkillGrouping, SkillGroup, SelfAssessment } from "../../core/dtos";
import { ReportsService } from "../reports.service";
import { SkillService } from "../../skills/skill.service";
import { NotificationService } from "../../core/notification.service";
import { Utils } from "../../core/utils";
import { SkillAssessmentService } from "../../skills/skill-assessment.service";
import { DialogChangeSkillGroupingComponent } from "../../skills/components/dialog-change-skill-grouping.component";
import { ScrollToService } from "@nicky-lenaers/ngx-scroll-to";
import { DialogMessageUsersComponent } from "./dialog-message-users.component";

@Component({
  templateUrl: "view-self-assessment-engagement.component.html",
})
export class ViewSelfAssessmentEngagementComponent implements OnInit {
  currentUser: User;
	reportForm: FormGroup;
  reportOnOptions: ReportOnOption[] = [];  
  filteredReportOnOptions: Observable<any[]>;
  filteredReportOnOptionsLength: number;
  filteredSkillSets: Observable<any[]>;
  filteredSkillSetsLength: number;
  maxOptionsToDisplay = 50;
  minDate: Date;
  skillSets: SkillSet[];
  touchUi = false; // Used for the mat-datepicker to load in a mobile friendly format.
  resultSet: SelfAssessmentEngagementReportResultSet;
  resultSummary: SelfAssessmentEngagementSummary;
  resultTimelines: SelfAssessmentEngagementTimeLine[];
	fetchingReportData: boolean = false;
	skillGroupings: SkillGrouping[];
	skillGrouping: SkillGrouping;
  skillGroups: SkillGroup[];
  reportsService: ReportsService;
  userList: MatTableDataSource<SelfAssessmentEngagementUser>;
  userToView: number; // The user to view in more detail.
  
	@ViewChild("reportOnInput", { static: true })
  reportOnInput: ElementRef;
  
	@ViewChild("skillSetInput", { static: true })
  skillSetsInput: ElementRef;

	@ViewChild("userListTable", { static: false })
  userListTable: MatTable<SelfAssessmentEngagementUser>;
  
	@ViewChild("scrollToUserList", { static: false })
  scrollToUserList: ElementRef;
  
  private userListPaginator: MatPaginator;
  
  @ViewChild(MatSort, { static: false }) 
  set matSort(matSort: MatSort) {
    if(this.userList) {
      this.userList.sort = matSort;
    }
  }

  @ViewChild(MatPaginator, { static: false }) 
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
    private skillService: SkillService,
    private skillAssessmentService: SkillAssessmentService,
    private formBuilder: FormBuilder,
    private notificationService: NotificationService,
		private dialog: MatDialog,
    private scrollToService: ScrollToService) { 
      this.reportsService = reportsService;
    }
  
  ngOnInit() {
    this.route.data.forEach((data: { currentUser: User }) => {
      this.currentUser = data.currentUser;
    });

    this.reportForm = this.formBuilder.group({
      who: [new Array<ReportOnOption>(), Validators.required],
      whoQuery: ["", Validators.required],
      skillSet: [undefined as SkillSet, Validators.required],
      skillSetQuery: ["", Validators.required],
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
    
    this.reportsService.getSelfAssessmentEngagementMinDate().subscribe(result => {
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
      skillSet: skillSet
    });
    this.skillSetsInput.nativeElement.value = '';
  }
    
	onRemoveSkillSetClick() {
    this.reportForm.patchValue({
      skillSet: undefined as SkillSet
    });
  }

  onGenerateReportClick() {
    const formValues = this.reportForm.value;

    let criteria = this.extractCriteriaFromForm(formValues);

    this.resultSet = undefined;
    this.resultSummary = undefined;
    this.resultTimelines = undefined;
		this.skillGroups = undefined;
    this.fetchingReportData = true;

    this.reportsService.getSelfAssessmentEngagementReport(criteria)
		  .subscribe((results: SelfAssessmentEngagementReportResultSet) => {
        this.resultSet = results
        let processedResults = this.processResults(results.dataSet, true);
        this.resultSummary = processedResults[0];
        this.resultTimelines = processedResults[1];
        this.loadSkillGroups();
        this.userList = new MatTableDataSource(this.resultSummary.usersEngagement);
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

	openPicker(picker:MatDatepicker<Date>) {
		if(!this.touchUi) picker.open();
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
      headers: ['id', 'Email', 'First Name', 'Surname', 'Average Assessment', 'Tutors']
    };
    let users = Utils.deepClone(this.userList.data) as SelfAssessmentEngagementUser[];
    users.forEach((user:SelfAssessmentEngagementUser) => {
      user.averageAssessment = user.averageAssessment ? user.averageAssessment : 0;
      user = this.reportsService.removeUserFieldsForCsvDownload(user);
    });
    
    new AngularCsv(users, 'Self Assessment Engagement', options);
  }

	private canAddToReportOnList(reportOn: ReportOnOption): boolean {
    let who = this.reportForm.value.who;
		return who ? !this.reportForm.value.who.find(w => w.name === reportOn.name) : true;
  }
  
  private extractCriteriaFromForm(formValues: any): SelfAssessmentEngagementReportCriteria {
		// Transfer from the form.
    const criteria = new SelfAssessmentEngagementReportCriteria();
    criteria.who = formValues.who;
    criteria.skillSetId = formValues.skillSet.id;
    criteria.from = formValues.from;
    criteria.to = formValues.to;

		return criteria;
  }

  private processResults(data: SelfAssessmentEngagementReportResult[], processTimelines: boolean): [SelfAssessmentEngagementSummary, SelfAssessmentEngagementTimeLine[]] {
    let users = Utils.deepClone(this.resultSet.users) as SelfAssessmentEngagementUser[];
    let userIndex: { [id:number] : SelfAssessmentEngagementUser; } = {}; // Index of the Users.
    let engagedUsers = new Set<number>(); // Used as a quick lookup for users who have already done at least 1 assessment.
    let assessmentLevels: number[] = []; // All assessments, used to calculate the average.
    let engagementByDate: { [date:string] : number[]; } = { }; // The users who have done at least 1 assessment by date.
    let minEngagementDateByUser: { [userId:number] : Date; } = {}; // Track the min engagement dates, as the ordering of engagment is not garenteed to be in date order.
    let cumulativeEngagementByDate: { [date:string] : number; } = { }; // The cumulative count of users who have done at least 1 assessment by date.
    let timelines:SelfAssessmentEngagementTimeLine[] = []
    
    // Order the users by lastname.
    users.sort((a,b) => {
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

    data.forEach(item => {
      if(processTimelines) {
        // We need to process the timeline data too.
        if(!engagedUsers.has(item.userId)) {
          // This user hasn't been seen before, so this is a new engagement which needs to be added.
          minEngagementDateByUser[item.userId] = item.date;
        } else {
          let engagementDate = minEngagementDateByUser[item.userId];
          // If this date is before the one we have, then overwrite it.
          if(engagementDate > item.date) {
            minEngagementDateByUser[item.userId] = item.date;
          }
        }
        let engagementOnThisDate = engagementByDate[item.date.toDateString()];
        // Check if there has already been engagment on this date, and either increment or set to 1.
        if(engagementOnThisDate) {
          // Check they haven't already engaged on this day.
          if(engagementOnThisDate.indexOf(item.userId) === -1) {
            engagementOnThisDate.push(item.userId);
          }
        } else {
          engagementByDate[item.date.toDateString()] = [item.userId];
        }
      }
      if(!engagedUsers.has(item.userId)){
        // We haven't seen this user before, so insert the user into the userIndex.
        userIndex[item.userId] = users.find(u => u.id === item.userId);
        userIndex[item.userId].engagement = new Array<SelfAssessment>(); // Initialise the list of their engagements.
      }  
      // Add the score to the list.
      assessmentLevels.push(item.score);
      // Add this engagement to the users list.
      userIndex[item.userId].engagement.push(new SelfAssessment(undefined /* not required */, item.skillId, item.score, item.date));

      if(!engagedUsers.has(item.userId)){
        // Add the user to the set of engaged users, we should do this last, once all other processing is complete for this item.
        engagedUsers.add(item.userId);
      }     
    });
    let averageAssessmentLevel = assessmentLevels.length === 0 ? 0 : assessmentLevels.reduce((sum, val) => sum + val, 0) / assessmentLevels.length;
    if(processTimelines) {
      let allDates = Utils.getDateArray(
         this.resultSet.criteria.from ? this.resultSet.criteria.from : this.minDate, 
         this.resultSet.criteria.to ? this.resultSet.criteria.to : new Date());
      // If we are processing timelines, then transfer the collected data into the correct shape.
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
      timelines.push(new SelfAssessmentEngagementTimeLine("Cumulative Engagment", cumulativeEngagementByDateDataPoints))

      let engagementByDateDataPoints = new Array<DataPoint>();
      // Need to have an entry for every date.
      allDates.forEach(date => {
        let value = engagementByDate[date.toDateString()];
        engagementByDateDataPoints.push(new DataPoint(date, value ? value.length : 0));
      });
      timelines.push(new SelfAssessmentEngagementTimeLine("Engagment", engagementByDateDataPoints))
    }

    // Loop the users, setting their average assessments score.
    users.forEach(user => {
      if(user.engagement) {
        user.averageAssessment = this.getAverageAssessment(user);
      }
    });
    return [new SelfAssessmentEngagementSummary(users.length, engagedUsers.size, averageAssessmentLevel, users), timelines];
  }
  
  private loadSkillGroups() {
		this.skillGroups = undefined;
		this.skillService.getSkillGroupings(this.resultSet.criteria.skillSetId)
			.subscribe(skillGroupings => {
				this.skillGroupings = skillGroupings;
				// Can't do anything if we don't have any skill groupings.
				if(skillGroupings.length === 0) return;
		
				// Get the selected grouping.
				this.skillGrouping = this.skillService.selectedSkillGroupings[this.resultSet.criteria.skillSetId];
				if(!this.skillGrouping) {
					this.skillGrouping = this.skillService.getDefaultSkillGrouping(skillGroupings);
					this.skillService.selectedSkillGroupings[this.resultSet.criteria.skillSetId] = this.skillGrouping;
				}
				this.skillService.getSkillGroups(this.resultSet.criteria.skillSetId, this.skillGrouping.id)
				.subscribe(skillGroups => {
				  this.skillAssessmentService.setSkillAssessmentsForSkillGroups(skillGroups, this.resultSummary.usersEngagement.map(u => u.engagement).reduce((acc, e) => acc.concat(e), []));
          this.skillGroups = skillGroups;
        },
				(error: any) => this.notificationService.addDanger(`There was an error trying to load the skill groups, please try again.
					${error}`)); 
			},
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the skill groupings, please try again.
        ${error}`));    
  }
  
	onChangeSkillGroupingClick() {		
		this.dialog.open(DialogChangeSkillGroupingComponent, {
		  data: {skillGroupings: this.skillGroupings, selectedSkillGrouping: this.skillGrouping}
		}).afterClosed().subscribe(skillGrouping => {
		  if(skillGrouping && this.skillGrouping.id !== skillGrouping.id) {
				this.skillGrouping = skillGrouping;				
				this.skillService.selectedSkillGroupings[this.resultSet.criteria.skillSetId] = this.skillGrouping;
				this.loadSkillGroups();
		  }
		});
  }
  
  onUserClick(userId: number, modal: any) {
    this.userToView = userId;
    modal.show()
  }

  onPieChartClick(data: {name: string, value: number} | HTMLElement, summary: SelfAssessmentEngagementSummary) {
    if (data instanceof HTMLElement) {
      // We want to catch when the total value legend is clicked on, as we don't have an event for it ootb.
      if(data.className && 
        (data.className.indexOf("total-value") >= 0 || data.className.indexOf("total-label") >= 0)) {
        this.userList.data = summary.usersEngagement;
        this.resultSummary.activeEngagementType = null;
      }
    } else {
      switch(data.name) {
        case "Engaged": {
          this.userList.data = summary.usersEngagement.filter(u => u.engagement);
          break;
        }
        case "Non-Engaged": {
          this.userList.data = summary.usersEngagement.filter(u => !u.engagement);
          break;
        }
      }
      this.resultSummary.activeEngagementType = data.name;
    }   
    
    this.userToView = null;
    this.userListPaginator.firstPage();
    this.userListTable.renderRows();
    // We can't use the scroll service here as it doesn't seem to work!
    this.scrollToUserList.nativeElement.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  getAverageAssessment(user: SelfAssessmentEngagementUser) {
    return user.engagement ? (user.engagement.map(e => e.score).reduce((s,v) => s+v) / user.engagement.length) : undefined;
  }
}

class SelfAssessmentEngagementSummary {
  engagement: DataPoint[];
  averageAssessmentLevel: number;
  usersEngagement: SelfAssessmentEngagementUser[];
  activeEngagementType: string;

  constructor(totalUsers: number, totalEngagedUsers: number, averageAssessmentLevel: number, usersEngagement: SelfAssessmentEngagementUser[]) {
    this.engagement = new Array<DataPoint>();
    this.engagement.push(new DataPoint("Engaged", totalEngagedUsers));
    this.engagement.push(new DataPoint("Non-Engaged", totalUsers - totalEngagedUsers));
    this.averageAssessmentLevel = averageAssessmentLevel;
    this.usersEngagement = usersEngagement;
  }
}
class DataPoint {
  name: string | Date;
  value: number;
  constructor(name:string | Date, value: number){
    this.name = name;
    this.value = value;
  }
}
class SelfAssessmentEngagementTimeLine {
  name: string;
  series: DataPoint[];
  constructor(name:string, series: DataPoint[]){
    this.name = name;
    this.series = series;
  }
}
