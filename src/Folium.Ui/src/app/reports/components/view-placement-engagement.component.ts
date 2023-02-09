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

import { ScrollToService } from '@nicky-lenaers/ngx-scroll-to';

import { AngularCsv } from "angular-csv-ext/dist/Angular-csv";

import { User, ReportOnOption, PlacementEngagementUser, PlacementEngagementReportResultSet, PlacementEngagementReportCriteria } from "../../core/dtos";
import { ReportsService } from "../reports.service";
import { NotificationService } from "../../core/notification.service";
import { DialogMessageUsersComponent } from "./dialog-message-users.component";
import { Utils } from "src/app/core/utils";

enum PlacementEngagementTypes {
  Placement_Engaged = 'Placement Engaged',
  Placement_NonEngaged = 'Placement Non-Engaged',
  PlacementEntry_Engaged = 'Placement Entry Engaged',
  PlacementEntry_NonEngaged = 'Placement Entry Non-Engaged',
  SharedPlacementEntry_Engaged = 'Shared Placement Entry Engaged',
  SharedPlacementEntry_NonEngaged = 'Shared Placement Entry Non Engagued',
  TutorSharedPlacementEntry_Engaged = 'Tutor Shared Placement Entry Engaged',
  TutorSharedPlacementEntry_NonEngaged = 'Tutor Shared Placement Entry Non Engagued',
  AllEntriesRequestedSignOff_Engaged = 'All Entries Requested sign-off Engaged',
  AllEntriesRequestedSignOff_NonEngaged = 'All Entries Requested sign-off Non Engaged',
  AllEntriesSignedOff_Engaged = 'All Entries Signed Off Engaged',
  AllEntriesSignedOff_NonEngaged = 'All Entries Signed Off Non Engaged'
}

@Component({
  templateUrl: "view-placement-engagement.component.html",
})
export class ViewPlacementEngagementComponent implements OnInit {
  currentUser: User;
	reportForm: FormGroup;
  reportOnOptions: ReportOnOption[] = [];  
  filteredReportOnOptions: Observable<any[]>;
  filteredReportOnOptionsLength: number;
  maxOptionsToDisplay = 50;
  minDate: Date;
  touchUi = false; // Used for the mat-datepicker to load in a mobile friendly format.
  resultSet: PlacementEngagementReportResultSet;
  resultSummary: PlacementEngagementSummary;
	fetchingReportData: boolean = false;
  reportsService: ReportsService;
  userList: MatTableDataSource<PlacementEngagementUser>;
  placementEngagementTypes = PlacementEngagementTypes; // Used to reference enum from the template. 
  userToView: number; // The user to view in more detail.
  placementTypes: string[];
  allPlacementType = "All";

	@ViewChild("reportOnInput", { static: true })
  reportOnInput: ElementRef;
  
	@ViewChild("skillSetInput", { static: false })
  skillSetsInput: ElementRef;

	@ViewChild("entryTypesInput", { static: false })
  entryTypesInput: ElementRef;

	@ViewChild("userListTable", { static: false })
  userListTable: MatTable<PlacementEngagementUser>;
  
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
      type: null,
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
    
    this.reportsService.getPlacementEngagementMinDate().subscribe(result => {
      this.minDate = result;
    });

    this.reportsService.getPlacementTypes().subscribe(result => {
      this.placementTypes = result.sort((a, b) => ('' + a).localeCompare(b)).filter(v => v);
    });
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
    
