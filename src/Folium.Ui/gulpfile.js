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
/// <binding Clean='clean' />
'use strict';

var gulp = require('gulp'),
    tsc = require('gulp-typescript'),
    sourcemaps = require('gulp-sourcemaps'),
    concat = require('gulp-concat'),
    uglify = require('gulp-uglify'),
    del = require('del'),
    sass = require('gulp-sass'),
    plumber = require('gulp-plumber'),
    path = require('path'),
    newer = require('gulp-newer'),
    sysBuilder = require('systemjs-builder'),
    colors  = require('colors'),
    runSequence = require('run-sequence'),
    tslint = require('gulp-tslint'),    
    sassLint = require('gulp-sass-lint'),
    cleanCSS = require('gulp-clean-css'),
    gulpif = require('gulp-if');

var webroot = './wwwroot/';
var approot = './';
var npmroot = './node_modules/';
var production = false;

var tscConfig = require(approot + "scripts/tsconfig.json");

// Clean the js distribution directory
gulp.task('clean:dist:js', function () {
  return del(webroot + 'js');
});

// Clean the css distribution directory
gulp.task('clean:dist:css', function () {
  return del(webroot + 'css');
});

// Clean library directory
gulp.task('clean:lib', function () {
  return del(webroot + 'lib');
});

// Clean assets
gulp.task('clean:assets', function () {  
  return del([
        webroot + 'fonts/**',
        webroot + 'html/**',
        webroot + 'images/**',
        '!' + webroot + 'images',
        '!' + webroot + 'images/profiles/**'
    ]);
});

// Compile TypeScript to JS
gulp.task('compile:ts', function () {      
  // copy slim
  gulp.src([
      approot + 'lib/slim/slim.commonjs.min.js'
  ],  {base: approot + 'lib/'})
  .pipe(gulp.dest(webroot + 'js/'));

  var tsProject = tsc.createProject(approot + 'Scripts/tsconfig.json', { 
        typescript: require('typescript')
  });
  return tsProject
    .src()
    .pipe(gulpif(!production, sourcemaps.init()))
    .pipe(plumber({
      errorHandler: function (err) {
        console.error('>>> [tsc] Typescript compilation failed'.bold.green);
        this.emit('end');
      }}))
    .pipe(tsc(tsProject))
    .pipe(gulpif(!production, sourcemaps.write()))
    .pipe(gulp.dest(webroot + 'js'));
});

// Lint Typescript
gulp.task('lint:ts', function() {
  return gulp.src(approot + 'scripts/**/*.ts')
    .pipe(tslint({
      formatter: "verbose"
    }))
    .pipe(tslint.report({ 
        emitError: false 
    }));
});

// Generate systemjs-based builds
gulp.task('bundle:js', function() {
  var builder = new sysBuilder('wwwroot', './systemjs.config.js');
  builder.bundle('app', webroot + 'js/app.min.js', {
    sourceMaps: production ? false : 'inline'
    })
    .catch(function(err) {
      console.error('>>> [systemjs-builder] Bundling app failed'.bold.green, err);
    });
  builder.bundle('js/entries/entries.module - js/main.js', webroot + 'js/entries/entries.module.js', {
    sourceMaps: production ? false : 'inline'
    })
    .catch(function(err) {
      console.error('>>> [systemjs-builder] Bundling entries module failed'.bold.green, err);
    });
  builder.bundle('js/placements/placements.module - js/main.js', webroot + 'js/placements/placements.module.js', {
    sourceMaps: production ? false : 'inline'
    })
    .catch(function(err) {
      console.error('>>> [systemjs-builder] Bundling placements module failed'.bold.green, err);
    });
  return builder.bundle('js/skills/skills.module - js/main.js', webroot + 'js/skills/skills.module.js', {
    sourceMaps: production ? false : 'inline'
    })
    .then(function () {
      return del([
        webroot + 'js/**/*', 
        '!' + webroot + 'js/app.min.*', 
        '!' + webroot + 'js/entries{,/entries.module.js}', 
        '!' + webroot + 'js/skills{,/skills.module.js}', 
        '!' + webroot + 'js/placements{,/placements.module.js}' ]);
    })
    .catch(function(err) {
      console.error('>>> [systemjs-builder] Bundling skills module failed'.bold.green, err);
    });
});

// Minify JS bundle
gulp.task('minify:js', function() {
  return gulp
    .src(webroot + 'js/**/*')
    .pipe(uglify())
    .pipe(gulp.dest(webroot + 'js'));
});

// Minify JS bundle
gulp.task('minify:vendors:js', function() {
  return gulp
    .src(webroot + 'js/vendors.min.js')
    .pipe(uglify())
    .pipe(gulp.dest(webroot + 'js'));
});

// Lint Sass
gulp.task('lint:sass', function() {
  return gulp.src('src/**/*.scss')
    .pipe(plumber({
      errorHandler: function (err) {
        console.error('>>> [sass-lint] Sass linting failed'.bold.green);
        this.emit('end');
      }}))
    .pipe(sassLint())
    .pipe(sassLint.format())
    .pipe(sassLint.failOnError());
});

