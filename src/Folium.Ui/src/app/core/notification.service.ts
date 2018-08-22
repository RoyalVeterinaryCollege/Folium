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
import { Injectable, EventEmitter } from "@angular/core";

@Injectable()
export class NotificationService {
    private notifications$: EventEmitter<Alert> = new EventEmitter<Alert>();

    constructor() { }

    get notifications() {
        return this.notifications$;
    }

    addSuccess(message: string) {
        this.addNotification(new Alert("success", message));
    }

    addInfo(message: string) {
        this.addNotification(new Alert("info", message));
    }

    addWarning(message: string) {
        this.addNotification(new Alert("warning", message));
    }

    addDanger(message: string) {
        this.addNotification(new Alert("danger", message));
    }

    addMessage(message: string, type: string) {
        this.addNotification(new Alert(type, message));
    }

    private addNotification(alert: Alert) {
        this.notifications$.emit(alert);
    }
}

export class Alert {
    constructor(public type: string, public message: string) { }
}
