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

import { ReportOnOption, SelfAssessmentEngagementReportCriteria, SelfAssessmentEngagementReportResultSet, EntryEngagementReportCriteria, EntryEngagementReportResultSet, PlacementEngagementReportResultSet, PlacementEngagementReportCriteria } from "../core/dtos";

@Injectable()
export class ReportsService {
    private reportsUrl = "reports";

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

	sendMessage(toUserIds: number[], body: string): Observable<string> {
		return this.http.post<string>(`messaging/send-to-many`, { to: toUserIds, body });
	}
}
