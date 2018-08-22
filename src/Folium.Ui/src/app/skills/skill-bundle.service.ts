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
import { Injectable, EventEmitter } from "@angular/core";
import { SelfAssessments, SkillGroup, Skill, SelfAssessment } from "../core/dtos";
import { Utils } from "../core/utils";
import { SkillAssessmentService } from "./skill-assessment.service";
import { SkillService } from "./skill.service";

@Injectable()
export class SkillBundleService {

	private skills: { [skillId: number]: Skill; } = {};
	private bundleChange$: EventEmitter<Skill> = new EventEmitter<Skill>();	
	private skillAssessmentChange$: EventEmitter<Skill> = new EventEmitter<Skill>(); /* Raised when a skills assessment in the bundle changes. */

	constructor(
		private skillAssessmentService: SkillAssessmentService,
		private skillService: SkillService) { }

    get onBundleChange() {
		return this.bundleChange$;
	}

    get onSkillAssessmentChange() {
		return this.skillAssessmentChange$;
	}

	get items(): Skill[] {
		return Object.keys(this.skills).map(e => this.skills[e]);
	}

	get skillIds(): number[] {
		return this.items.map(i => i.id);
	}

	get bundleSize(): number {
		return this.items.length;
	}

	addToBundle(skill: Skill);
	addToBundle(skill: Skill, selfAssessment: SelfAssessment);
	addToBundle(skill: Skill, selfAssessment?: SelfAssessment) {
		if (this.isInBundle(skill)) return;
		this.add(skill, selfAssessment ? selfAssessment : skill.assessment.activeSelfAssessment);
		this.bundleChange$.emit(skill);
	}
	
	removeFromBundle(skill: Skill) {
		if (!this.isInBundle(skill)) return;
		this.remove(skill);
		this.bundleChange$.emit(skill);
	}

	emptyBundle() {
		this.items.forEach(
			(item) => this.removeFromBundle(item)
		);
	}
	
	resetBundle() {
		this.skills = [];
	}

	setBundleItems(skillGroups: SkillGroup[], items: number[], userId: number);
	setBundleItems(skillGroups: SkillGroup[], items: SelfAssessments);
	setBundleItems(skillGroups: SkillGroup[], items: any, userId?: number) {
		this.resetBundle();
		this.set(skillGroups, items, userId);
	}
	
	getSelfAssessmentsInBundle(skillGroups: SkillGroup[]): SelfAssessments {
		let selfAssessments = new SelfAssessments();
		this.items.forEach(skill => {
			selfAssessments[skill.id] = skill.assessment.activeSelfAssessment;
		});
		return selfAssessments;
	}

	isInBundle(skill: Skill) {
		return this.skills[skill.id] !== undefined;
	}

	private add(skill: Skill);
	private add(skill: Skill, selfAssessment: SelfAssessment);
	private add(skill: Skill, selfAssessment?: SelfAssessment) {
		this.skills[skill.id] = skill;
		if(selfAssessment) {
			skill.assessment.activeSelfAssessment = selfAssessment;
		}
		if(!skill.assessment.activeSelfAssessment) {
			this.skillService.getSelfAssessmentScales(skill.skillSetId)
				.subscribe(selfAssessmentScales => { 
					let scale = selfAssessmentScales.filter(scales => scales.id === skill.selfAssessmentScaleId)[0]; // get the lowest scale.
					skill.assessment.activeSelfAssessment = new SelfAssessment(scale.levelId, skill.id, scale.score, new Date(Date.now())); 
				});
		}
	}

	private remove(skill: Skill) {
		delete this.skills[skill.id]
		skill.assessment.activeSelfAssessment = Utils.deepClone(skill.assessment.prevailingSelfAssessment);
	}
	
	private set(skillGroups: SkillGroup[], items: number[], userId: number);
	private set(skillGroups: SkillGroup[], items: SelfAssessments);
	private set(skillGroups: SkillGroup[], items: any, userId?: number) {
		if(!items) return;
		if(!skillGroups) return;
		// loop the skills adding the relevant ones into the bundle.
		skillGroups.forEach(skillGroup => {
			skillGroup.skills.forEach(skill => {
				if(Array.isArray(items)) {
					if(items.indexOf(skill.id) !== -1) {
						this.add(skill, this.skillAssessmentService.getSelfAssessment(userId, skill.skillSetId, skill.id));
					}
				} else {
					if(items[skill.id]) {
						this.add(skill, items[skill.id]);
					}
				}
			});
			this.set(skillGroup.childGroups, items, userId);
		});
	}
}
