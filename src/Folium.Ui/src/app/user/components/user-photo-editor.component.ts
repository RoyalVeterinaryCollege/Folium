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
import { Component, ViewChild, ElementRef, Input, Output, EventEmitter } from "@angular/core";

import { UserService } from "../user.service";
import { User } from "../../core/dtos";

declare var Slim: any;

@Component({
  selector: "user-photo-editor",
  templateUrl: "user-photo-editor.component.html",
})
export class UserPhotoEditorComponent {
  @ViewChild("originalUserPic", { static: true })
  public originalUserPic: ElementRef;

  @ViewChild("editedUserPic", { static: true })
  public editedUserPic: ElementRef;

  @ViewChild("slimImageCropper", { static: false })
  public slimImageCropper: ElementRef;

  @Input()
  user: User;
  
	@Output()
	onDone = new EventEmitter();

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

  constructor(private userService: UserService) { }

  onSaveClick() {
    let originalUserPic = this.originalUserPic.nativeElement.files[0];
    let editedUserPic = this.editedUserPic.nativeElement.value;
    // Only send the edited pic if we have it.
    if (editedUserPic) {
      originalUserPic = undefined;
      editedUserPic = (JSON.parse(editedUserPic)).output.image; // Get just the base64 encoded edited image.
    }
    this.userService
      .updateProfileImage(this.user, originalUserPic, editedUserPic)
      .subscribe(response => this.done());
  }

  onNoPhotoClick() {
    this.userService
      .removeProfileImage(this.user)
      .subscribe(response => this.done());
  }

  onCancelClick() {
    this.done();
  }
  
  private done() {
    this.onDone.emit();
  }
}