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
import { Response, Http } from "@angular/http";
import { ResponseService } from "../common/response.service";

import 'rxjs/add/operator/publishReplay';

import { User } from "../dtos";
import { HttpService } from "../common/http.service";
import { Observable } from "rxjs/Observable";
import { ReplaySubject } from "rxjs/ReplaySubject";

@Injectable()
export class UserService {
    private userUrl = "users/";
	private _signedInUser$: ReplaySubject<User> = new ReplaySubject<User>(1);
	private _signedInUser:User;
    private showUserEditView$: EventEmitter<boolean> = new EventEmitter<boolean>();

    constructor(private http: Http, private responseService: ResponseService) { }

	get signedInUser(): Observable<User> {
        if (!this._signedInUser) {
            // Refresh the current user if we don"t have one.
            this.refreshUser();
        }
		return this._signedInUser$.asObservable();
    }
	
    get onShowUserEditView(): EventEmitter<boolean> {
        return this.showUserEditView$;
    }

    getUsersTutors(users: User, courseId: number): Observable<User[]> {
		return this.http.get(`${this.userUrl}current/courses/${courseId}/tutors`)
			.map((res: Response) => this.responseService.parseJson(res));
	}
    
    registerSignIn() {
        // Register that a profile has logged in.
        return this.http.get(this.userUrl + "sign-in")
            .map(response => {
				let user = this.responseService.parseJson(response) as User;
                this._signedInUser$.next(user);
                return user;
			})
			.subscribe(_ => _);
    }

	updateProfileImage(user: User, originalUserPic: any, editedUserPic: any): Observable<User> {
        let formData = new FormData();
        if (originalUserPic) {
            formData.append("originalPic", originalUserPic);
        }
        if (editedUserPic) {
            formData.append("editedPic", editedUserPic);
        }

	    return this.http
		    .post(`${this.userUrl}${user.id}/profile-image`, formData)
		    .map(response => {
				let updatedUser = this.responseService.parseJson(response).result as User;
				this._signedInUser$.next(updatedUser);
			    return user;
		    });
    }

	removeProfileImage(user: User): Observable<User> {
	    return this.http
		    .post(`${this.userUrl}${user.id}/profile-image`, "")
		    .map(response => {
				let updatedUser = this.responseService.parseJson(response).result as User;
				this._signedInUser$.next(updatedUser);
			    return user;
		    });
    }

    showUserEditView() {
        this.showUserEditView$.emit(true);
    }

    hideUserEditView() {
        this.showUserEditView$.emit(false);
    }

	private refreshUser() {
        this.http.get(this.userUrl + "current")
            .map(response => {
				let user = response.json() as User;
				this._signedInUser$.next(user);
			})
			.subscribe(_ => _);
    }
}
