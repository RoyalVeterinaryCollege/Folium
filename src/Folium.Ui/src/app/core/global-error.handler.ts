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
import { ErrorHandler, Injectable } from '@angular/core';

import { NotificationService } from "./notification.service";

@Injectable()
export class GlobalErrorHandler extends ErrorHandler {

  constructor(private notificationService: NotificationService) {
    super();
  }

  handleError(error) {
    super.handleError(error);

    // https://github.com/swimlane/ngx-charts/issues/1212
    // Swallow the error for now.
    if (error.message === 'Unable to get property \'name\' of undefined or null reference') {
      // DO NOTHING
      return;
    }
    this.notificationService.addDanger(`An unhandled error occured, please try again:
      ${error}`);
  }
}
