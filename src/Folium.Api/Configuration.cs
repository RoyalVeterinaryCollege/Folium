
using System;
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
namespace Folium.Api {
    public class Configuration {
        public string UiBaseUrl { get; set; }
        public string OidcAuthority { get; set; }
        public bool OidcRequireHttpsMetadata { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public DateTime IgnoreEmailNotificationsBefore { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpAccountUser { get; set; }
        public string SmtpAccountName { get; set; }
        public bool SmtpRequiresAuthentication { get; set; }
        public string SmtpAccountPassword { get; set; }
        public bool SmtpUseSsl { get; set; }
        public string TusUrlPath { get; set; }
        public string TusFiles { get; set; }
        public bool EncodeVideoFiles { get; set; }
        public string CoconutAPIKey { get; set; }
        public string CoconutApiUrl { get; set; }
        public string CoconutWebHookBaseUrl { get; set; } // If running localhost use https://ngrok.com/
    }
}