// This file can be replaced during build by using the `fileReplacements` array.
// `ng build ---prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
  production: false,
  apiRootUri: 'http://hhsrvmars.rvc.ac.uk:5003/',
  oidcConfig: {
      authority: "http://hhsrvmars.rvc.ac.uk:5001/",
      client_id: "folium_app_dev",
      redirect_uri: "http://localhost:8080/html/callback.html",
      post_logout_redirect_uri: "http://localhost:8080",
      response_type: "token id_token",
      scope: "openid profile email folium_app_api",
      silent_redirect_uri: "http://localhost:8080/html/sign-in-silent.html",
      automaticSilentRenew: true,
      accessTokenExpiringNotificationTime: 1000
  }
};

/*
 * In development mode, to ignore zone related error stack frames such as
 * `zone.run`, `zoneDelegate.invokeTask` for easier debugging, you can
 * import the following file, but please comment it out in production mode
 * because it will have performance impact when throw error
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
