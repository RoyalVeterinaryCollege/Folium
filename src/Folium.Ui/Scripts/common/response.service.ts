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
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";

@Injectable()
export class ResponseService {
	/**
	 * Attempts to return body as parsed `JSON` object, or raises an exception.
	 */
	parseJson(response: Response) {
		return JSON.parse(response.text(), this.dateReviver);
	}

	private dateReviver(key, value) {
		if (typeof value === 'string') {
			let a = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})\.?(\d*)?Z$/.exec(value);
			if (a) {
				return new Date(Date.UTC(+a[1], +a[2] - 1, +a[3], +a[4],
					+a[5], +a[6], a[7] ? +a[7] : 0));
			}
		}
		return value;
	}
}