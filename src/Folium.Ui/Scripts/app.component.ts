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
import { Component, OnInit, OnDestroy, ViewContainerRef }       from "@angular/core";
import { ActivatedRoute, Router, NavigationEnd } from "@angular/router";
import { Subscription } from "rxjs/subscription";

import { SecurityService } from "./common/security.service";
import { UserService } from "./user/user.service";

@Component({
  selector: "my-app",
  templateUrl: "html/layout.html",
})

export class AppComponent implements OnInit, OnDestroy {
  isAuthenticated: boolean = false;

  private viewContainerRef: ViewContainerRef;
  private onSignInComplete$: Subscription;

  constructor(private router: Router, viewContainerRef: ViewContainerRef, private securityService: SecurityService, private userService: UserService) {
    // You need this small hack in order to catch application root view container ref
    // https://valor-software.com/ngx-bootstrap/index-bs4.html#/modals
    this.viewContainerRef = viewContainerRef;

    // To allow scrolling to an anchor.
    // If no anchor is specified we scroll to the top.
    // Workaround lifted from https://github.com/angular/angular/issues/6595#issuecomment-244232725
    router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        const tree = router.parseUrl(router.url);
        const elementId = tree.fragment ? "#" + tree.fragment : "body";
        const element = document.querySelector(elementId);
        if (element) { element.scrollIntoView(); }
      }
    });
  }

  ngOnInit() {
    // Regsiter a sign in with the user service when the security service is happy a user has signed in.
    this.onSignInComplete$ = this.securityService.onSignInComplete.subscribe(url => {
		this.userService.registerSignIn();
    });
  }

  ngOnDestroy() {
    this.onSignInComplete$.unsubscribe();
  }
}