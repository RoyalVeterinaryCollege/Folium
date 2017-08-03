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
import { SelfAssessments } from "../dtos";

@Injectable()
export class SkillBundleService {

	private skillIds: number[] = [];
	private bundleChange$: EventEmitter<number[]> = new EventEmitter<number[]>();

	constructor() { }

    get onBundleChange() {
		return this.bundleChange$;
	}

	get items(): number[] {
		return this.skillIds;
	}

	get bundleSize(): number {
		return this.skillIds.length;
	}

	addToBundle(skillId: number) {
		if (this.isInBundle(skillId)) return;
		this.skillIds.push(skillId);
		this.bundleChange$.emit(this.skillIds);
	}
	
	removeFromBundle(skillId: number) {
		if (!this.isInBundle(skillId)) return;
		this.skillIds.splice(this.skillIds.indexOf(skillId), 1);
		this.bundleChange$.emit(this.skillIds);
	}

	emptyBundle() {
		this.skillIds.forEach(
			(item) => this.removeFromBundle(item)
		);
	}
	
	resetBundle() {
		this.skillIds = [];
	}

	setBundleItems(items: number[]);
	setBundleItems(items: SelfAssessments);
	setBundleItems(items: any) {
		this.skillIds = [];
		if(!items) return;
		if(Array.isArray(items)) {
			this.skillIds = items as number[];
		} else {
			this.skillIds = Object.keys(items).length == 0 ? [] : Object.keys(items).map(k => +k)
		}

	}
	
	private isInBundle(skillId: number) {
		return this.skillIds.includes(skillId);
	}
}
