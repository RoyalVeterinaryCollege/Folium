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
import { ModalDirective } from "ngx-bootstrap";

import { Subscription } from "rxjs/subscription";

import { UserService } from "./user.service";
import { User } from "./../dtos";

@Component({
  selector: "user-card",
  templateUrl: "html/user/user-card.component.html",
})
export class UserCardComponent implements OnInit, OnDestroy {

  user: User;
  tutors: User[];

  private signedInUser$: Subscription;

  constructor(
    private userService: UserService) { }

  ngOnInit() {
	  this.signedInUser$ = this.userService.signedInUser.subscribe(user => {
      this.user = user;
      if(!this.user) return; // If no user then don't try to load anything else.
      if (this.user.courses && this.user.courses.length > 0) {
        this.userService.getUsersTutors(this.user, this.user.courses[0] /* TODO: We need to have a course selector if > 1 course */).subscribe(tutors => this.tutors = tutors);
      } else {
        this.tutors = []; // no tutors.
      }
    });
  }

  ngOnDestroy() {
	  this.signedInUser$.unsubscribe();
  }
}