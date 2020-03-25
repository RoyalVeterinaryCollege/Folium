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
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse, HttpSentEvent, HttpHeaderResponse, HttpProgressEvent, HttpResponse, HttpUserEvent } from '@angular/common/http';

import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { catchError, switchMap, filter, take } from "rxjs/operators";

import { SecurityService } from './security.service';

/**
 * Provide custom Authorization capabilities for api requests.
 * @export
 * @class AuthorizationInterceptor
 */
@Injectable()
export class AuthorizationInterceptor implements HttpInterceptor {

  private isAuthenticating: boolean = false;
  private tokenSubject: BehaviorSubject<string> = new BehaviorSubject<string>(null);
  
	constructor(private securityService: SecurityService) {
    this.securityService.onSignInComplete.subscribe(url => this.tokenSubject.next(this.securityService.authenticationToken));
  }

  /**
   * Custom http request interceptor
   * @public
   * @param {HttpRequest<any>} request
   * @param {HttpHandler} next
   * @returns {Observable<HttpEvent<any>>}
   * @memberof JsonInterceptor
   */
  public intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpSentEvent | HttpHeaderResponse | HttpProgressEvent | HttpResponse<any> | HttpUserEvent<any>> {
    return next.handle(this.addAuthorizationToken(request, this.securityService.authenticationToken))
      .pipe(
          catchError(
              (error: any, caught: Observable<HttpEvent<any>>) => {
                if (error instanceof HttpErrorResponse) {
                  switch ((<HttpErrorResponse>error).status) {
                      case 400:
                      case 401:
                      case 403:
                          return this.handleAuthError(request, next);
                  }
                }
                return throwError(error);
              }
          )
      );
  }
  
  addAuthorizationToken(req: HttpRequest<any>, token: string): HttpRequest<any> {
    return req.clone({ setHeaders: { Authorization: `Bearer ${token}` }})
  }

  handleAuthError(request: HttpRequest<any>, next: HttpHandler) {
    if (!this.isAuthenticating) {
      this.isAuthenticating = true;

      // Reset here so that the following requests wait until the token
      // comes back after re-authenticating.
      this.tokenSubject.next(null);

      this.securityService.signin();
    }
    return this.tokenSubject.pipe(
      filter(token => token != null),
      take(1),
      switchMap(token => {
        this.isAuthenticating = false;
        return next.handle(this.addAuthorizationToken(request, token));
      })
    )
  }
}
