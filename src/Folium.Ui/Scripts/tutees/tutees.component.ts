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
	OnInit
} from "@angular/core";

import { UserService } from "../user/user.service";
import { User, TuteeGroup } from "../dtos";
import { ActivatedRoute } from "@angular/router";

@Component({
    templateUrl: "html/tutees/tutees.component.html",
    selector: "tutees"
})
export class TuteesComponent implements OnInit {
    tuteeGroups: TuteeGroup[];

	constructor(
		private userService: UserService,
		private route: ActivatedRoute) { }

	ngOnInit() {
		this.loadTuteeGroups();
	}

    private loadTuteeGroups(){
        this.userService.getUsersTuteeGroups().subscribe(groups => this.tuteeGroups = groups);
    }
}