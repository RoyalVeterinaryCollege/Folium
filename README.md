# Folium
Plan Record Reflect Learn
---

This repository contains the source code for Folium, you can can find more about Folium at [coming soon].

### Architecture

Folium.Ui is a [Single Page Application (SPA)](https://en.wikipedia.org/wiki/Single-page_application) which uses a lightweight [ASP.NET core MVC](https://github.com/aspnet/Mvc) web app to bootstrap an [Angular](https://github.com/angular/angular) based client app written in [Typescript](https://github.com/Microsoft/TypeScript). The Folium.Ui client app calls directly onto Folium.Api, a Web API built using ASP.NET Core MVC. Folium.Api implements an [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) pattern using [EventSaucing](https://github.com/RoyalVeterinaryCollege/EventSaucing) to store an event stream of aggregate changes. The app is secured using [IdentityServer](https://github.com/IdentityServer/IdentityServer4).

### Build

To create a build of the Folium app you will need to follow the following steps:

> Currently the Folium.Api must be run on a Windows OS (min Windows 7 SP1), as there is it a dependency on the full .NET Framework stack (via EventSaucing), however Folium.Ui is cross platform.

1. Ensure you have .NET Core SDK installed [https://www.microsoft.com/net/core](https://www.microsoft.com/net/core)
2. Have Node.js installed [https://nodejs.org/en/download/](https://nodejs.org/en/download/)
3. Make sure you have gulp installed globally `npm install -g gulp-cli`
4. Once you have cloned the repo, navigate to Folium.Ui project directory and run `npm install`

To create a build, navigate to the Folium.Api folder and run `dotnet restore` to restore the nuget packages, then run `dotnet publish -c Release`. Navigate to the Folium.Ui folder and run `gulp publish; dotnet publish -c Release`.

### Deploy

To deploy the app, first complete the build steps above and then complete the following:

> You need to host the app on a Windows Server (min Windows Server 2008 R2 SP1), we recommend using IIS.

1. Follow this [guide](https://docs.microsoft.com/en-us/aspnet/core/publishing/iis) to install ASP.NET Core on Windows with IIS.
2. You will need a SQL Server instance (min SQL Server 2012), [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-editions-express) will do fine
3. Run all the scripts in the SQL Scripts folder
4. Install and configure [IdentityServer](https://github.com/IdentityServer/IdentityServer4)
	> We found identityserver app pool must have 'Load User Profile' in Advanced Settings and the identity needs permissions to read its private key via mmc snap in -> All Tasks -> Manage Private Keys.
5. Copy the Folium.Api and Folium.Ui build outputs (publish folders) into two seperate web applications
6. You need to set your appsettings, copy the appsettings.template.json in both projects and paste as appsettings.json, update the template with the correct settings for your environment
7. Install a Skill set (see below)

### Skill set install

You will need to install at least one Skill Set to use the app, details of how to do this can be found at (https://github.com/RoyalVeterinaryCollege/Folium/tree/master/Skills Import)[https://github.com/RoyalVeterinaryCollege/Folium/tree/master/Skills%20Import]

### Slim Image Cropper

[Slim Image Cropper](http://slimimagecropper.com/) is a 3rd party library that is used to edit and updoad users' profile pics, it is not a requirement, but does dramatically improve the user experience. However, the use of the library does requires you to purchase a license.

Once a license is purchased you can overwrite the contents of Folium.Ui/lib/slim with the corresponding javascript and css from the library.

### Students, Placements and Tutor Groups

In our setup we have developed some small integration tasks that sync the students, placements and tutor groups from our student records system into Folium. The Folium.Api exposes a number of end points which allow for these placements to be sync'd, we then modify the User and Tutee tables directly (this should also be accomplished via the api in the future). If you would like further info then please get in touch.
We recommend using [Hangfire](https://www.hangfire.io/) to schedule and run your tasks.

### Contributing to Folium

If you would like to get involved and develop and contribute to the project, then great, we welcome pull requests :) 
Some points to help you get setup and running, please shout if we have forgot anything!

> Currently the Folium.Api must be run on a Windows OS (min Windows 7 SP1), as there is it a dependency on the full .NET Framework stack (via EventSaucing), however Folium.Ui is cross platform.

* Following the Build steps above locally
* Our prefered development environment is [Visual Studio Code](https://code.visualstudio.com/)
* In the Folium.Ui project there are a number of predefined Visual Studio tasks which in run the various gulp tasks, or you can run them directly from the command line.
* There is a gulp watch task which will automatically transpile the typescript, sass files and sync the files to the wwwroot folder. You should run this task in the background whilst developing Folium.Ui, there is also a watch.bat and watch.command which can be run directly from the relevant OS.


