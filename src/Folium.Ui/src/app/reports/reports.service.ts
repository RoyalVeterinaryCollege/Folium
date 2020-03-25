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
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";

import { Observable } from 'rxjs';

import { ReportOnOption, SelfAssessmentEngagementReportCriteria, SelfAssessmentEngagementReportResultSet, EntryEngagementReportCriteria, EntryEngagementReportResultSet, PlacementEngagementReportResultSet, PlacementEngagementReportCriteria, SelfAssessmentEngagementUser, EntryEngagementUser, PlacementEngagementUser, EntryType } from "../core/dtos";
import { publishReplay, refCount } from "rxjs/operators";

@Injectable()
export class ReportsService {
  private reportsUrl = "reports";

  // Local cache.
  private entryTypes: { [ids: string]: Observable<EntryType[]>; } = {};

	public colorScheme = {
	  domain: ['#607D8B', '#FF5722', '#CFD8DC', '#AAAAAA']
	};

	constructor(private http: HttpClient) { }

	getUserOptions(): Observable<ReportOnOption[]> {
		return this.http.get<ReportOnOption[]>(`${this.reportsUrl}/user-options`);
	}

	getSelfAssessmentEngagementReport(criteria: SelfAssessmentEngagementReportCriteria): Observable<SelfAssessmentEngagementReportResultSet> {
		return this.http.post<SelfAssessmentEngagementReportResultSet>(`${this.reportsUrl}/self-assessment-engagement`, criteria);
	}

	getSelfAssessmentEngagementMinDate(): Observable<Date> {
		return this.http.get<Date>(this.reportsUrl + "/self-assessment-engagement/min-date");
	}

	getEntryEngagementReport(criteria: EntryEngagementReportCriteria): Observable<EntryEngagementReportResultSet> {
		return this.http.post<EntryEngagementReportResultSet>(`${this.reportsUrl}/entry-engagement`, criteria);
	}

	getEntryEngagementMinDate(): Observable<Date> {
		return this.http.get<Date>(this.reportsUrl + "/entry-engagement/min-date");
	}

	getPlacementEngagementReport(criteria: PlacementEngagementReportCriteria): Observable<PlacementEngagementReportResultSet> {
		return this.http.post<PlacementEngagementReportResultSet>(`${this.reportsUrl}/placement-engagement`, criteria);
	}

	getPlacementEngagementMinDate(): Observable<Date> {
		return this.http.get<Date>(this.reportsUrl + "/placement-engagement/min-date");
	}

	getPlacementTypes(): Observable<string[]> {
		return this.http.get<string[]>(this.reportsUrl + "/placement-engagement/placement-types");
  }

  getEntryTypes(skillSetIds: number[]): Observable<EntryType[]>;
  getEntryTypes(skillSetId: number): Observable<EntryType[]>;
  getEntryTypes(value: number[] | number): Observable<EntryType[]> {
    let ids = [];
    if (typeof value === "number") {
      ids.push(value);
    }
    if (value instanceof Array) {
      ids = value.sort(function (a, b) {
        return a - b;
      });
    }
    let querystring = `?skillSetIds=${ids.join("&skillSetIds=")}`;
    let index = ids.join(",");
    // Return the cached value if available.
    if (!this.entryTypes[index]) {
      this.entryTypes[index] = this.http.get<EntryType[]>(`${this.reportsUrl}/entry-types${querystring}`).pipe(
        publishReplay(1),
        refCount()
      );
    }
    return this.entryTypes[index];
  }

	sendMessage(toUserIds: number[], body: string): Observable<string> {
		return this.http.post<string>(`messaging/send-to-many`, { to: toUserIds, body });
	}

	removeUserFieldsForCsvDownload(user): any {
		if(user.hasOwnProperty('engagement')) {
			delete user.engagement;
		}
		delete user.courses;
		delete user.hasTutees;
		delete user.hasTutor;
		delete user.lastSignIn;
		delete user.pic;
		delete user.hasProfilePic;
		delete user.profilePicVersion;
		if(!user.hasOwnProperty('totalComments')) {
			delete user.totalEntries; // Only delete this if this is not a EntryEngagementUser (i.e. has totalComments property)
		}
		delete user.totalEntriesSharedWithYou;
		delete user.totalPlacements;
		delete user.totalSelfAssessments;
		let tutors = user.tutors && user.tutors.length > 0 ? user.tutors.join(",") : "";
		delete user.tutors;
		user.tutors = tutors;
		return user;
	}
}
