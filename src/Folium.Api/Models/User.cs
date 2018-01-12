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

namespace Folium.Api.Models {
    /// <summary>
    /// Represents a User.
    /// </summary>
    public class User {
        public int Id { get; set; } 
        public string Email { get; set; } 
        public int? SkillsBundlesTaxonomyId { get; set; } 
        public int? EntryTagsTaxonomyId { get; set; } 
        public bool HasProfilePic { get; set; } 
        public int ProfilePicVersion { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime LastSignIn { get; set; }
		public List<CourseEnrolment> Courses { get; set; }
        public bool HasTutees { get; set; }
        public bool HasTutor { get; set; }
        public bool IsSystemUser {
            get { return Id == -1; }
        }
        public ActivitySummary ActivitySummary { get; set; }
    }
}      
