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
import { Component, Inject } from "@angular/core"
import { MAT_DIALOG_DATA } from "@angular/material/dialog";

@Component({
    selector: "dialog-confirm",
    template: `
    <div mat-dialog-title>
        <h1>Just checking...</h1>
    </div>
    <div mat-dialog-content class="pt-3" [innerHtml]="data.body | safeHtml"></div>
    <div mat-dialog-actions class="justify-content-between">
        <button class="btn btn-secondary btn-shadow" mat-dialog-close="false">No</button>
        <button class="btn btn-primary btn-shadow" mat-dialog-close="true">Yes</button>
    </div>`
})
export class DialogConfirmComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: any) { }
}
