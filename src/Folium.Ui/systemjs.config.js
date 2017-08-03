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
(function (global) {
  System.config({
    // map tells the System loader where to look for things
    map: {
      app: 'js',
      // angular bundles  
      '@angular/core': 'lib/@angular/core/bundles/core.umd.js',
      '@angular/common': 'lib/@angular/common/bundles/common.umd.js',
      '@angular/compiler': 'lib/@angular/compiler/bundles/compiler.umd.js',
      '@angular/platform-browser': 'lib/@angular/platform-browser/bundles/platform-browser.umd.js',
      '@angular/animations': 'lib/@angular/animations/bundles/animations.umd.js',
      '@angular/animations/browser': 'lib/@angular/animations/bundles/animations-browser.umd.js',
      '@angular/platform-browser/animations': 'lib/@angular/platform-browser/bundles/platform-browser-animations.umd.js',   
      '@angular/platform-browser-dynamic': 'lib/@angular/platform-browser-dynamic/bundles/platform-browser-dynamic.umd.js',
      '@angular/http': 'lib/@angular/http/bundles/http.umd.js',
      '@angular/router': 'lib/@angular/router/bundles/router.umd.js',
      '@angular/forms': 'lib/@angular/forms/bundles/forms.umd.js',
      // material design
      '@angular/material': 'lib/@angular/material/bundles/material.umd.js',
      // other libraries
      'rxjs':'lib/rxjs',
      'moment':'lib/moment/moment.js',
      'ngx-bootstrap':'lib/ngx-bootstrap'
    },
    // packages tells the System loader how to load when no filename and/or no extension
    packages: {
      app: {
        main: './main.js',
        defaultExtension: 'js'
      },
      rxjs: {
        defaultExtension: 'js'
      },
      'ngx-bootstrap': { 
        format: 'cjs', 
        main: 'bundles/ngx-bootstrap.umd.js',
        defaultExtension: 'js'
      },
    }
  });
})(this);