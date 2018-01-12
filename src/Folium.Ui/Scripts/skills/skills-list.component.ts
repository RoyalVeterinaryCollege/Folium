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
import {
  Component,
  Input,
  ChangeDetectionStrategy,
  OnInit,
  OnDestroy,
  ChangeDetectorRef,
  Output,
  EventEmitter
} from "@angular/core";

import { Subscription } from "rxjs/subscription";

import { Skill, SelfAssessment, SelfAssessmentScale, User } from "../dtos";
import { SkillService } from "./skill.service";
import { SkillBundleService } from "./skill-bundle.service";
import { NotificationService } from "../common/notification.service";
import { AssessmentSliderComponent } from "./assessment-slider.component";
import { Utils } from "../common/utils";
import { SkillAssessmentService } from "./skill-assessment.service";

@Component({
  selector: "skills-list",
  templateUrl: "html/skills/skills-list.component.html",
  /* We use this stratergy as the assessment-slider binds to the resize event and this seems to cause performance
  issues with change detection */
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SkillsListComponent implements OnInit, OnDestroy {
  @Input()
  skills: Skill[];

  @Input()
  user: User;

  @Input()
  selfAssessmentScales: SelfAssessmentScale[];

  @Input()
  expanded: boolean;

  @Input()
  readOnly: boolean;

  @Input()
  autoSave: boolean;

  @Output()
  selfAssessmentChange = new EventEmitter<SelfAssessment>();

  //private bundleFilterUpdated$: Subscription;

  constructor(
    private notificationService: NotificationService,
	  private skillBundleService: SkillBundleService,
	  private skillService: SkillService,
    private skillAssessmentService: SkillAssessmentService,
    private changeDetectorRef: ChangeDetectorRef
  ) {   }

  ngOnInit() {
    /**
    this.bundleFilterUpdated$ = this.assessmentBundleService.onBundleFilterUpdated.subscribe(c => {
      this.changeDetectorRef.markForCheck();
    });
    **/
  }

  /** Used to see if the current score of the skill is at the provided scale.
   */
  isSelfAssessmentScaleSelected(skill: Skill, scale: SelfAssessmentScale) {
	  return skill.assessment.activeSelfAssessment && (skill.assessment.activeSelfAssessment.score === scale.score);
  }

  /** Used to set the current score of the skill to the provided scale.
   */
  setSelfAssessmentScore(skill: Skill, scale: SelfAssessmentScale) {
    if (this.readOnly) return;
    let selfAssessment = new SelfAssessment(scale.levelId, skill.id, scale.score, new Date(Date.now()));
    this.onSelfAssessmentChange(skill, selfAssessment);
    skill.assessment.activeSelfAssessment = selfAssessment;
  }

  getSelfAssessmentScale(skill: Skill) {
    return this.selfAssessmentScales.filter(
      scale => scale.id === skill.selfAssessmentScaleId);
  }

  getSelfAssessmentScaleColour(selfAssessmentScale: SelfAssessmentScale) {
    return AssessmentSliderComponent.getScaleColour(selfAssessmentScale);
  }

  onSelfAssessmentChange(skill: Skill, updatedSelfAssessment: SelfAssessment) {
    if (this.autoSave) {
      this.saveSelfAssessment(skill, updatedSelfAssessment);
    } else {
      this.addToBundle(skill, updatedSelfAssessment);
    }
    this.selfAssessmentChange.emit(updatedSelfAssessment);
  }
	
  onBundleClick(skill: Skill) {
    if (this.autoSave) return;
    if (skill.assessment.isInBundle) {
      // In bundle, remove.
      this.removeFromBundle(skill, skill.assessment.activeSelfAssessment);
    } else {
      // Not in the bundle, add.      
      this.addToBundle(skill, skill.assessment.activeSelfAssessment);
    }
  }

  trackSkill(index, skill: Skill) {
    return skill.id;
  }

  ngOnDestroy() {
    //this.bundleFilterUpdated$.unsubscribe();
  }

  private saveSelfAssessment(skill: Skill, selfAssessment: SelfAssessment) {
    this.skillAssessmentService.createSelfAssessment(skill.skillSetId, selfAssessment)
      .subscribe(() => {
        this.skillAssessmentService.updateSelfAssessment(this.user.id, skill.skillSetId, selfAssessment);
        this.notificationService.addSuccess("Self assessment saved");
      },
      (error: any) => {
        this.notificationService.addDanger(`There was an error trying to save your self assessment, please try again.
        ${error}`)
      });	
  }

  private addToBundle(skill: Skill, selfAssessment: SelfAssessment) {
	  if (skill.assessment.isInBundle) return;
    if (!selfAssessment) {
      let scale = this.getSelfAssessmentScale(skill)[0]; // get the lowest scale.
      skill.assessment.activeSelfAssessment = new SelfAssessment(scale.levelId, skill.id, scale.score, new Date(Date.now()));
    }
    this.skillBundleService.addToBundle(skill.id);
    skill.assessment.isInBundle = true;
  }

  private removeFromBundle(skill: Skill, selfAssessment: SelfAssessment) {
	  if (!skill.assessment.isInBundle) return;
    this.skillBundleService.removeFromBundle(skill.id);
    skill.assessment.activeSelfAssessment = Utils.deepClone(skill.assessment.prevailingSelfAssessment);
    skill.assessment.isInBundle = false;
  }
}