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

import 'rxjs/observable/of';
import 'rxjs/add/operator/publishReplay';
import 'rxjs/add/operator/map';

import { Skill, SkillSet, SkillGroup, SelfAssessment, SkillAssessment, SkillFilter, SelfAssessmentBundle } from "../dtos";
import { HttpService } from "../common/http.service";
import { ResponseService } from "../common/response.service";
import { Utils } from "../common/utils";
import { SkillSetSelectionService } from "../skill-set/selection.service";

@Injectable()
export class SkillAssessmentService {
    private skillSetUrl = "skill-set";
	private skillSetChanged$: Subscription;

    // Local cache.
	private selfAssessments: { [skillSetId: number]: { [skillId: number]: SelfAssessment } } = {};

    constructor(
		private http: Http, 
		private responseService: ResponseService,
		private skillSetSelectionService: SkillSetSelectionService) { 
		if(skillSetSelectionService.skillSet) {
			this.getSelfAssessments(skillSetSelectionService.skillSet.id)
				.subscribe(_=>_);;
		}
		this.skillSetChanged$ = skillSetSelectionService.onSkillSetChanged.subscribe(s => this.onSkillSetChanged())
	}

	setSkillAssessmentsForSkillGroups(skillSetId: number, skillGroups: SkillGroup[]);	
	setSkillAssessmentsForSkillGroups(skillSetId: number, skillGroups: SkillGroup[], selfAssessmentBundle: SelfAssessmentBundle);
	setSkillAssessmentsForSkillGroups(skillSetId: number, skillGroups: SkillGroup[], selfAssessmentBundle?: SelfAssessmentBundle) {
		if(!skillGroups) return;

	    // loop the skills setting the assessment.
		skillGroups.forEach(skillGroup => {
			skillGroup.skills.forEach(skill => {
				skill.assessment = new SkillAssessment();
				skill.assessment.skill = skill;
				skill.assessment.prevailingSelfAssessment = Utils.deepClone(this.selfAssessments[skillSetId][skill.id]);
				skill.assessment.activeSelfAssessment = (selfAssessmentBundle ?  Utils.deepClone(selfAssessmentBundle[skill.id]) :  Utils.deepClone(this.selfAssessments[skillSetId][skill.id]));
				skill.assessment.isInBundle = selfAssessmentBundle ? selfAssessmentBundle[skill.id] != undefined : false;
			});
			this.setSkillAssessmentsForSkillGroups(skillSetId, skillGroup.childGroups, selfAssessmentBundle);
		});		
    }

	updateSelfAssessment(skillSetId: number, selfAssessment: SelfAssessment) {
		let selfAssessments: { [skillId: number]: SelfAssessment; } = {};
		selfAssessments[selfAssessment.skillId] = selfAssessment;
		this.updateSelfAssessments(skillSetId, selfAssessments);
	}

	updateSelfAssessments(skillSetId: number, selfAssessmentBundle: SelfAssessmentBundle) {
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

			// Set it if we don't have a current self assessment for that skll or it is newer.
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

	getAssessmentBundle(skillGroups: SkillGroup[]): SelfAssessmentBundle {
		let bundle = new SelfAssessmentBundle();
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

	private onSkillSetChanged() {
		this.getSelfAssessments(this.skillSetSelectionService.skillSet.id)
			.subscribe(_=>_);
	}
	private getEpochTime(dateTime: Date): number {
		return Math.floor(dateTime.getTime() / 1000);
	}
	private getSelfAssessments(skillSetId: number): Observable<SelfAssessment[]> {
		let url = `${this.skillSetUrl}/${skillSetId}/self-assessments`;
		// Return the cached value if available.
        return this.selfAssessments[skillSetId]
			? Observable.of(this.selfAssessments[skillSetId])
            : this.http.get(url)
               .map(response => {
				   let selfAssessments = this.responseService.parseJson(response) as SelfAssessment[];
				   this.selfAssessments[skillSetId] = Utils.toDictionary(selfAssessments, (selfAssessment) => selfAssessment.skillId);
                   return this.selfAssessments[skillSetId];
                });
	}
}
