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

import { Observable } from "rxjs/Observable";
import { ReplaySubject } from "rxjs/ReplaySubject";
import 'rxjs/add/operator/publishReplay';

import { User, CollaboratorOption, SkillSet, TuteeGroup } from "../dtos";

@Injectable()
export class UserService {
    private userUrl = "users";
	private user: ReplaySubject<User> = new ReplaySubject<User>(1);
    private _showUserEditView: EventEmitter<boolean> = new EventEmitter<boolean>();
    private _signedInUser$: Observable<User>
    private userSkillSets: { [id: number]: Observable<SkillSet[]>; } = {};

    constructor(private http: Http, private responseService: ResponseService) { }

	get signedInUser(): Observable<User> {
        if (!this._signedInUser$) {
            this._signedInUser$ = this.user.asObservable();
            // Refresh the current user.
            this.refreshUser();
        }
		return this.user.asObservable();
    }
	
    get onShowUserEditView(): EventEmitter<boolean> {
        return this._showUserEditView;
    }
    
    getUser(id: number): Observable<User> {
        return this.http.get(`${this.userUrl}/${id}`)
            .map((res: Response) => this.responseService.parseJson(res));
    }

	getUsers(query: string): Observable<CollaboratorOption[]> {
		return this.http.get(`${this.userUrl}?q=${query}`)
			.map((res: Response) => this.responseService.parseJson(res));
	}

    getUsersTutors(userId: number, courseId: number): Observable<User[]> {
		return this.http.get(`${this.userUrl}/${userId}/courses/${courseId}/tutors`)
			.map((res: Response) => this.responseService.parseJson(res));
	}

    getUsersTuteeGroups(): Observable<TuteeGroup[]> {
		return this.http.get(`${this.userUrl}/current/tutees`)
			.map((res: Response) => this.responseService.parseJson(res));
	}
    
    registerSignIn(): Observable<User> {
        // Register that a profile has logged in.
        return this.http.get(this.userUrl + "/sign-in")
            .map(response => {
				let user = this.responseService.parseJson(response) as User;
                this.user.next(user);
                return user;
			});
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
		    .post(`${this.userUrl}/${user.id}/profile-image`, formData)
		    .map(response => {
				let updatedUser = this.responseService.parseJson(response).result as User;
				this.user.next(updatedUser);
			    return user;
		    });
    }

	removeProfileImage(user: User): Observable<User> {
	    return this.http
		    .post(`${this.userUrl}/${user.id}/profile-image`, "")
		    .map(response => {
				let updatedUser = this.responseService.parseJson(response).result as User;
				this.user.next(updatedUser);
			    return user;
		    });
    }
    
    getSkillSets(user: User): Observable<SkillSet[]> {
        // Return the cached value if available.
        if (!this.userSkillSets[user.id]) {
            this.userSkillSets[user.id] = this.http.get(`${this.userUrl}/${user.id}/skill-sets`)
                .map(response => {
                    let skillSets = this.responseService.parseJson(response) as SkillSet[];
                    if(skillSets.length == 1) {
                        // Set the skill set as selected, if there is only 1.
                        skillSets[0].selected = true;						
                    }
                    return skillSets;
                })
                .publishReplay(1)
                .refCount();
        }
        return this.userSkillSets[user.id];
    }
	
	addUserSkillSet(user: User, skillSetId: number): Observable<Response> {
		return this.http.post(`${this.userUrl}/current/skill-sets/add/${skillSetId}`, { })
			.do(_ => this.userSkillSets[user.id] = undefined); // invalidate the cache.
	}

	removeUserSkillSet(user: User, skillSetId: number): Observable<Response> {
		return this.http.post(`${this.userUrl}/current/skill-sets/remove/${skillSetId}`, { })
			.do(_ => this.userSkillSets[user.id] = undefined); // invalidate the cache.
    }
    
    showUserEditView() {
        this._showUserEditView.emit(true);
    }

    hideUserEditView() {
        this._showUserEditView.emit(false);
    }

	private refreshUser() {
        this.http.get(this.userUrl + "/current")
            .do(response => {
                let user = response.json() as User;
                if(user) {
                    this.user.next(user);
                }
			})
			.subscribe(_ => _);
    }
}
