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

import { Observable, of } from 'rxjs';
import { map, publishReplay, refCount } from 'rxjs/operators';

import { Skill, SkillSet, SkillGroup, SelfAssessmentScale, SkillFilter, SkillGrouping } from "../core/dtos";
import { Utils } from "../core/utils";

@Injectable()
export class SkillService {
    private skillSetUrl = "skill-sets";

    // Local cache.
    private skillSets: Observable<SkillSet[]>;
	private skillGroupings: { [skillSetId: number]: Observable<SkillGrouping[]>; } = {};
	private skillGroups: { [skillSetId: number]: { [skillGroupingId: number]: SkillGroup[]; } } = {};
	private selfAssessmentScales: { [skillSetId: number]: Observable<SelfAssessmentScale[]>; } = {};
	private skillFilters: { [skillSetId: number]: SkillFilter[]; } = {};
	private skillBundles: { [skillBundleId: number]: number[]; } = {};

	public selectedSkillGroupings: { [skillSetId: number]: SkillGrouping } = {};

    constructor(private http: HttpClient) { }

	getSkillSets(): Observable<SkillSet[]> {
        // Return the cached value if available.
		if (!this.skillSets) {
			this.skillSets = this.http.get<SkillSet[]>(this.skillSetUrl).pipe(
				publishReplay(1),
				refCount()
			);
		}
		return this.skillSets;
	}
	
	getSkillGroupings(skillSetId: number): Observable<SkillGrouping[]> {
		let url = `${this.skillSetUrl}/${skillSetId}/skill-groupings`;
		// Return the cached value if available.
		if (!this.skillGroupings[skillSetId]) {
			this.skillGroupings[skillSetId] = this.http.get<SkillGrouping[]>(url).pipe(
				publishReplay(1),
				refCount()
			);
		}
		return this.skillGroupings[skillSetId];
    }
	
	getDefaultSkillGrouping(skillGroupings: SkillGrouping[]): SkillGrouping {		
		return skillGroupings.reduce((prev, curr) => { return (prev && (prev.id < curr.id)) ? prev : curr });
	}
	
	getSkillGroups(skillSetId: number, skillGroupingId: number): Observable<SkillGroup[]> {
		let url = `${this.skillSetUrl}/${skillSetId}/skill-groupings/${skillGroupingId}`;
        // Return the cached value if available.
		// We use this cache method rather than the publishReplay as we need to deepcopy the result.
		// The negitive of this method is that we can cause multiple requests.
        return !this.skillGroups[skillSetId] || !this.skillGroups[skillSetId][skillGroupingId]
			? this.http.get<SkillGroup[]>(url).pipe(
               	map((skillGroups: SkillGroup[]) => {
				   if(!this.skillGroups[skillSetId]) {
					this.skillGroups[skillSetId] = {};
				   }
				   this.skillGroups[skillSetId][skillGroupingId] = skillGroups;
                   return Utils.deepClone(this.skillGroups[skillSetId][skillGroupingId]);
				}))
			: of<SkillGroup[]>(Utils.deepClone(this.skillGroups[skillSetId][skillGroupingId]));
	}

	getSelfAssessmentScales(skillSetId: number): Observable<SelfAssessmentScale[]> {
		let url = `${this.skillSetUrl}/${skillSetId}/self-assessment-scales`;
		// Return the cached value if available.
		if (!this.selfAssessmentScales[skillSetId]) {
			this.selfAssessmentScales[skillSetId] = this.http.get<SelfAssessmentScale[]>(url).pipe(
				publishReplay(1),
				refCount()
			);
		}
		return this.selfAssessmentScales[skillSetId];
	}

	getSkillFilters(skillSetId: number): Observable<SkillFilter[]> {
        let url = `${this.skillSetUrl}/${skillSetId}/filters`;
        // Return the cached value if available.
        return this.skillFilters[skillSetId]
            ? of<SkillFilter[]>(Utils.deepClone(this.skillFilters[skillSetId]))
            : this.http.get<SkillFilter[]>(url).pipe(
               	map((skillFilters: SkillFilter[]) => {
				   this.skillFilters[skillSetId] = skillFilters;
                   return Utils.deepClone(this.skillFilters[skillSetId]);
				})
			);
	}

	getSkillBundle(skillSetId: number, skillBundleId: number): Observable<number[]> {
        let url = `${this.skillSetUrl}/${skillSetId}/skill-bundles/${skillBundleId}`;
        // Return the cached value if available.
        return this.skillBundles[skillBundleId]
            ? of<number[]>(this.skillBundles[skillBundleId])
            : this.http.get<number[]>(url).pipe(
               	map((ids: number[]) => {
				   this.skillBundles[skillBundleId] = ids;
                   return this.skillBundles[skillBundleId];
				})
			);
	}

	static findSkillInGroups(skillId: number, skillGroups: SkillGroup[]): Skill {
		if (!skillGroups || skillGroups.length === 0) return undefined;
		let skill: Skill;
		skillGroups.some(group => {
			skill = group.skills.find(s => s.id === skillId);
			if (skill) return true;
			skill = this.findSkillInGroups(skillId, group.childGroups);
			return skill !== undefined;
		});
		return skill;
	}
}
