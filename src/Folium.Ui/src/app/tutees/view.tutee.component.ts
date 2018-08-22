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
	OnInit,
	OnDestroy
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";

import { Subscription } from "rxjs";
@Component({
	template: `
	<section class="title d-print-none">
		<div class="container"> 
		<div class="d-flex justify-content-start">
			<div class="mr-auto">
				<a [routerLink]="['/tutees']">
					<h1 class="text-uppercase p-1 m-0"><span class="folium-tutee small"></span> Tutees</h1> 
				</a>
			</div>
		</div>
		</div>
	</section>
  	<section class="content-main">
		<div class="container">
			<tutee [userId]='id' [courseId]='courseId'></tutee>
		</div>
	</section>
  `
})
export class ViewTuteeComponent implements OnInit, OnDestroy {	
	id: number;
	courseId: number;

	private paramsSubscription$: Subscription;

	constructor(
		private route: ActivatedRoute) { }

	ngOnInit() {
		this.paramsSubscription$ = this.route.paramMap.subscribe(params => {
			// Load the placement.
			this.id = +params.get('id');
			this.courseId = +params.get('courseId');
		});	  
	}

	ngOnDestroy() {
		this.paramsSubscription$.unsubscribe();
	}  
}