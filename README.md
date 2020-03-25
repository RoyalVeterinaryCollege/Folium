# Folium
Plan Record Reflect Learn
---

This repository contains the source code for Folium, you can can find more about Folium at [http://folium.education/](http://folium.education/).

### Architecture

Folium.Ui is a [Single Page Application (SPA)](https://en.wikipedia.org/wiki/Single-page_application) which uses a lightweight [ASP.NET core MVC](https://github.com/aspnet/Mvc) web app to bootstrap an [Angular](https://github.com/angular/angular) based client app written in [Typescript](https://github.com/Microsoft/TypeScript). The Folium.Ui client app calls directly onto Folium.Api, a Web API built using ASP.NET Core MVC. Folium.Api implements an [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) pattern using [EventSaucing](https://github.com/RoyalVeterinaryCollege/EventSaucing) to store an event stream of aggregate changes. The app is secured using [IdentityServer](https://github.com/IdentityServer/IdentityServer4).

### Build

To create a build of the Folium app you will need to follow the following steps:

> Currently the Folium.Api must be run on a Windows OS (min Windows 7 SP1), as there is it a dependency on the full .NET Framework stack (via [EventSaucing](https://github.com/RoyalVeterinaryCollege/EventSaucing)), however Folium.Ui is cross platform.

1. Ensure you have .NET Core SDK installed [https://www.microsoft.com/net/core](https://www.microsoft.com/net/core)
2. Have Node.js installed [https://nodejs.org/en/download/](https://nodejs.org/en/download/)
3. Make sure you have the Angular CLI installed globally `npm install -g @angular/cli`
4. Once you have cloned the repo, navigate to Folium.Ui project directory and run `npm install`
5. Copy the [environment.template.ts](https://github.com/RoyalVeterinaryCollege/Folium/blob/master/src/Folium.Ui/src/environments/environment.template.ts) file and rename it `environment.ts` and update the template with the correct settings for your environment.
6. Copy the [appsettings.template.json](https://github.com/RoyalVeterinaryCollege/Folium/blob/master/src/Folium.Api/appsettings.template.json) file and rename it `appsettings.json ` and update the template with the correct settings for your environment.

To create a build, navigate to the Folium.Api folder and run `dotnet restore` to restore the nuget packages, then run `dotnet publish -c Release`. Navigate to the Folium.Ui folder and run `ng build --configuration=production`.

### Deploy

To deploy the app, first complete the build steps above and then complete the following:

> You need to host the app on a Windows Server (min Windows Server 2008 R2 SP1), we recommend using IIS.

1. Follow this [guide](https://docs.microsoft.com/en-us/aspnet/core/publishing/iis) to install ASP.NET Core on Windows with IIS.
2. You will need a SQL Server instance (min SQL Server 2012), [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-editions-express) will do fine
3. Run all the scripts in the [SQL-Scripts](SQL-Scripts) folder
4. Install and configure [IdentityServer](https://github.com/IdentityServer/IdentityServer4)
	> We found identityserver app pool must have 'Load User Profile' in Advanced Settings and the identity needs permissions to read its private key via mmc snap in -> All Tasks -> Manage Private Keys.
5. Copy the Folium.Api and Folium.Ui build outputs (publish folders) into two seperate web applications
6. Install a Skill set (see below)

### Skill set install

You will need to install at least one Skill Set to use the app, [here's how](Skills-Import/readme.md)

### Slim Image Cropper

[Slim Image Cropper](http://slimimagecropper.com/) is a 3rd party library that is used to edit and updoad users' profile pics, it is not a requirement, but does dramatically improve the user experience. However, the use of the library does requires you to purchase a license.

Once a license is purchased you can overwrite the contents of Folium.Ui/lib/slim with the corresponding javascript and css from the library.

### Students, Placements and Tutor Groups

In our setup we have developed some small integration tasks that sync the students, placements and tutor groups from our student records system into Folium. The Folium.Api exposes a number of end points which allow for these placements to be sync'd, we then modify the User and Tutee tables directly (this should also be accomplished via the api in the future). If you would like further info then please get in touch.
We recommend using [Hangfire](https://www.hangfire.io/) to schedule and run your tasks.

### Entry Attachments

Folium supports attaching files to an Entry or a comment. It uses [Uppy](https://github.com/transloadit/uppy) to manage the front end file uploads and takes advantage of the [tus](https://tus.io/) protocol to provide resumable uploads. Once uploaded the files are securly stored so that only the Entry author or anyone they have shared with are able to access the files.
The TinyMce editor is also configured to handle image uploads, which are embeded into the content. These files are uploaded outside of the tus protocol and are not secured.

### Audio & Video Encoding

Audio and video files are supported as part of the above Entry Attachment functionality. Once these files are uploaded Folium uses the [coconut](https://coconut.co/) encoding service to process these files and provide both thumbnails of videos and re-encoded files in mp4 and webm. This allows Folium to play these files in-situ on most desktop and mobile web browsers.
This is an optional feature and can be toggled using the `EncodeVideoFiles` flag in the `appsettings.json` file.

### Entry Sign-off

By using a custom Entry Type (as shown below) Folium provides an Entry 'sign-off' workflow. Once an Entry is created which supports sign-off the user has the option to request another user (or tutor, if configured) to review their Entry and 'sign it off'. Once a request has been made an email notification will be sent to the requested user who will be able to view the entry and choose to 'sign-off'. Once an Entry is signed off it is made read-only so no changes can be made, but comments are still available.

### Entry Types

You are able to setup different types of Entries which can be created in Folium. These are setup by creating a new entry in the `EntryType` table using the following json:

```json
{
	"summary": "<p>Some html summaty text for the entry type</p>",
	"skillGroupingId": 3,
	"skillBundleIds": [5,25,31,44,142,147,150,156,166],
	"inputs":[{"title":"Title of the section","help":"Any help text for the section"},],
	"signOff":{
		"allowedBy": "anyone|tutor",
		"text":"Some text to display when requesting a sign off"
	}
}
```
summary - (Optional) A summary for the type of entry.

skillGroupingId - (Optional) The id of the initial skill grouping to use for the entry.

skillBundleIds - (Optional) The ids of the Skill bundles to automatically include when the entry is created.

inputs - (Required) 1 or more sections which can be completed on the entry.

signOff - (Optional) If the entry can be signed off.

allowedBy - (Required) Who is authorised to sign off the entry, either anyone or just the users tutors. Course admin are also authorised to sign off if the entry is shared with them.

text - (Optional) Text to be shown when a sign-off is requested.

### Contributing to Folium

If you would like to get involved and develop and contribute to the project, then great, we welcome pull requests :) 
Some points to help you get setup and running, please shout if we have forgot anything!

> Currently the Folium.Api must be run on a Windows OS (min Windows 7 SP1), as there is it a dependency on the full .NET Framework stack (via EventSaucing), however Folium.Ui is cross platform.

* Following the Build steps above locally
* Our prefered development environment is [Visual Studio Code](https://code.visualstudio.com/)
* In the Folium.Ui project there are a number of predefined Visual Studio tasks which in run the various ng tasks, or you can run them directly from the command line e.g. `npm start` will serve the UI project using `ng serve` this will automatically watch for changes to the code and reload the page.

### Dependencies

Folium has been built with the help of these superb librarys

- [ASP.NET Core MVC](https://github.com/aspnet/Mvc)
- [Dapper](https://github.com/StackExchange/Dapper)
- [ImageMagick](https://github.com/dlemstra/Magick.NET)
- [MailKit](https://github.com/jstedfast/MailKit)
- [Autofac](https://github.com/autofac/Autofac)
- [Serilog](https://github.com/serilog/serilog)
- [EventSaucing](https://github.com/RoyalVeterinaryCollege/EventSaucing)
- [TypeScript](https://github.com/Microsoft/TypeScript)
- [Angular](https://github.com/angular/angular)
- [Material](https://github.com/angular/components)
- [Bootstrap](https://github.com/twbs/bootstrap)
- [ngx-bootstrap](https://github.com/valor-software/ngx-bootstrap)
- [ngx-charts](https://github.com/swimlane/ngx-charts)
- [TinyMce](https://github.com/tinymce/tinymce)
- [RxJS](https://github.com/ReactiveX/rxjs)
- [oidc-client](https://github.com/IdentityModel/oidc-client-js)
- [hammer.js](https://github.com/hammerjs/hammer.js)
- [node.js](https://github.com/nodejs/node)
- [npm](https://github.com/npm/npm)
- [sass](https://github.com/sass/sass)
- [systemjs](https://github.com/systemjs/systemjs)
- [IdentityServer](https://github.com/IdentityServer/IdentityServer4)
- [Hangfire](https://github.com/HangfireIO/Hangfire)
- [Uppy](https://github.com/transloadit/uppy)
- [ngx-plyr](https://github.com/smnbbrv/ngx-plyr)
- [tusdotnet](https://github.com/tusdotnet/tusdotnet)

Crossbrowser testing sponsored by [Browser Stack](https://www.browserstack.com)

[<img src="https://www.browserstack.com/images/layout/browserstack-logo-600x315.png" alt="Browser Stack" height="100px">](https://www.browserstack.com)
