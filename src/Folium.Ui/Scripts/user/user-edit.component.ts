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
import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from "@angular/core";
import { ModalDirective } from "ngx-bootstrap";
import { Subscription } from "rxjs/subscription";

import { UserService } from "./user.service";
import { User } from "./../dtos";

declare var Slim: any;

@Component({
  selector: "modal-user-edit",
  templateUrl: "html/user/user-edit.component.html",
})
export class EditUserComponent implements OnInit, OnDestroy {
  @ViewChild("editUserModal")
  editUserModal: ModalDirective;

  @ViewChild("originalUserPic")
  public originalUserPic: ElementRef;

  @ViewChild("editedUserPic")
  public editedUserPic: ElementRef;

  @ViewChild("slimImageCropper")
  public slimImageCropper: ElementRef;

  modalVisible: boolean = false;
  slimOptions = {
    ratio: "1:1",
    size: {
      width: 150,
      height: 150,
    },
    minSize: {
        width: 150,
        height: 150,
    },
    forceSize: {
        width: 150,
        height: 150,
    }
  };
  user: User;

  private onUserEditViewChange$: Subscription;
  private onSignedInUserChange$: Subscription;

  constructor(private userService: UserService) { }

  ngOnInit() {
    this.onUserEditViewChange$ = this.userService.onShowUserEditView.subscribe(show => {
      if (show) {
        this.showModal();
      } else {
        this.closeModal();
      }
    });
	this.onSignedInUserChange$ = this.userService.signedInUser.subscribe(user => this.user = user);
  }

  onSave() {
    let originalUserPic = this.originalUserPic.nativeElement.files[0];
    let editedUserPic = this.editedUserPic.nativeElement.value;
    // Only send the edited pic if we have it.
    if (editedUserPic) {
      originalUserPic = undefined;
      editedUserPic = (JSON.parse(editedUserPic)).output.image; // Get just the base64 encoded edited image.
    }
    this.userService
      .updateProfileImage(this.user, originalUserPic, editedUserPic)
      .subscribe(response => this.closeModal());
  }

  setNoPhoto() {
    this.userService
      .removeProfileImage(this.user)
      .subscribe(response => this.closeModal());
  }

  closeModal() {
    this.editUserModal.hide();
  }

  showModal() {
    this.editUserModal.show();
  }

  ngOnDestroy() {
    this.onUserEditViewChange$.unsubscribe();
    this.onSignedInUserChange$.unsubscribe();
  }
}