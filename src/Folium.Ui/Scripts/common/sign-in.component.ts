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
import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from "@angular/core";
import { ModalDirective } from "ngx-bootstrap";

import { Subscription } from "rxjs/subscription";

import { SecurityService } from "./security.service";

@Component({
  selector: "modal-sign-in",
  templateUrl: "html/sign-in.component.html",
})
export class SignInComponent implements OnInit, OnDestroy {
  private onSignInRequested$: Subscription;
  private onSignInComplete$: Subscription;

  @ViewChild("signInModal")
  public signInModal: ModalDirective;

  @ViewChild("signInFrame")
  public signInFrame: ElementRef;

  constructor(private securityService: SecurityService) { }

  ngOnInit() {
    this.onSignInRequested$ = this.securityService.onSignInRequested.subscribe(url => this.showModal());
    this.onSignInComplete$ = this.securityService.onSignInComplete.subscribe(url => this.hideModal());
  }

  hideModal() {
    this.signInModal.hide();
  }

  showModal() {
    this.signInFrame.nativeElement.src = "/html/sign-in.html";
    this.signInModal.show();
  }

  ngOnDestroy() {
    this.onSignInRequested$.unsubscribe();
    this.onSignInComplete$.unsubscribe();
  }
}