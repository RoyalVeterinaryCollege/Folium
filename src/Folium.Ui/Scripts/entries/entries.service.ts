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
import { FormBuilder, FormGroup } from '@angular/forms';
import { URLSearchParams, Response, Http } from "@angular/http";
import { Observable } from 'rxjs/Observable';

import 'rxjs/add/operator/map';

import { Entry, SelfAssessment, Where, Skill, EntryType, EntrySummary } from "../dtos";
import { ResponseService } from "../common/response.service";
import { SkillSetSelectionService } from "../skill-set/selection.service"
import { SkillAssessmentService } from "../skills/skill-assessment.service";

@Injectable()
export class EntriesService {
    private entriesUrl = "entries";

    // Local cache.
	private entryTypes: EntryType[];

	constructor(
		private http: Http, 
		private responseService: ResponseService, 
		private skillAssessmentService: SkillAssessmentService,
		private skillSetSelectionService: SkillSetSelectionService) { }

	getEntry(entryId: string): Observable<Entry> {
		return this.http.get(`${this.entriesUrl}/${entryId}`)
			.map((res: Response) => this.responseService.parseJson(res));
	}
	
	getEntryTypes(skillSetId: number): Observable<EntryType[]> {
        // Return the cached value if available.
        return this.entryTypes
			? Observable.of(this.entryTypes)
			: this.http.get(`${this.entriesUrl}/types?skillSetId=${skillSetId}`)
				.map((res: Response) => this.responseService.parseJson(res));
    }

	getEntries(skillSetId: number, page: number, pageSize: number): Observable<EntrySummary[]> {
		return this.http.get(`${this.entriesUrl}?skillSetId=${skillSetId}&skip=${((page - 1) * pageSize)}&take=${pageSize}`)
			.map((res: Response) => this.responseService.parseJson(res));
	}

	createEntry(entry: Entry): Observable<Entry> {
		return this.http.post(this.entriesUrl + "/create", entry)
			.map((res: Response) => this.responseService.parseJson(res));
	}

	updateEntry(entry: Entry): Observable<Entry> {
		return this.http.post(`${this.entriesUrl}/${entry.id}/update`, entry)
			.do((response: Response) => {
				// If any self assessments were removed we will receive back the latest ones.
				let latestSelfAssessments: { [key: number]: SelfAssessment };
				latestSelfAssessments = this.responseService.parseJson(response);
				this.skillAssessmentService.updateSelfAssessments(this.skillSetSelectionService.skillSet.id, latestSelfAssessments);
			})
			.map((res: Response) => entry);
	}

	removeEntry(entryId: any): Observable<{ [key: number]: SelfAssessment }> {
		return this.http.post(`${this.entriesUrl}/${entryId}/remove`, {})
			.map((res: Response) => this.responseService.parseJson(res))			
			.do((latestSelfAssessments: { [key: number]: SelfAssessment }) => {
				// If any self assessments were removed we will receive back the latest ones.
				this.skillAssessmentService.updateSelfAssessments(this.skillSetSelectionService.skillSet.id, latestSelfAssessments);
			});
	}

	getUserPlaces(): Observable<Where[]> {
		return this.http.get(`${this.entriesUrl}/where`)
			.map((res: Response) => this.responseService.parseJson(res));
	}
}
