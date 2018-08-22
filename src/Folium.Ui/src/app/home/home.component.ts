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

import { Subscription } from "rxjs";

import { User } from "../core/dtos";
import { UserService } from "../user/user.service";

@Component({
  templateUrl: "home.component.html",
})
export class HomeComponent implements OnInit, OnDestroy {
  profileCardFlipped: boolean;
  user: User;

  private signedInUser$: Subscription;
  
  constructor(
    private userService: UserService) { }

  ngOnInit() {
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => {
      this.user = user;
    });
  }

  flipProfileCard() {
    this.profileCardFlipped = !this.profileCardFlipped;
  }

  ngOnDestroy() {
	  this.signedInUser$.unsubscribe();
  }
}