// Compile SCSS to CSS, concatenate, and minify
gulp.task('compile:sass', function () {
  // concat and minify global scss files
  gulp
    .src(approot + 'Styles/**/*.scss')
    .pipe(plumber({
      errorHandler: function (err) {
        console.error('>>> [sass] Sass global style compilation failed'.bold.green);
        console.error(err);
        this.emit('end');
      }}))
    .pipe(gulpif(!production, sourcemaps.init()))
    .pipe(sass({ errLogToConsole: true }))
    .pipe(concat('site.min.css'))
    .pipe(cleanCSS())
    .pipe(gulpif(!production, sourcemaps.write()))
    .pipe(gulp.dest(webroot + 'css'));
});

// Copy dependencies
gulp.task('copy:libs', function() {
    gulp.src([
        npmroot + 'oidc-client/dist/oidc-client.min.js',
        npmroot + 'core-js/client/shim.min.js',
        npmroot + 'zone.js/dist/zone.js',
        npmroot + 'reflect-metadata/Reflect.js',
        npmroot + 'systemjs/dist/system.src.js',
        npmroot + 'hammerjs/hammer.min.js',
        "systemjs.config.js",
        npmroot + 'tinymce/tinymce.min.js',
        npmroot + 'tinymce/themes/**/*.min.js',
        npmroot + 'intl/dist/Intl.min.js'
    ],  {base: './node_modules/'})
    .pipe(gulpif(production, concat('vendors.min.js')))
    .pipe(gulp.dest(webroot + 'lib/'));

    // copy source maps
    gulp.src([
        npmroot + 'es6-shim/es6-shim.map',
        npmroot + 'reflect-metadata/Reflect.js.map',
        npmroot + 'systemjs/dist/system-polyfills.js.map',
        npmroot + 'hammerjs/hammer.min.js.map',
        npmroot + 'intl/dist/Intl.min.js.map'
    ],  {base: './node_modules/'})
    .pipe(gulp.dest(webroot + 'lib/'));

    return gulp.src([
        npmroot + '@angular/**/*.js',
        npmroot + 'rxjs/**/*.js',
        npmroot + 'moment/moment.js',
        npmroot + 'ngx-bootstrap/**/*.js',        
        npmroot + 'tinymce/skins/**/*.*',
        npmroot + 'tinymce/plugins/autoresize/plugin.min.js',
        npmroot + 'oidc-client/dist/oidc-client.min.js' // this script is concatinated and copied over so we can just reference it on a single page when needed.
    ],  {base: './node_modules/'})
    .pipe(gulp.dest(webroot + 'lib/'));
});

// Copy static assets
gulp.task('copy:assets', function() {
  return gulp.src([
        approot + 'fonts/**',
        approot + 'html/**',
        approot + 'images/**'
    ], {base: approot})
    .pipe(gulp.dest(webroot));
});

gulp.task('app:sync', function() {
	return gulp.src([
        approot + 'fonts/**',
        approot + 'html/**',
        approot + 'images/**'
	], { base: approot })
    .pipe(plumber({
        handleError: function (err) {
            console.log(err);
            this.emit('end');
        }
    }))
    .pipe(newer(webroot))
    .pipe(gulp.dest(webroot));
});

gulp.task('watch', function () {
  gulp.watch('Styles/**/*.scss', {cwd: approot}, ['styles']);
  gulp.watch('Scripts/**/*.ts', {cwd: approot}, ['scripts']);
  var syncWatcher = gulp.watch(['fonts/**/*', 'html/**/*', 'images/**/*'], {cwd: approot}, ['app:sync']);
  syncWatcher.on('change', function(ev) {
        if(ev.type === 'deleted') {
            // path.relative gives us a string where we can easily switch
        	// directories
        	var wwwrootPath = ev.path.replace('html', path.join('wwwroot', 'html'));
        	wwwrootPath = wwwrootPath.replace('fonts', path.join('wwwroot', 'fonts'));
        	wwwrootPath = wwwrootPath.replace('images', path.join('wwwroot', 'images'));
        	del(path.relative('./', wwwrootPath));
        }
    });
});

gulp.task('lint', ['lint:ts', 'lint:sass']);

gulp.task('clean', ['clean:dist:js', 'clean:dist:css', 'clean:lib', 'clean:assets']);

gulp.task('copy', function(callback) {
  runSequence('clean:lib', 'copy:libs', 'clean:assets', 'copy:assets', callback);
});

gulp.task('scripts', function(callback) {
  if(production){
    runSequence(['lint:ts', 'clean:dist:js'], 'compile:ts', 'bundle:js', ['minify:js', 'minify:vendors:js'], callback);
  } else{
    runSequence(['lint:ts', 'clean:dist:js'], 'compile:ts', callback);
  }
});

gulp.task('styles', function(callback) {
  runSequence(['lint:sass', 'clean:dist:css'], 'compile:sass', callback);
});

gulp.task('build', function(callback) {
  runSequence('copy', 'scripts', 'styles', callback);
});

gulp.task('publish', function(callback) {
  production = true;
  runSequence('copy', 'scripts', 'styles', callback);
});

gulp.task('default', function(callback) {
  runSequence('build', callback);
});