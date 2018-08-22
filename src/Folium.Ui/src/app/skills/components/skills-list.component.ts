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
  SimpleChanges,
  SimpleChange,
  OnChanges
} from "@angular/core";

import { Skill, SelfAssessment, SelfAssessmentScale, User } from "../../core/dtos";
import { SkillService } from "../skill.service";
import { SkillBundleService } from "../skill-bundle.service";
import { NotificationService } from "../../core/notification.service";
import { AssessmentSliderComponent } from "./assessment-slider.component";
import { SkillAssessmentService } from "../skill-assessment.service";

@Component({
  selector: "skills-list",
  templateUrl: "skills-list.component.html",
  /* We use this stratergy as the assessment-slider binds to the resize event and this seems to cause performance
  issues with change detection */
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SkillsListComponent implements OnInit, OnDestroy, OnChanges {
  @Input()
  skills: Skill[];

  @Input()
  skillSetId: number;

  @Input()
  user: User;

  @Input()
  expanded: boolean;

  @Input()
  readOnly: boolean;

  @Input()
  autoSave: boolean;

  selfAssessmentScales: SelfAssessmentScale[];

  constructor(
    private notificationService: NotificationService,
	  private skillBundleService: SkillBundleService,
	  private skillService: SkillService,
    private skillAssessmentService: SkillAssessmentService,
    private changeDetectorRef: ChangeDetectorRef
  ) {   }

  ngOnInit() {
    this.skillService.getSelfAssessmentScales(this.skillSetId).subscribe(selfAssessmentScales => {
        this.selfAssessmentScales = selfAssessmentScales;
        this.changeDetectorRef.markForCheck();
    });
    this.skillBundleService.onBundleChange.subscribe(skill => {      
      if(this.skills.filter(s => s.id === skill.id).length > 0) {
        this.changeDetectorRef.markForCheck();
      }
    });
    this.skillBundleService.onSkillAssessmentChange.subscribe(skill => {      
      if(this.skills.filter(s => s.id === skill.id).length > 0) {
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    const expanded: SimpleChange = changes.expanded;
    if (expanded && (expanded.previousValue !== expanded.currentValue)) {
      this.changeDetectorRef.markForCheck();
    }
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
      this.skillBundleService.addToBundle(skill, updatedSelfAssessment);
    }
    this.skillBundleService.onSkillAssessmentChange.emit(skill);
  }
	
  onBundleClick(skill: Skill) {
    if (this.autoSave) return;
    if (this.skillBundleService.isInBundle(skill)) {
      // In bundle, remove.
      this.skillBundleService.removeFromBundle(skill);
    } else {
      // Not in the bundle, add.      
      this.skillBundleService.addToBundle(skill);
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
}