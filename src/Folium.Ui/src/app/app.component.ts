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
import { Router, NavigationEnd } from "@angular/router";
import { MatDialog, MatDialogRef } from "@angular/material";

import { Subscription } from "rxjs";

import { SecurityService } from "./core/security.service";
import { UserService } from "./user/user.service";
import { DialogUserEditorComponent } from "./user/components/dialog-user-editor.component";
import { User } from "./core/dtos";

@Component({
  selector: "app-root",
  templateUrl: "app.component.html",
})

export class AppComponent implements OnInit, OnDestroy {  
  user: User;

  private viewContainerRef: ViewContainerRef;
  private onSignInComplete$: Subscription;
  private onUserEditViewChange$: Subscription;
  private signedInUser$: Subscription;
  private registerSignIn$: Subscription;
  private dialogRef: MatDialogRef<DialogUserEditorComponent>

  constructor(
    router: Router, 
    viewContainerRef: ViewContainerRef, 
    private securityService: SecurityService, 
    private userService: UserService,
    private dialog: MatDialog) {
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
    this.onUserEditViewChange$ = this.userService.onShowUserEditView.subscribe(show => {
      if (show) {
        this.dialogRef = this.dialog.open(DialogUserEditorComponent, { disableClose: true });
      } else {
        this.dialogRef.close();
      }
    });
  }

  ngOnInit() {
    // Regsiter a sign in with the user service when the security service is happy a user has signed in.
    this.onSignInComplete$ = this.securityService.onSignInComplete.subscribe(url => {
		  this.registerSignIn$ = this.userService.registerSignIn().subscribe(user => this.user = user);
    });
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => this.user = user);
  }

  ngOnDestroy() {
    this.onSignInComplete$.unsubscribe();
    this.onUserEditViewChange$.unsubscribe();
    this.signedInUser$.unsubscribe();
    if(this.registerSignIn$){
      this.registerSignIn$.unsubscribe();
    }
  }
}