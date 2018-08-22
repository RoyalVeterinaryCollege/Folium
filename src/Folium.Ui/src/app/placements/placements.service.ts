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
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";

import { Observable } from 'rxjs';

import { Placement, EntrySummary } from "../core/dtos";

@Injectable()
export class PlacementsService {
    private placementsUrl = "placements";

	constructor(private http: HttpClient) { }

	getPlacement(placementId: string): Observable<Placement> {
		return this.http.get<Placement>(`${this.placementsUrl}/${placementId}`);
	}

	getPlacements(userId: number, page: number, pageSize: number): Observable<Placement[]> {
		return this.http.get<Placement[]>(`${this.placementsUrl}?userId=${userId}&skip=${((page - 1) * pageSize)}&take=${pageSize}`);
	}

	createPlacement(placement: Placement): Observable<Placement> {
		return this.http.post<Placement>(this.placementsUrl + "/create", placement);
	}

	updatePlacement(placement: Placement): Observable<Placement> {
		return this.http.post<Placement>(`${this.placementsUrl}/${placement.id}/update`, placement);
	}

	removePlacement(placement: Placement) {
		return this.http.post<Placement>(`${this.placementsUrl}/${placement.id}/remove`, placement);
	}

	getEntries(placement: Placement, page: number, pageSize: number): Observable<EntrySummary[]> {
		return this.http.get<EntrySummary[]>(`${this.placementsUrl}/${placement.id}/entries?skip=${((page - 1) * pageSize)}&take=${pageSize}`);
	}
}
