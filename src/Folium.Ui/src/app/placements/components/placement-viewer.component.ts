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
import { Component, Input } from "@angular/core";

import { Placement, User } from "../../core/dtos";

@Component({
  selector: "placement-viewer",
  template: `
  <!-- Loading... -->
  <div *ngIf="!placement" class="loader-container">
    <div class="loader primary">Loading...</div>
  </div>
  <!-- Placement... -->
  <div *ngIf="placement">
    <div class="row">
      <div class="col">
        <h2 class="mt-2">{{placement.title}} <small class="text-muted">{{placement.start | date: 'd MMM yyyy'}} - {{placement.end | date: 'd MMM yyyy'}}</small></h2>	
      </div>
    </div>			
    <!-- Entries... -->
    <entries-viewer [user]="user" [placement]="placement" [sharedEntriesOnly]="sharedEntriesOnly"></entries-viewer>
  </div>`
})
export class PlacementViewerComponent {
  @Input()
  placement: Placement;

  @Input()
  user: User;

  @Input()
  sharedEntriesOnly: boolean;
}
