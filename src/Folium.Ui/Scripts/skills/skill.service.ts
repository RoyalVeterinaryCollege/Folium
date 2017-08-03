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
import { Response, Http } from "@angular/http";
import { Observable } from 'rxjs/Observable';

import 'rxjs/add/operator/publishReplay';
import 'rxjs/add/operator/map';
import 'rxjs/add/observable/of';

import { Skill, SkillSet, SkillGroup, SelfAssessment, SelfAssessmentScale, SkillFilter } from "../dtos";
import { HttpService } from "../common/http.service";
import { ResponseService } from "../common/response.service";
import { Utils } from "../common/utils";

@Injectable()
export class SkillService {
    private skillSetUrl = "skill-sets";

    // Local cache.
    private skillSets: Observable<SkillSet[]>;
	private skillGroups: { [id: number]: SkillGroup[]; } = {};
	private selfAssessmentScales: { [id: number]: Observable<SelfAssessmentScale[]>; } = {};
    private skillFilters: { [id: number]: SkillFilter[]; } = {};

    constructor(private http: Http, private responseService: ResponseService) { }

	getSkillSets(): Observable<SkillSet[]> {
        // Return the cached value if available.
		if (!this.skillSets) {
			this.skillSets = this.http.get(this.skillSetUrl)
				.map(response => {
					return this.responseService.parseJson(response) as SkillSet[];
				})
				.publishReplay(1)
				.refCount();
		}
		return this.skillSets;
    }

	getSkillGroups(skillSetId: number): Observable<SkillGroup[]> {
		let url = `${this.skillSetUrl}/${skillSetId}`;
        // Return the cached value if available.
		// We use this cache method rather than the publishReplay as we need to deepcopy the result.
		// The negitive of this method is that we can cause multiple requests.
        return this.skillGroups[skillSetId]
			? Observable.of(Utils.deepClone(this.skillGroups[skillSetId]))
            : this.http.get(url)
               .map(response => {
				   this.skillGroups[skillSetId] = this.responseService.parseJson(response) as SkillGroup[];
                   return Utils.deepClone(this.skillGroups[skillSetId]);
                });
	}

	getSkill(skillSetId: number, skillId: number): Observable<Skill> {
		// Get the skill group and then search within in.
		
        let groups =  this.skillGroups[skillSetId]
			? Observable.of(this.skillGroups[skillSetId])
            : this.getSkillGroups(skillSetId);

		return groups
			.map(groups => {
				let skill = SkillService.findSkillInGroups(skillId, groups);
				return Utils.deepClone(skill); // deep clone the skill.
			});
	}

	getSelfAssessmentScales(skillSetId: number): Observable<SelfAssessmentScale[]> {
		let url = `${this.skillSetUrl}/${skillSetId}/self-assessment-scales`;
		// Return the cached value if available.
		if (!this.selfAssessmentScales[skillSetId]) {
			this.selfAssessmentScales[skillSetId] = this.http.get(url)
               .map(response => {
				   return this.responseService.parseJson(response) as SelfAssessmentScale[];
				})
				.publishReplay(1)
				.refCount();
		}
		return this.selfAssessmentScales[skillSetId];
	}

	getSkillFilters(skillSetId: number): Observable<SkillFilter[]> {
        let url = `${this.skillSetUrl}/${skillSetId}/filters`;
        // Return the cached value if available.
        return this.skillFilters[skillSetId]
            ? Observable.of(Utils.deepClone(this.skillFilters[skillSetId]))
            : this.http.get(url)
               .map(response => {
				   this.skillFilters[skillSetId] = this.responseService.parseJson(response) as SkillFilter[];
                   return Utils.deepClone(this.skillFilters[skillSetId]);
                });
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
