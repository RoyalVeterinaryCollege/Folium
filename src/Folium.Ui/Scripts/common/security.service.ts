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
import { Injectable, NgZone, EventEmitter, Injector } from "@angular/core";
import { Router } from "@angular/router";
import { HttpService } from "../common/http.service";
import { UserService } from "../user/user.service";
declare var appSettings;

@Injectable()
export class SecurityService {
    private oidcManager: Oidc.UserManager;
    private config = appSettings.oidcConfig;
    private signInRedirectUrl: string;
    private currentUser: Oidc.User;
    private signInRequested$: EventEmitter<string> = new EventEmitter<string>();
    private signInComplete$: EventEmitter<string> = new EventEmitter<string>();

	constructor(private injector: Injector, private ngZone: NgZone) {
        this.oidcManager = new Oidc.UserManager(this.config);
        window["angularComponentRef"] = {
            signInComplete: (user: Oidc.User) => this.signInComplete(user),
            oidcConfig: this.config
        };
    }

    get onSignInComplete() {
        return this.signInComplete$;
    }

    get onSignInRequested() {
        return this.signInRequested$;
    }

    get isAuthenticated(): Promise<boolean> {
        return this.oidcManager.getUser().then(user => {
            // Store the user.
            if (user && user.expired === false) {
                this.currentUser = user;
                return true;
            } else {
                return false;
            }
        });
    }

    get user() {
        return this.currentUser;
    }

    get authenticationToken() {
        return this.currentUser ? this.currentUser.access_token : "";
    }

    signin(url?: string) {
        // store the route to redirect too.
		this.signInRedirectUrl = url;
		// Pass some state which is used on the callback from external providers such as Google.
		window["angularComponentRef"].signInRedirectUrl = url;
		window["angularComponentRef"].apiRootUri = appSettings.apiRootUri;
        this.signInRequested$.emit(url);
    }

    signout() {
        return this.oidcManager.signoutRedirect().then(value => {
            // Reset the user.
            this.currentUser = undefined;
        });
    }

	// Need to use the injector service here, as this service is used in APP_INITIALIZER 
	// and taking a dependency on Router causes circular dependency issue.
	private get router(): Router {
		return this.injector.get(Router);
	}

    private signInComplete(user: Oidc.User) {
        this.currentUser = user;
        this.signInComplete$.emit(this.signInRedirectUrl);
        if(this.signInRedirectUrl){
            this.ngZone.run(() => {
                // Redirect to the route.
                this.router.navigateByUrl(this.signInRedirectUrl);
            });
        }
    }
}