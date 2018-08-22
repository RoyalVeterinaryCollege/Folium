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
import { HttpClient } from '@angular/common/http';

import { Observable, ReplaySubject } from "rxjs";
import { tap, map, publishReplay, refCount } from 'rxjs/operators';

import { User, CollaboratorOption, SkillSet, TuteeGroup } from "../core/dtos";

@Injectable()
export class UserService {
    private userUrl = "users";
	private user: ReplaySubject<User> = new ReplaySubject<User>(1);
    private _showUserEditView: EventEmitter<boolean> = new EventEmitter<boolean>();
    private _signedInUser$: Observable<User>
    private userSkillSets: { [id: number]: Observable<SkillSet[]>; } = {};

    constructor(private http: HttpClient) { }

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
        return this.http.get<User>(`${this.userUrl}/${id}`);
    }

	getUsers(query: string): Observable<CollaboratorOption[]> {
		return this.http.get<CollaboratorOption[]>(`${this.userUrl}?q=${query}`);
	}

    getUsersTutors(userId: number, courseId: number): Observable<User[]> {
		return this.http.get<User[]>(`${this.userUrl}/${userId}/courses/${courseId}/tutors`);
	}

    getUsersTuteeGroups(): Observable<TuteeGroup[]> {
		return this.http.get<TuteeGroup[]>(`${this.userUrl}/current/tutees`);
	}
    
    registerSignIn(): Observable<User> {
        // Register that a profile has logged in.
        return this.http.get<User>(this.userUrl + "/sign-in").pipe(
            tap((user: User) => this.user.next(user))
        );
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
		    .post<User>(`${this.userUrl}/${user.id}/profile-image`, formData).pipe(
                tap((updatedUser: User) => this.user.next(updatedUser))
            );
    }

	removeProfileImage(user: User): Observable<User> {
	    return this.http
		    .post<User>(`${this.userUrl}/${user.id}/profile-image`, "").pipe(
                tap((updatedUser: User) => this.user.next(updatedUser)),
                map(u => user)
            );
    }
    
    getSkillSets(user: User): Observable<SkillSet[]> {
        // Return the cached value if available.
        if (!this.userSkillSets[user.id]) {
            this.userSkillSets[user.id] = this.http.get<SkillSet[]>(`${this.userUrl}/${user.id}/skill-sets`).pipe(
                tap((skillSets: SkillSet[]) => {
                    if(skillSets.length == 1) {
                        // Set the skill set as selected, if there is only 1.
                        skillSets[0].selected = true;						
                    }
                    return skillSets;
                }),
                publishReplay(1),
                refCount()
            );
        }
        return this.userSkillSets[user.id];
    }
	
	addUserSkillSet(user: User, skillSetId: number): Observable<any> {
		return this.http.post(`${this.userUrl}/current/skill-sets/add/${skillSetId}`, { }).pipe(
            tap(_ => this.userSkillSets[user.id] = undefined) // invalidate the cache.;
        );
	}

	removeUserSkillSet(user: User, skillSetId: number): Observable<any> {
		return this.http.post(`${this.userUrl}/current/skill-sets/remove/${skillSetId}`, { }).pipe(
            tap(_ => this.userSkillSets[user.id] = undefined) // invalidate the cache.;
        );
    }
    
    showUserEditView() {
        this._showUserEditView.emit(true);
    }

    hideUserEditView() {
        this._showUserEditView.emit(false);
    }

	private refreshUser() {
        this.http.get<User>(this.userUrl + "/current").pipe(
            tap((user: User) => {
                if(user) {
                    this.user.next(user);
                }
            })
        ).subscribe(_ => _);
    }
}
