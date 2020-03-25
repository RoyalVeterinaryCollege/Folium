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
import { Pipe, PipeTransform } from "@angular/core";
import { Observable } from "rxjs";
import { HttpClient, HttpHeaders } from "@angular/common/http"
import { SecurityService } from "../security.service";

@Pipe({ name: 'secureImage' })
export class SecureImagePipe implements PipeTransform {
  constructor(
    private http: HttpClient,
    private securityService: SecurityService) { }

  transform(url: string) {

    return new Observable<string | ArrayBuffer>((observer) => {
      observer.next('data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==');
      const { next, error } = observer;

      this.http.get(url, {
        headers: new HttpHeaders({
          'Authorization': `Bearer ${this.securityService.authenticationToken}`,
          'Content-Type': 'image/*',
        }), responseType: 'blob'
      }).subscribe(response => {
          const reader = new FileReader();
          reader.readAsDataURL(response);
          reader.onloadend = function () {
            observer.next(reader.result);
          };
        });
      return { unsubscribe() { } };
    });
  }
}
