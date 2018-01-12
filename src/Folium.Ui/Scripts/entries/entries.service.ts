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
import { Response, Http } from "@angular/http";
import { Observable } from 'rxjs/Observable';

import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/observable/of';

import { Entry, SelfAssessment, Where, Skill, EntryType, EntrySummary, ShareEntryDto, EntryCommentDto, User, SelfAssessments } from "../dtos";
import { ResponseService } from "../common/response.service";
import { SkillAssessmentService } from "../skills/skill-assessment.service";

@Injectable()
export class EntriesService {
    private entriesUrl = "entries";

    // Local cache.
    private entryTypes: { [ids: string]: Observable<EntryType[]>; } = {};

	constructor(
		private http: Http, 
		private responseService: ResponseService, 
		private skillAssessmentService: SkillAssessmentService) { }

	getEntry(entryId: string): Observable<Entry> {
		return this.http.get(`${this.entriesUrl}/${entryId}`)
			.map((res: Response) => this.responseService.parseJson(res));
	}

	getEntrySummary(entryId: string): Observable<EntrySummary> {
		return this.http.get(`${this.entriesUrl}/${entryId}/summary`)
			.map((res: Response) => this.responseService.parseJson(res));
	}
	
	getEntryTypes(skillSetIds: number[]): Observable<EntryType[]> {
		let ids = skillSetIds.sort(function(a, b) {
			return a - b;
		});		
		let querystring = `?skillSetIds=${ids.join("&skillSetIds=")}`;
		let index = ids.join(",");
        // Return the cached value if available.
        if (!this.entryTypes[index]) {
			this.entryTypes[index] = this.http.get(`${this.entriesUrl}/types${querystring}`)
				.map((res: Response) => this.responseService.parseJson(res))
				.publishReplay(1)
				.refCount();
		}
		return this.entryTypes[index];
    }

	getEntries(userId: number, page: number, pageSize: number): Observable<EntrySummary[]> {
		return this.http.get(`${this.entriesUrl}?skip=${((page - 1) * pageSize)}&take=${pageSize}&userId=${userId}`)
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
				let latestSelfAssessments: SelfAssessments;
				latestSelfAssessments = this.responseService.parseJson(response);
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, latestSelfAssessments);
				// Update the self assessments with the updated bundle.
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, entry.assessmentBundle);
			})
			.map((res: Response) => entry);
	}

	removeEntry(entry: EntrySummary): Observable<{ [key: number]: SelfAssessment }> {
		return this.http.post(`${this.entriesUrl}/${entry.id}/remove`, {})
			.map((res: Response) => this.responseService.parseJson(res))			
			.do((latestSelfAssessments: SelfAssessments) => {
				// Remove the self assessments first before updating as we need to remove any from the cache.
				let selfAssessmentsToRemove: SelfAssessments = {};
				Object.keys(latestSelfAssessments).forEach(skillId => {
					selfAssessmentsToRemove[skillId] = null;
				});
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, selfAssessmentsToRemove);
				// If any self assessments were removed we will receive back the latest ones.
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, latestSelfAssessments);
			});
	}

	shareEntry(shareEntryDto: ShareEntryDto): Observable<Response> {
		return this.http.post(`${this.entriesUrl}/${shareEntryDto.entryId}/share`, shareEntryDto);
	}

	removeCollaborator(entryId: any, userId: number): Observable<Response> {
		return this.http.post(`${this.entriesUrl}/${entryId}/collaborators/${userId}/remove`, {});
	}

	getCollaborators(entryId: any): Observable<User[]> {
		return this.http.get(`${this.entriesUrl}/${entryId}/collaborators`)
				.map((res: Response) => this.responseService.parseJson(res));
	}

	comment(entryCommentDto: EntryCommentDto): Observable<EntryCommentDto> {
		return this.http.post(`${this.entriesUrl}/${entryCommentDto.entryId}/comment`, entryCommentDto)
			.map((res: Response) => {
				let commentId = this.responseService.parseJson(res);
				entryCommentDto.id = +commentId;
				return entryCommentDto;
			});
	}

	getUserPlaces(): Observable<Where[]> {
		return this.http.get(`${this.entriesUrl}/where`)
			.map((res: Response) => this.responseService.parseJson(res));
	}
}
