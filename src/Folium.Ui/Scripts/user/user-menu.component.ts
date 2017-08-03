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
import { Component, OnInit, OnDestroy } from "@angular/core";
import { ModalDirective } from "ngx-bootstrap";

import { Subscription } from "rxjs/subscription";

import { UserService } from "./user.service";
import { SecurityService } from "./../common/security.service";
import { User } from "./../dtos";

@Component({
  selector: "user-menu",
  templateUrl: "html/user/user-menu.component.html",
})
export class UserMenuComponent implements OnInit, OnDestroy {
  user: User;

  private onSignedInUserChange$: Subscription;

  constructor(private userService: UserService, private securityService: SecurityService) { }

  ngOnInit() {
	this.onSignedInUserChange$ = this.userService.signedInUser.subscribe(user =>
	  this.user = user);
  }

  editProfile() {
    // TODO: Move this into its own user profile component.
    this.userService.showUserEditView();
  }

  logOut() {
    // TODO: Move this into its own user profile component.
    this.securityService.signout();
  }

  ngOnDestroy() {
    this.onSignedInUserChange$.unsubscribe();
  }
}