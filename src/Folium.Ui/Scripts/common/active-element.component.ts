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
import { Component, Output, EventEmitter } from "@angular/core"
@Component({
    moduleId: module.id,
    selector: "active-element",
    template: `
		<div style="position:relative;">			
			<div (click)="backgroundClick.emit()" style="z-index:100; position:fixed; top:0; left:0; right:0; bottom: 0; background: rgba(0,0,0,.6); opacity: 0.4;">
			</div>
			<div style="position: relative; z-index:101">
				<ng-content></ng-content>	
			</div>
		</div>`
})
export class ActiveElementComponent {
  constructor() { }

	@Output()
	backgroundClick = new EventEmitter();	
}
