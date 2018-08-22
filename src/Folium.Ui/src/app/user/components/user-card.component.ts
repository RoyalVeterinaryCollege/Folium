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
import { Component, Input, SimpleChanges, OnChanges, SimpleChange } from "@angular/core";

import { UserService } from "../user.service";
import { User } from "../../core/dtos";

@Component({
  selector: "user-card",
  templateUrl: "user-card.component.html",
})
export class UserCardComponent implements OnChanges {
  
  @Input()
  user: User;
  
  tutors: TutorGroup[];

  constructor(
    private userService: UserService) { }

  ngOnChanges(changes: SimpleChanges) {
    // Check if the user has changed.
    const user: SimpleChange = changes.user;
    if (this.user && user && (!user.previousValue || (user.previousValue.id !== user.currentValue.id))) {
      this.loadTutors();
    }
  }
  
  loadTutors() {
    if(!this.user) return; // If no user then don't try to load anything else.
    if (this.user.hasTutor && this.user.courses && this.user.courses.length > 0) {
      this.tutors = [];
      for(let x = 0; x < this.user.courses.length; x++) {
          this.userService.getUsersTutors(this.user.id, this.user.courses[x].courseId)
            .subscribe(tutors => {
              let group = new TutorGroup();
              group.title = `${this.user.courses[x].title} Year ${this.user.courses[x].year}`;
              group.tutors = tutors;
              this.tutors = [ ...this.tutors, group ];
            });
      }
    } else {
      this.tutors = []; // no tutors.
    }
  }
}

class TutorGroup {
  title: string;
  tutors: User[];
}