  onGenerateReportClick() {
    const formValues = this.reportForm.value;

    let criteria = this.extractCriteriaFromForm(formValues);

    this.resultSet = undefined;
    this.resultSummary = undefined;
    this.fetchingReportData = true;

    this.reportsService.getPlacementEngagementReport(criteria)
		  .subscribe((results: PlacementEngagementReportResultSet) => {
        this.resultSet = results
        this.sortUsersBySurname();
        this.resultSummary = this.processResults();
        this.userList = new MatTableDataSource(this.resultSet.users);
        this.fetchingReportData = false;
        this.scrollToService.scrollTo({
          target: 'users-container',
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
        case 'placement': {
          this.userList.data = (data.name === "Yes")
            ? this.resultSet.users.filter(u => u.placements > 0)
            : this.resultSet.users.filter(u => !u.placements || u.placements === 0);
          this.resultSummary.activeEngagementType = (data.name === "Yes")
              ? PlacementEngagementTypes.Placement_Engaged
              : PlacementEngagementTypes.Placement_NonEngaged;
          break;
        }
        case 'placement_entry': {
          this.userList.data = (data.name === "Yes")
            ? this.resultSet.users.filter(u => u.placementsWithEntries > 0) 
            : this.resultSet.users.filter(u => !u.placementsWithEntries || u.placementsWithEntries === 0);
          this.resultSummary.activeEngagementType = (data.name === "Yes")
            ? PlacementEngagementTypes.PlacementEntry_Engaged
            : PlacementEngagementTypes.PlacementEntry_NonEngaged;
          break;
        }
        case 'shared_placement_entry': {
          this.userList.data = (data.name === "Yes")
            ? this.resultSet.users.filter(u => u.placementsWithSharedEntries > 0)
            : this.resultSet.users.filter(u => !u.placementsWithSharedEntries || u.placementsWithSharedEntries === 0);
          this.resultSummary.activeEngagementType = (data.name === "Yes")
              ? PlacementEngagementTypes.SharedPlacementEntry_Engaged
              : PlacementEngagementTypes.SharedPlacementEntry_NonEngaged;
          break;
        }
        case 'tutor_shared_placement_entry': {
          this.userList.data = (data.name === "Yes")
            ? this.resultSet.users.filter(u => u.placementsWithTutorSharedEntries > 0)
            : this.resultSet.users.filter(u => !u.placementsWithTutorSharedEntries || u.placementsWithTutorSharedEntries === 0);
          this.resultSummary.activeEngagementType = (data.name === "Yes")
              ? PlacementEngagementTypes.TutorSharedPlacementEntry_Engaged
              : PlacementEngagementTypes.TutorSharedPlacementEntry_NonEngaged;
          break;
        }
        case 'placement_requested_sign_off_entries': {
          this.userList.data = (data.name === "Yes")
            ? this.resultSet.users.filter(u => u.placementsWithAllEntriesRequestedSignOff > 0)
            : this.resultSet.users.filter(u => !u.placementsWithAllEntriesRequestedSignOff || u.placementsWithAllEntriesRequestedSignOff === 0);
          this.resultSummary.activeEngagementType = (data.name === "Yes")
            ? PlacementEngagementTypes.AllEntriesRequestedSignOff_Engaged
            : PlacementEngagementTypes.AllEntriesRequestedSignOff_NonEngaged;
          break;
        }
        case 'placement_signed_off_entries': {
          this.userList.data = (data.name === "Yes")
            ? this.resultSet.users.filter(u => u.placementsWithAllEntriesSignedOff > 0)
            : this.resultSet.users.filter(u => !u.placementsWithAllEntriesSignedOff || u.placementsWithAllEntriesSignedOff === 0);
          this.resultSummary.activeEngagementType = (data.name === "Yes")
            ? PlacementEngagementTypes.AllEntriesSignedOff_Engaged
            : PlacementEngagementTypes.AllEntriesSignedOff_NonEngaged;
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
    const options = { 
      showLabels: true, 
      headers: ['id', 'Email', 'First Name', 'Surname', 'Placements', 'With Entries', 'Can be signed off', 'All Entries requested for sign-off', 'All Entries signed off', 'Shared', 'Shared with Tutor', 'Tutors']
    };

    const reportUsers: object[] = [];
    this.userList.data.forEach((user: PlacementEngagementUser) => {
      const reportUser = {
        id: user.id,
        email: user.email,
        firstName: user.firstName,
        surname: user.lastName,
        placements: user.placements,
        withEntries: user.placementsWithEntries,
        canBeSignOff: user.placementsWithEntriesCanBeSignOff,
        requestedSignOff: user.placementsWithAllEntriesRequestedSignOff,
        signedOff: user.placementsWithAllEntriesSignedOff,
        shared: user.placementsWithSharedEntries,
        sharedWithTutor: user.placementsWithTutorSharedEntries,
        tutors: user.tutors && user.tutors.length > 0 ? user.tutors.join(",") : ""
      };
      reportUsers.push(reportUser);
    });
        
    new AngularCsv(reportUsers, 'Placement Engagement', options);
  }

	private canAddToReportOnList(reportOn: ReportOnOption): boolean {
    let who = this.reportForm.value.who;
		return who ? !who.find(w => w.name === reportOn.name) : true;
  }

  private extractCriteriaFromForm(formValues: any): PlacementEngagementReportCriteria {
		// Transfer from the form.
    const criteria = new PlacementEngagementReportCriteria();
    criteria.who = formValues.who;
    criteria.type = formValues.type;
    if(criteria.type === this.allPlacementType) {
      criteria.type = null; // Reset the type back to null as we just use the SelfCreatedPlacementType string for the UI.
    }
    criteria.from = formValues.from;
    criteria.to = formValues.to;

		return criteria;
  }

  private processResults(): PlacementEngagementSummary {
    let userIndex: { [id:number] : PlacementEngagementUser; } = {}; // Index of the Users.
    let engagedUsers = new Set<number>(); // Used as a quick lookup for users who have least 1 placement.
    
    // Process each data item.
    this.resultSet.dataSet.forEach(item => {
      if(!engagedUsers.has(item.userId)) {
        // Insert the user into the userIndex.
        userIndex[item.userId] = this.resultSet.users.find(u => u.id === item.userId);
        // Add the user to the set of engaged users.
        engagedUsers.add(item.userId);
      } 
      // Record the stats for the user.
      let user = userIndex[item.userId];
      user.placements = (user.placements ? user.placements : 0) + 1;
      user.placementsWithEntries = (user.placementsWithEntries ? user.placementsWithEntries : 0) + (item.entryCount > 0 ? 1 : 0);
      user.placementsWithEntriesCanBeSignOff = (user.placementsWithEntriesCanBeSignOff ? user.placementsWithEntriesCanBeSignOff : 0) + item.entrySignOffCompatibleCount > 0 ? 1 : 0;
      user.placementsWithAllEntriesRequestedSignOff = (user.placementsWithAllEntriesRequestedSignOff ? user.placementsWithAllEntriesRequestedSignOff : 0) + ((item.entrySignOffCompatibleCount > 0 && (item.entrySignOffCompatibleCount == item.entrySignOffRequestCount)) ? 1 : 0);
      user.placementsWithAllEntriesSignedOff = (user.placementsWithAllEntriesSignedOff ? user.placementsWithAllEntriesSignedOff : 0) + ((item.entrySignOffCompatibleCount > 0 && (item.entrySignOffCompatibleCount == item.entrySignedOffCount)) ? 1 : 0);
      user.placementsWithSharedEntries = (user.placementsWithSharedEntries ? user.placementsWithSharedEntries : 0) + (item.sharedEntryCount > 0 ? 1 : 0);
      user.placementsWithTutorSharedEntries = (user.placementsWithTutorSharedEntries ? user.placementsWithTutorSharedEntries : 0) + (item.sharedEntryWithTutorCount > 0 ? 1 : 0);
    });

    let totalUsersEngagedWithPlacementEntries = 0;
    let totalUsersEngagedWithSharing = 0;
    let totalUsersEngagedSharingWithTutor = 0;
    let totalUsersAllEntriesRequestedSignOff = 0;
    let totalUsersAllEntriesSignedOff = 0;
    this.resultSet.users.forEach(u => {
      // Set any undefined fields to 0.
      u.placements = u.placements ? u.placements : 0; 
      u.placementsWithEntries = u.placementsWithEntries ? u.placementsWithEntries : 0; 
      u.placementsWithSharedEntries = u.placementsWithSharedEntries ? u.placementsWithSharedEntries : 0;
      u.placementsWithTutorSharedEntries = u.placementsWithTutorSharedEntries ? u.placementsWithTutorSharedEntries : 0;
      u.placementsWithEntriesCanBeSignOff = u.placementsWithEntriesCanBeSignOff ? u.placementsWithEntriesCanBeSignOff : 0;
      u.placementsWithAllEntriesRequestedSignOff = u.placementsWithAllEntriesRequestedSignOff ? u.placementsWithAllEntriesRequestedSignOff : 0;
      u.placementsWithAllEntriesSignedOff = u.placementsWithAllEntriesSignedOff ? u.placementsWithAllEntriesSignedOff : 0;
      totalUsersEngagedWithPlacementEntries = totalUsersEngagedWithPlacementEntries + (u.placementsWithEntries > 0 ? 1 : 0);
      totalUsersEngagedWithSharing = totalUsersEngagedWithSharing + (u.placementsWithSharedEntries > 0 ? 1 : 0);
      totalUsersEngagedSharingWithTutor = totalUsersEngagedSharingWithTutor + (u.placementsWithTutorSharedEntries > 0 ? 1 : 0);
      totalUsersAllEntriesRequestedSignOff = totalUsersAllEntriesRequestedSignOff + ((u.placementsWithEntriesCanBeSignOff > 0 && (u.placementsWithEntriesCanBeSignOff == u.placementsWithAllEntriesRequestedSignOff)) ? 1 : 0);
      totalUsersAllEntriesSignedOff = totalUsersAllEntriesSignedOff + ((u.placementsWithEntriesCanBeSignOff > 0 && (u.placementsWithEntriesCanBeSignOff == u.placementsWithAllEntriesSignedOff)) ? 1 : 0);
    });

    return new PlacementEngagementSummary(this.resultSet.users.length, engagedUsers.size, totalUsersEngagedWithPlacementEntries, totalUsersEngagedWithSharing, totalUsersEngagedSharingWithTutor, totalUsersAllEntriesRequestedSignOff, totalUsersAllEntriesSignedOff);
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

class PlacementEngagementSummary {
  placements: DataPoint[];
  shared: DataPoint[];
  sharedWithTutor: DataPoint[];
  placementEntry: DataPoint[];
  requestSignOffEntries: DataPoint[];
  signedOffEntries: DataPoint[];
  public activeEngagementType: PlacementEngagementTypes;

  constructor(totalUsers: number, totalUsersEngagedWithPlacements: number, totalUsersEngagedWithPlacementEntries: number, totalUsersEngagedWithSharing: number, totalUsersEngagedSharingWithTutor: number, totalUsersAllEntriesRequestedSignOff: number, totalUsersAllEntriesSignedOff: number) {
    this.placements = new Array<DataPoint>();
    this.placements.push(new DataPoint("Yes", totalUsersEngagedWithPlacements, PlacementEngagementTypes.Placement_Engaged));
    this.placements.push(new DataPoint("No", totalUsers - totalUsersEngagedWithPlacements, PlacementEngagementTypes.Placement_NonEngaged));
    this.placementEntry = new Array<DataPoint>();
    this.placementEntry.push(new DataPoint("Yes", totalUsersEngagedWithPlacementEntries, PlacementEngagementTypes.PlacementEntry_Engaged));
    this.placementEntry.push(new DataPoint("No", totalUsers - totalUsersEngagedWithPlacementEntries, PlacementEngagementTypes.PlacementEntry_NonEngaged));
    this.shared = new Array<DataPoint>();
    this.shared.push(new DataPoint("Yes", totalUsersEngagedWithSharing, PlacementEngagementTypes.SharedPlacementEntry_Engaged));
    this.shared.push(new DataPoint("No", totalUsers - totalUsersEngagedWithSharing, PlacementEngagementTypes.SharedPlacementEntry_NonEngaged));
    this.sharedWithTutor = new Array<DataPoint>();
    this.sharedWithTutor.push(new DataPoint("Yes", totalUsersEngagedSharingWithTutor, PlacementEngagementTypes.TutorSharedPlacementEntry_Engaged));
    this.sharedWithTutor.push(new DataPoint("No", totalUsers - totalUsersEngagedSharingWithTutor, PlacementEngagementTypes.TutorSharedPlacementEntry_NonEngaged));
    this.requestSignOffEntries = new Array<DataPoint>();
    this.requestSignOffEntries.push(new DataPoint("Yes", totalUsersAllEntriesRequestedSignOff, PlacementEngagementTypes.AllEntriesRequestedSignOff_Engaged));
    this.requestSignOffEntries.push(new DataPoint("No", totalUsers - totalUsersAllEntriesRequestedSignOff, PlacementEngagementTypes.AllEntriesRequestedSignOff_NonEngaged));
    this.signedOffEntries = new Array<DataPoint>();
    this.signedOffEntries.push(new DataPoint("Yes", totalUsersAllEntriesSignedOff, PlacementEngagementTypes.AllEntriesSignedOff_Engaged));
    this.signedOffEntries.push(new DataPoint("No", totalUsers - totalUsersAllEntriesSignedOff, PlacementEngagementTypes.AllEntriesSignedOff_NonEngaged));
  }
}
class DataPoint {
  extra: PlacementEngagementTypes; // This value is returned on click of the pie item, but not the legend :( https://github.com/swimlane/ngx-charts/blob/4d5e1767eae8397dfae79227609dc4969e7e54fa/src/common/legend/advanced-legend.component.ts#L39
  name: string | Date;
  value: number;
  constructor(name:string | Date, value: number, type: PlacementEngagementTypes = null){
    this.name = name;
    this.value = value;
    this.extra = type;
  }
}
