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

// Credit: https://stackoverflow.com/a/47872396
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from "rxjs/operators";

// https://github.com/angular/angular/blob/master/packages/common/http/src/xhr.ts#L18
const XSSI_PREFIX = /^\)\]\}',?\n/;

/**
 * Provide custom json parsing capabilities for api requests.
 * @export
 * @class JsonInterceptor
 */
@Injectable()
export class JsonInterceptor implements HttpInterceptor {

  /**
   * Custom http request interceptor
   * @public
   * @param {HttpRequest<any>} request
   * @param {HttpHandler} next
   * @returns {Observable<HttpEvent<any>>}
   * @memberof JsonInterceptor
   */
  public intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
	  if (request.responseType !== 'json') {
      return next.handle(request);
    }
    // convert to responseType of text to skip angular parsing
    request = request.clone({
      responseType: 'text'
    });

	return next.handle(request).pipe(
		map(event => {
			// Pass through everything except for the final response.
			if (!(event instanceof HttpResponse)) {
				return event;
			}
			return this.processJsonResponse(event);
    	}));
  }

  /**
   * Parse the json body using custom revivers.
   * @private
   * @param {HttpResponse<string>} response
   * @returns{HttpResponse<any>}
   * @memberof JsonInterceptor
   */
  private processJsonResponse(response: HttpResponse<string>): HttpResponse<any> {
      let body = response.body;
      if (typeof body === 'string') {
        const originalBody = body;
        body = body.replace(XSSI_PREFIX, '');
        try {
          body = body !== '' ? JSON.parse(body, (key: any, value: any) => this.reviveUtcDate(key, value)) : null;
        } catch (error) {
          // match https://github.com/angular/angular/blob/master/packages/common/http/src/xhr.ts#L221
          throw new HttpErrorResponse({
            error: { error, text: originalBody },
            headers: response.headers,
            status: response.status,
            statusText: response.statusText,
            url: response.url || undefined,
          });
        }
      }
      return response.clone({ body });
  }

  /**
   * Detect a date string and convert it to a date object.
   * @private
   * @param {*} key json property key.
   * @param {*} value json property value.
   * @returns {*} original value or the parsed date.
   * @memberof JsonInterceptor
   */
  private reviveUtcDate(key: any, value: any): any {
      if (typeof value !== 'string') {
          return value;
      }
      if (value === '0001-01-01T00:00:00') {
          return null;
      }
      const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*)?)Z$/.exec(value);
      if (!match) {
          return value;
      }
      return new Date(value);
  }
}