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
using System;
using System.Collections.Generic;
using Folium.Api.Models;
using System.Linq;

namespace Folium.Api.Dtos {
    public class UserDto {
        public int Id { get; set; } 
        public string Email { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
	    public string Pic => HasProfilePic ? $"{Id}_{ProfilePicVersion}.jpg" : null;
		public DateTime LastSignIn { get; set; }
		public List<CourseEnrolmentDto> Courses { get; set; }
		public bool HasProfilePic { get; set; }
		public int ProfilePicVersion { get; set; }
        public bool HasTutees { get; set; }
        public bool HasTutor { get; set; }
        public int TotalEntries { get; set; }
        public int TotalSelfAssessments { get; set; }
        public int TotalPlacements { get; set; }
        public int TotalEntriesSharedWithYou { get; set; }

        public UserDto() { }

        public UserDto(User user) {
            if(user == null) return;

            Id = user.Id;
            Email = user.Email;
            FirstName = user.FirstName + (string.IsNullOrEmpty(user.MiddleNames) ? "" : " " + user.MiddleNames);
            LastName = user.LastName;
            LastSignIn = user.LastSignIn;
	        Courses = user.Courses == null ? new List<CourseEnrolmentDto>() : user.Courses.Select(c => new CourseEnrolmentDto(c)).ToList();
	        HasProfilePic = user.HasProfilePic;
	        ProfilePicVersion = user.ProfilePicVersion;
            HasTutees = user.HasTutees;
            HasTutor = user.HasTutor;
            TotalEntries = user.ActivitySummary == null ? 0 : user.ActivitySummary.TotalEntries;
            TotalSelfAssessments = user.ActivitySummary == null ? 0 : user.ActivitySummary.TotalSelfAssessments;
            TotalPlacements = user.ActivitySummary == null ? 0 : user.ActivitySummary.TotalPlacements;
        }
    }
}      
