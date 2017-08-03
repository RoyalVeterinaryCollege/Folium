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
    Directive,
    AfterViewInit,
    OnDestroy,
    Input
} from "@angular/core";
import { FormGroup } from "@angular/forms";
import { Observable } from 'rxjs/Observable';
import { Subject } from "rxjs/Subject";
import { Subscription } from "rxjs/subscription";

@Directive({
    selector: "[fmAutoSave]"
})

export class FormAutoSaveDirective implements AfterViewInit, OnDestroy {

    @Input()
    set formGroup(formGroup: FormGroup) {
        formGroup.valueChanges.subscribe(changes => {
            this.change$.next(changes)
        });
    }

    @Input("fmAutoSave")
    onSave: Function;

    private change$: Subject<any> = new Subject<any>();
    private onChange$: Subscription;

    constructor() {
    }

    ngAfterViewInit(): void {
        this.onChange$ = this.change$.debounceTime(5000).subscribe(change => {
            this.onSave();
        });
    }
    
    ngOnDestroy(): void {
        this.onChange$.unsubscribe();
    }
}