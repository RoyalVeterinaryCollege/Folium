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
import { Request, XHRBackend, RequestOptions, Response, Http, RequestOptionsArgs, Headers } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { Router } from '@angular/router';
import { SecurityService } from "./security.service";
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/throw';
declare var appSettings;

@Injectable()
export class HttpService extends Http {
	private apiRootUri: string = appSettings.apiRootUri;

	constructor(backend: XHRBackend, defaultOptions: RequestOptions, private securityService: SecurityService) {
		super(backend, defaultOptions);
	}

	request(url: string | Request, options?: RequestOptionsArgs): Observable<Response> {
		if (typeof url === 'string') {
			url = this.getAbsoluteUrl(url);
			if (!options) {
				options = { headers: new Headers() };
			}
			this.setHeaders(options);
		} else {
			this.setHeaders(url);
		}

		return super.request(url, options).catch(this.catchErrors());
	}

	private catchErrors() {
		return (res: Response) => {
			if (res.status === 401 || res.status === 403) {
				// handle authorization errors
				this.securityService.signin();
				return;
			}
			return Observable.throw(res);
		};
	}

	private getAbsoluteUrl(url: string): string {
		if (url.slice(0,3).toLowerCase() !== ("http")) {
			// Append the api root address for any relative paths.
			return this.apiRootUri + url;
		}
		return url;
	}

	private setHeaders(objectToSetHeadersTo: Request | RequestOptionsArgs) {
		if (this.securityService.authenticationToken && this.securityService.authenticationToken.length > 0) {
			objectToSetHeadersTo.headers.append("Authorization", `Bearer ${this.securityService.authenticationToken}`);
		}
		objectToSetHeadersTo.url = this.getAbsoluteUrl(objectToSetHeadersTo.url);
	}
}