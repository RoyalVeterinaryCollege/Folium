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
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Folium.Api.Dtos {
    public class EntryDto {
        public Guid Id { get; set; }
		public int SkillSetId { get; set; }
		public string Title { get; set; }
		private string _description;
		public dynamic Description {
			get {
				return (EntryType == null || _description == null) ? _description : JsonConvert.DeserializeObject(_description);
			}
			set {
				if (value is string) {
					_description = value;
				}
				else {
					_description = JsonConvert.SerializeObject(value);
				}
			}
		}
		public Dictionary<int, SelfAssessmentDto> AssessmentBundle { get; set; }
		public string Where { get; set; }
		public DateTime When { get; set; }
		public UserDto Author { get; set; }
		public EntryTypeDto EntryType { get; set; }
		public DateTime LastUpdatedAt { get; set; }
		public List<EntryCommentDto> Comments { get; set; }
		public bool Shared { get; set; }
	}
}