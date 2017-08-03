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
import { Http } from "@angular/http";
import { Observable } from 'rxjs/Observable';
import { Subscription } from "rxjs/Subscription";

import 'rxjs/add/operator/publishReplay';
import 'rxjs/add/operator/map';
import 'rxjs/add/observable/of';

import { Skill, SkillSet, SkillGroup, SelfAssessment, SkillAssessment, SkillFilter, SelfAssessments } from "../dtos";
import { HttpService } from "../common/http.service";
import { ResponseService } from "../common/response.service";
import { Utils } from "../common/utils";

@Injectable()
export class SkillAssessmentService {
    private skillSetUrl = "skill-sets";
	private skillSetChanged$: Subscription;

    // Local cache.
	private selfAssessments: { [skillSetId: number]: { [skillId: number]: SelfAssessment } } = {};

    constructor(
		private http: Http, 
		private responseService: ResponseService) {
	}

	setSkillAssessmentsForSkillGroups(skillSetId: number, skillGroups: SkillGroup[]): Observable<SkillGroup[]>;
	setSkillAssessmentsForSkillGroups(skillSetId: number, skillGroups: SkillGroup[], selfAssessmentBundle: SelfAssessments, readOnlyBundle: boolean): Observable<SkillGroup[]>;
	setSkillAssessmentsForSkillGroups(skillSetId: number, skillGroups: SkillGroup[], selfAssessmentBundle?: SelfAssessments, readOnlyBundle?: boolean): Observable<SkillGroup[]> {		
		let setAssessments = function(skillGroups: SkillGroup[], selfAssessments: SelfAssessments, selfAssessmentBundle: SelfAssessments, readOnlyBundle: boolean) {
			if(!skillGroups) return;
			// loop the skills setting the assessment.
			skillGroups.forEach(skillGroup => {
				skillGroup.skills.forEach(skill => {
					skill.assessment = new SkillAssessment();
					skill.assessment.skill = skill;
					skill.assessment.prevailingSelfAssessment = readOnlyBundle ? {} : Utils.deepClone(selfAssessments[skill.id]);
					skill.assessment.activeSelfAssessment = ((selfAssessmentBundle && selfAssessmentBundle[skill.id]) ?  Utils.deepClone(selfAssessmentBundle[skill.id]) :  Utils.deepClone(selfAssessments[skill.id]));
					skill.assessment.isInBundle = selfAssessmentBundle ? selfAssessmentBundle[skill.id] != undefined : false;
				});
				setAssessments(skillGroup.childGroups, selfAssessments, selfAssessmentBundle, readOnlyBundle);
			});
		}
		return this.getSelfAssessments(skillSetId)
			.do(selfAssessments => {				
				setAssessments(skillGroups, selfAssessments, selfAssessmentBundle, readOnlyBundle);
			})
			.map(_ => skillGroups);
    }

	updateSelfAssessment(skillSetId: number, selfAssessment: SelfAssessment) {
		let selfAssessments: SelfAssessments = {};
		selfAssessments[selfAssessment.skillId] = selfAssessment;
		this.updateSelfAssessments(skillSetId, selfAssessments);
	}

	updateSelfAssessments(skillSetId: number, selfAssessmentBundle: SelfAssessments) {
		if(!this.selfAssessments[skillSetId]) return;
		Object.keys(selfAssessmentBundle).forEach(skillId => {
			let updatedSelfAssessment = selfAssessmentBundle[skillId] as SelfAssessment;
			let currentSelfAssessment = this.selfAssessments[skillSetId][skillId] as SelfAssessment;
			
			// If we have a self assessment that has been updated to empty then remove it.
			if(!updatedSelfAssessment) {
				if(this.selfAssessments[skillSetId][skillId]) {
					delete this.selfAssessments[skillSetId][skillId];
				}
				return;
			}

			// Set it if we don't have a current self assessment for that skill or it is newer.
			if(!currentSelfAssessment 
				|| this.getEpochTime(currentSelfAssessment.createdAt) <= this.getEpochTime(updatedSelfAssessment.createdAt)) {
				this.selfAssessments[skillSetId][skillId] = Utils.deepClone(updatedSelfAssessment);
				return;
			}
		});
	}

	createSelfAssessment(skillSetId: number, selfAssessment: SelfAssessment): Observable<void> {
		return this.http.post(`${this.skillSetUrl}/${skillSetId}/self-assessments/create`, selfAssessment)
			.map(_ => {});
	}

	getAssessmentBundle(skillGroups: SkillGroup[]): SelfAssessments {
		let bundle = new SelfAssessments();
		for (let skillGroup of skillGroups) {
			for (let skill of skillGroup.skills) {
				if (skill.assessment.isInBundle) {
					bundle[skill.id] = skill.assessment.activeSelfAssessment;
				}
			}
			if(skillGroup.childGroups) {
				let childBundle = this.getAssessmentBundle(skillGroup.childGroups);
				for (let skillId in childBundle) {
					bundle[skillId] = childBundle[skillId];
				}
			}
		}
		return bundle;
	}

	private getEpochTime(dateTime: Date): number {
		return Math.floor(dateTime.getTime() / 1000);
	}
	
	private getSelfAssessments(skillSetId: number): Observable<SelfAssessments> {
		let url = `${this.skillSetUrl}/${skillSetId}/self-assessments`;
		// Return the cached value if available.
        return this.selfAssessments[skillSetId]
			? Observable.of(this.selfAssessments[skillSetId])
            : this.http.get(url)
               .map(response => {
				   let selfAssessments = this.responseService.parseJson(response) as SelfAssessment[];
				   this.selfAssessments[skillSetId] = Utils.toDictionary(selfAssessments, (selfAssessment) => selfAssessment.skillId);
                   return this.selfAssessments[skillSetId];
                }, this);
	}
}
