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
import { ActivatedRoute } from "@angular/router";

import { Subscription } from "rxjs";

import { Placement, User } from "../../core/dtos";
import { PlacementsService } from "../placements.service";
import { NotificationService } from "../../core/notification.service"

@Component({
  template: `
  <section class="title">
    <div class="container">
      <div class="d-flex justify-content-start">
        <a [routerLink]="['/placements']" routerLinkActive="active">
          <h1 class="text-uppercase p-1 m-0"> <span class="folium-placement small"></span> Placements </h1>
        </a>
      </div>
    </div>
  </section>
  <section class="content-main">
    <div class="container">
      <placement-viewer [user]="user" [placement]="placement"></placement-viewer>	
    </div>
  </section>`
})
export class ViewPlacementComponent implements OnInit, OnDestroy {
  placement: Placement;
  user: User;

  private paramsSubscription$: Subscription;

  constructor(
		private route: ActivatedRoute,
		private placementsService: PlacementsService,
		private notificationService: NotificationService) { }

  ngOnInit() {
	  this.paramsSubscription$ = this.route.paramMap.subscribe(params => {
			// Load the placement.
			this.loadPlacement(params.get('id'));
		});
    this.route.data.forEach((data: { currentUser: User }) => {
      this.user = data.currentUser;
    });
  }

  loadPlacement(id: string) {
    this.placementsService.getPlacement(id)
      .subscribe((placement: Placement) => {
        this.placement = placement;
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to load the placement, please try again.
          ${error}`));  
  }

  ngOnDestroy() {
		this.paramsSubscription$.unsubscribe();
  }
}