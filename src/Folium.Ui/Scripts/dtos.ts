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
export class Skill {
  id: number;
  name: string;
  childSkills: Skill[];
  canSelfAssess: boolean;
  skillSetId: number;
  canSelfCount: boolean;
  selfAssessmentScaleId: number;
  assessment: SkillAssessment;
}

export class SelfAssessment {
	levelId: number;
	skillId: number;
	score: number;
	createdAt: Date;
	public constructor(levelId: number, skillId: number, score: number, createdAt: Date) {
		this.levelId = levelId;
		this.skillId = skillId;
		this.score = score;
		this.createdAt = createdAt;
	}
}

export class SelfAssessments { [skillId: number]: SelfAssessment } 

export class SkillAssessment {
  skill: Skill;
  hidden: boolean;
  isInBundle:boolean;
  prevailingSelfAssessment: SelfAssessment;
  activeSelfAssessment: SelfAssessment;
}

export class SkillGroup {
  id: number;
  name: string;
  childGroups: SkillGroup[];
  skills: Skill[];
  expanded: boolean;
}

export class SkillSet {
  id: number;
  name: string;
  description: string;
  selected: boolean;
}

export class SelfAssessmentScale {
  id: number;
  name: string;
  levelId: number;
  levelName: string;
  score: number;
}

export class SkillFilter {
  id: number;
  name: string;
  facets: SkillFilterFacet[];
}

export class SkillFilterFacet {
  id: number;
  name: string;
  matchedSkillIds: number[];
  skillFilterId: number;
  selected: boolean;
}

export class Entry {
  id: string; // guid
  title: string;
  description: any;
  where: string;
  when: Date;
  skillSetId: number;
  assessmentBundle: SelfAssessments;
  entryType: EntryType;
  lastUpdatedAt: Date;
  author: User;
  comments: EntryCommentDto[];
  shared: boolean;
}

export class EntrySummary {
  id: string; // guid
  title: string;
  where: string;
  when: Date;
  type: string;
  author: User;
  shared: boolean;
  skillSetId: number;
  viewing: boolean; // Whether the entry is being viewed.
  editing: boolean; // Whether the entry is being edited.
	public constructor(entry: Entry) {
    this.id = entry.id;
    this.title = entry.title;
    this.type = entry.entryType ? entry.entryType.name : undefined;
    this.when = entry.when;
    this.where = entry.where;
    this.author = entry.author;
    this.shared = entry.shared;
    this.skillSetId = entry.skillSetId;
	}
}

export class EntryType {
  id: number;
  name: string;
  template: [ { title: string; help: string; } ];
  skillSetId: number;
}

export class User {
	id: number;
	firstName: string;
	lastName: string;
	pic: string;
	lastSignIn: Date;
  courses: number[];
  email: string;
}

export class Where {
	name: string;
	usageCount: number;
}

export class Placement {
  id: string; // guid
	userId: number;
  title: string;
  fullyQualifiedTitle: string;
  start: Date;
  end: Date;
  cancelled: boolean;
  entryCount: number;
  editing: boolean;
  lastUpdatedAt: Date;
}

export class CollaboratorOption {
	name: string;
	user: User;
	isGroup: boolean;
	group: User[];
}

export class EntryCommentDto {
  id: number;
  entryId: string; // guid
  comment: string;
  author: User;
  createdAt: Date;
}
  
export class ShareEntryDto {
  entryId: string; // guid
	collaboratorIds: number[];
	message: string;
}