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

namespace Folium.Api.Models.Entry.Events {
    public class EntryCreated {
		public EntryCreated(int skillSetId, string title, string description, int userId, string where, DateTime when, DateTime createdAt, DateTime lastUpdatedAt) {
			SkillSetId = skillSetId;
			Description = description;
			Title = title;
			UserId = userId;
			Where = where;
			When = when;
			CreatedAt = createdAt;
			LastUpdatedAt = lastUpdatedAt;
		}
		public int SkillSetId { get; }
		public string Title { get; }
		public string Description { get; }
		public int UserId { get; }
		public string Where { get; }
		public DateTime When { get; }
		public DateTime CreatedAt { get; }
		public DateTime LastUpdatedAt { get; }
	}
}      
