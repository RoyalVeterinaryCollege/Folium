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
import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core";
import { MatDialogRef } from "@angular/material/dialog";
import { ModalDirective } from "ngx-bootstrap/modal";
import { Subscription } from "rxjs";

import { UserService } from "../user.service";
import { User, SkillSet } from "../../core/dtos";
import { NotificationService } from "../../core/notification.service";

enum UserEditView {
  Default,
  Photo,
  SkillSet,
}

@Component({
  templateUrl: "dialog-user-editor.component.html",
})
export class DialogUserEditorComponent implements OnInit, OnDestroy {
  @ViewChild("editUserModal", { static: false })
  editUserModal: ModalDirective;

  user: User;
  currentView: UserEditView = UserEditView.Default;
  userEditView = UserEditView;
  userSkillSets: SkillSet[];

  private onSignedInUserChange$: Subscription;

  constructor(
    private userService: UserService,
    private notificationService: NotificationService) { }

  ngOnInit() {
    this.onSignedInUserChange$ = this.userService.signedInUser.subscribe(user => {
      this.user = user;      
      this.loadSkillSets();
    });
  }

  onDoneClick() {
    this.userService.hideUserEditView();
  }

  onSkillSetSelectorDone() {
    this.loadSkillSets();
    this.currentView = UserEditView.Default;
  }

  onPhotoEditDone() {
    this.currentView = UserEditView.Default;
  }
  
  onUpdatePhotoClick() {
    this.currentView = UserEditView.Photo;
  }
  
  onManageSkillSetsClick() {
    this.currentView = UserEditView.SkillSet;
  }

  ngOnDestroy() {
    this.onSignedInUserChange$.unsubscribe();
  }

	private loadSkillSets() {
    this.userSkillSets = undefined;
		this.userService
			.getSkillSets(this.user)
			.subscribe((skillSets: SkillSet[]) => {
				this.userSkillSets = skillSets;
			},
			(error: any) => this.notificationService.addDanger(`There was an error trying to load the user skill sets, please try again.
				${error}`));  
	}
}