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
  Input,
  OnInit,
  OnDestroy
} from "@angular/core";
import { Subscription, Observable } from "rxjs";
import { NotificationService, Alert } from "../notification.service";

@Component({
    selector: "notifications",
    host: {
      "class": "notifications-wrapper"
    },
    template: ` <alert 
                  *ngFor="let alert of notifications;let i = index" 
                  (close)="closeAlert(i)"
                  [type]="alert.type" 
                  dismissible="true"
                  dismissOnTimeout="{{alert.type === 'danger' ? undefined : 5000}}">
                  <i class="fa" [ngClass]="{'fa-exclamation-circle': alert.type === 'danger', 'fa-check': alert.type === 'success'}" aria-hidden="true"></i>
                  {{ alert?.message }}
                </alert>
              `
})
export class NotificationsComponent implements OnInit, OnDestroy {
  notifications: Array<Alert> = [];
  private subscription: Subscription;

  constructor(private notificationService: NotificationService) { }

  ngOnInit() {
    this.subscription = this.notificationService.notifications.subscribe(a => this.notifications.push(a));
  }

  closeAlert(i: number) {
    this.notifications.splice(i, 1);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}
