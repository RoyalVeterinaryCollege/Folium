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
import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { User, Placement } from "../../core/dtos";

@Component({
  template: `
  <section class="title">
    <div class="container">
      <div class="d-flex justify-content-start">
        <h1 class="text-uppercase p-1 m-0"><span class="folium-placement small"></span> Placements </h1>				
      </div>
    </div>
  </section>
  <section class="content-main">
    <div class="container" *ngIf="user">
      <!-- List Placements... -->
      <placements-viewer [user]="user" (onViewPlacement)="onViewPlacement($event)"></placements-viewer>
    </div>
  </section>`
})
export class ViewPlacementsComponent implements OnInit {
  user: User;

  constructor(
    private route: ActivatedRoute,
    private router: Router) { }
  
  ngOnInit() {
    this.route.data.forEach((data: { currentUser: User }) => {
      this.user = data.currentUser;
    });
  }

  onViewPlacement(placement: Placement) {
    this.router.navigate(['/placements', placement.id]);
  }
}