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

import { Observable, Subscription, of } from 'rxjs';
import { tap, map } from "rxjs/operators";

import { SkillGroup, SelfAssessment, SkillAssessment, SelfAssessments } from "../core/dtos";
import { Utils } from "../core/utils";

@Injectable()
export class SkillAssessmentService {
    private skillSetUrl = "skill-sets";

    // Local cache.
	private selfAssessments: { [userId:number]: { [skillSetId: number]: { [skillId: number]: SelfAssessment } } } = {};

    constructor(private http: HttpClient) {
	}

	setUserSkillAssessmentsForSkillGroups(userId: number, skillSetId: number, skillGroups: SkillGroup[]): Observable<SkillGroup[]> {
		return this.getSelfAssessments(userId, skillSetId).pipe(
			tap(selfAssessments => {				
				this.setSkillAssessmentsForSkillGroups(skillGroups, selfAssessments);
			}),
			map(_ => skillGroups)
		);
	}

	setSkillAssessmentsForSkillGroups(skillGroups: SkillGroup[], selfAssessments: SelfAssessments, skillAssessments: {[skillId:number]: SkillAssessment} = {} /* To track existing skill assessments */) {
		if(!skillGroups) return;
		// loop the skills setting the assessment.
		skillGroups.forEach(skillGroup => {
			skillGroup.skills.forEach(skill => {
				// Check if we already have an assessment for this skill, this can happen when skills sit in multiple groups.
				if(skillAssessments[skill.id]) {
					skill.assessment = skillAssessments[skill.id];
				} else {
					skill.assessment = new SkillAssessment();
					skill.assessment.skill = skill;
					skill.assessment.prevailingSelfAssessment = Utils.deepClone(selfAssessments[skill.id]);
					skill.assessment.activeSelfAssessment = Utils.deepClone(selfAssessments[skill.id]);
					skillAssessments[skill.id] = skill.assessment;
				}
			});
			this.setSkillAssessmentsForSkillGroups(skillGroup.childGroups, selfAssessments, skillAssessments);
		});
	}

	updateSelfAssessment(userId: number, skillSetId: number, selfAssessment: SelfAssessment) {
		let selfAssessments: SelfAssessments = {};
		selfAssessments[selfAssessment.skillId] = selfAssessment;
		this.updateSelfAssessments(userId, skillSetId, selfAssessments);
	}

	getSelfAssessment(userId: number, skillSetId: number, skillId: number): SelfAssessment {
		if(!this.selfAssessments[userId] || !this.selfAssessments[userId][skillSetId] || !this.selfAssessments[userId][skillSetId][skillId]) return;
		return this.selfAssessments[userId][skillSetId][skillId];
	}

	updateSelfAssessments(userId: number, skillSetId: number, selfAssessments: SelfAssessments) {
		if(!this.selfAssessments[userId] || !this.selfAssessments[userId][skillSetId]) return;
		Object.keys(selfAssessments).forEach(skillId => {
			let updatedSelfAssessment = selfAssessments[skillId] as SelfAssessment;
			let currentSelfAssessment = this.selfAssessments[userId][skillSetId][skillId] as SelfAssessment;
			
			// If we have a self assessment that has been updated to empty then remove it.
			if(!updatedSelfAssessment) {
				if(this.selfAssessments[userId][skillSetId][skillId]) {
					delete this.selfAssessments[userId][skillSetId][skillId];
				}
				return;
			}

			// Set it if we don't have a current self assessment for that skill or it is newer.
			if(!currentSelfAssessment 
				|| this.getEpochTime(currentSelfAssessment.createdAt) <= this.getEpochTime(updatedSelfAssessment.createdAt)) {
				this.selfAssessments[userId][skillSetId][skillId] = Utils.deepClone(updatedSelfAssessment);
				return;
			}
		});
	}

	createSelfAssessment(skillSetId: number, selfAssessment: SelfAssessment): Observable<void> {
		return this.http.post(`${this.skillSetUrl}/${skillSetId}/self-assessments/create`, selfAssessment).pipe(
			map(_ => {})
		);
	}

	getAverageSelfAssessmentFromGroup(skillGroup: SkillGroup) {
		if (!skillGroup) return 0;
		let getTotalAndCount = function(skillGroup: SkillGroup) {
			let totalAndCount = skillGroup.skills.map(s => {
			if(s.assessment.hidden) {
				return { total: 0, count: 0 }; // don't include the assessment if it is hidden.
			} 
			return s.assessment.activeSelfAssessment 
				? { total: s.assessment.activeSelfAssessment.score, count: 1 } // If we have an assessment then use the score.
				: { total: 0, count: 1 }; // No assessment, count it, but use 0 as the total.
			});
			skillGroup.childGroups.forEach(group => {
			totalAndCount.push(getTotalAndCount(group));
			});
			// Sum up all the totals and counts.
			return totalAndCount.reduce((previous, current) => { return { total: previous.total + current.total, count: previous.count + current.count }; }, { total: 0, count: 0 });
		}
		let totals = getTotalAndCount(skillGroup);
		return totals.total / totals.count;    
	}

	private getEpochTime(dateTime: Date): number {
		return Math.floor(dateTime.getTime() / 1000);
	}
	
	private getSelfAssessments(userId: number, skillSetId: number): Observable<SelfAssessments> {
		let url = `${this.skillSetUrl}/${skillSetId}/self-assessments?userId=${userId}`;
		// Return the cached value if available.
        return (this.selfAssessments[userId] && this.selfAssessments[userId][skillSetId])
			? of<SelfAssessments>(this.selfAssessments[userId][skillSetId])
            : this.http.get<SelfAssessment[]>(url).pipe(
               map((selfAssessments: SelfAssessment[]) => {
				   if(!this.selfAssessments[userId]) {
					this.selfAssessments[userId] = {};
				   }
				   this.selfAssessments[userId][skillSetId] = Utils.toDictionary(selfAssessments, (selfAssessment) => selfAssessment.skillId);
                   return this.selfAssessments[userId][skillSetId];
				}, this)
			);
	}
}
