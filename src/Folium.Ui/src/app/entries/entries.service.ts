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
import { publishReplay, refCount, tap, map } from "rxjs/operators";

import { Entry, SelfAssessment, Where, EntryType, EntrySummary, ShareEntry, EntryComment, User, SelfAssessments, EntryFile, EntrySignOffRequest } from "../core/dtos";
import { SkillAssessmentService } from "../skills/skill-assessment.service";
import { EntryFilter } from "./entry-filters";

@Injectable()
export class EntriesService {
    private entriesUrl = "entries";

    // Local cache.
    private entryTypes: { [ids: string]: Observable<EntryType[]>; } = {};

	constructor(
		private http: HttpClient, 
		private skillAssessmentService: SkillAssessmentService) { }

	getEntry(entryId: string): Observable<Entry> {
		return this.http.get<Entry>(`${this.entriesUrl}/${entryId}`);
	}

	getEntrySummary(entryId: string): Observable<EntrySummary> {
		return this.http.get<EntrySummary>(`${this.entriesUrl}/${entryId}/summary`);
	}
	
	getEntryTypes(skillSetIds: number[]): Observable<EntryType[]>;
	getEntryTypes(skillSetId: number): Observable<EntryType[]>;
	getEntryTypes(value: number[] | number): Observable<EntryType[]> {
		let ids = [];
		if(typeof value === "number") {
			ids.push(value);
		}
		if(value instanceof Array) {
			ids = value.sort(function(a, b) {
				return a - b;
			});
		}
		let querystring = `?skillSetIds=${ids.join("&skillSetIds=")}`;
		let index = ids.join(",");
        // Return the cached value if available.
        if (!this.entryTypes[index]) {
			this.entryTypes[index] = this.http.get<EntryType[]>(`${this.entriesUrl}/types${querystring}`).pipe(
				publishReplay(1),
				refCount()
			);
		}
		return this.entryTypes[index];
    }

  getEntries(page: number, pageSize: number): Observable<EntrySummary[]> {
    return this.http.get<EntrySummary[]>(`${this.entriesUrl}?skip=${((page - 1) * pageSize)}&take=${pageSize}`);
  }
	getMyEntries(page: number, pageSize: number, filter: EntryFilter): Observable<EntrySummary[]> {
    const filterQuery = filter ? `&filter=${filter}` : '';
    return this.http.get<EntrySummary[]>(`${this.entriesUrl}/my?skip=${((page - 1) * pageSize)}&take=${pageSize}${filterQuery}`);
  }
  getEntriesSharedWithMe(page: number, pageSize: number, filter: EntryFilter): Observable<EntrySummary[]> {
    const filterQuery = filter ? `&filter=${filter}` : '';
    return this.http.get<EntrySummary[]>(`${this.entriesUrl}/shared?skip=${((page - 1) * pageSize)}&take=${pageSize}${filterQuery}`);
  }
  getEntriesSharedWithMeBy(userId: number, page: number, pageSize: number, filter: EntryFilter): Observable<EntrySummary[]> {
    const filterQuery = filter ? `&filter=${filter}` : '';
    return this.http.get<EntrySummary[]>(`${this.entriesUrl}/shared/${userId}?skip=${((page - 1) * pageSize)}&take=${pageSize}${filterQuery}`);
  }

	createEntry(entry: Entry): Observable<Entry> {
		return this.http.post<Entry>(this.entriesUrl + "/create", entry);
	}

	updateEntry(entry: Entry): Observable<Entry> {
		return this.http.post<SelfAssessments>(`${this.entriesUrl}/${entry.id}/update`, entry).pipe(
			tap((latestSelfAssessments: SelfAssessments) => {
				// If any self assessments were removed we will receive back the latest ones.
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, latestSelfAssessments);
				// Update the self assessments with the updated bundle.
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, entry.assessmentBundle);
			}),
			map(_ => entry)
		);
	}

	changeEntrySkillGrouping(entryId: string, skillGroupingId: number): Observable<string> {
		return this.http.post<string>(`${this.entriesUrl}/${entryId}/skill-grouping`, { skillGroupingId : skillGroupingId });
	}

	removeEntry(entry: EntrySummary): Observable<{ [key: number]: SelfAssessment }> {
		return this.http.post<SelfAssessments>(`${this.entriesUrl}/${entry.id}/remove`, {}).pipe(
			tap((latestSelfAssessments: SelfAssessments) => {
				// Remove the self assessments first before updating as we need to remove any from the cache.
				let selfAssessmentsToRemove: SelfAssessments = {};
				Object.keys(latestSelfAssessments).forEach(skillId => {
					selfAssessmentsToRemove[skillId] = null;
				});
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, selfAssessmentsToRemove);
				// If any self assessments were removed we will receive back the latest ones.
				this.skillAssessmentService.updateSelfAssessments(entry.author.id, entry.skillSetId, latestSelfAssessments);
			})
		);
	}

	shareEntry(shareEntryDto: ShareEntry): Observable<string> {
		return this.http.post<string>(`${this.entriesUrl}/${shareEntryDto.entryId}/share`, shareEntryDto);
	}

	removeCollaborator(entryId: any, userId: number): Observable<string> {
		return this.http.post<string>(`${this.entriesUrl}/${entryId}/collaborators/${userId}/remove`, {});
	}

	getCollaborators(entryId: any): Observable<User[]> {
		return this.http.get<User[]>(`${this.entriesUrl}/${entryId}/collaborators`);
	}

  requestEntrySignOff(entrySignOffRequestDto: EntrySignOffRequest): Observable<string> {
    return this.http.post<string>(`${this.entriesUrl}/${entrySignOffRequestDto.entryId}/request-sign-off`, entrySignOffRequestDto);
  }

  removeSignOffUser(entryId: any, userId: number): Observable<string> {
    return this.http.post<string>(`${this.entriesUrl}/${entryId}/request-sign-off/users/${userId}/remove`, {});
  }

  getSignOffUsers(entryId: any): Observable<User[]> {
    return this.http.get<User[]>(`${this.entriesUrl}/${entryId}/request-sign-off/users`);
  }

  signOff(entryCommentDto: EntryComment): Observable<EntryComment> {
    return this.http.post<number>(`${this.entriesUrl}/${entryCommentDto.entryId}/sign-off`, entryCommentDto).pipe(
      map((commentId: number) => {
        entryCommentDto.id = +commentId;
        return entryCommentDto;
      })
    );
  }


	comment(entryCommentDto: EntryComment): Observable<EntryComment> {
		return this.http.post<number>(`${this.entriesUrl}/${entryCommentDto.entryId}/comment`, entryCommentDto).pipe(
			map((commentId: number) => {
				entryCommentDto.id = +commentId;
				return entryCommentDto;
			})
		);
	}

	getUserPlaces(): Observable<Where[]> {
		return this.http.get<Where[]>(`${this.entriesUrl}/where`);
  }

  getEntryFiles(entryId: any): Observable<EntryFile[]> {
    return this.http.get<EntryFile[]>(`${this.entriesUrl}/${entryId}/files`);
  }

  deleteEntryFile(entryId: any, fileId: any): Observable<string> {
    return this.http.delete<string>(`file-uploads/entries/${entryId}/${fileId}`);
  }
}
