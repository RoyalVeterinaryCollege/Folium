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

namespace Folium.Api.Models.Placement.Events {
    public abstract class PlacementEvent {
	    protected PlacementEvent(int userId, string title, DateTime start, DateTime end, string reference, int createdBy, DateTime createdAt, int lastUpdatedBy, DateTime lastUpdatedAt, string type = null) {
			UserId = userId;
			Title = title;
			Start = start;
			End = end;
			Reference = reference;
			CreatedBy = createdBy;
			CreatedAt = createdAt;
			LastUpdatedBy = lastUpdatedBy;
			LastUpdatedAt = lastUpdatedAt;
            Type = type;
		}
		public int UserId { get; }
		public string Title { get; }
		public DateTime Start { get; }
		public DateTime End { get; }
		public string Reference { get; }
		public int CreatedBy { get; }
		public DateTime CreatedAt { get; }
		public int LastUpdatedBy { get; }
		public DateTime LastUpdatedAt { get; }
		public string FullyQualifiedTitle => PlacementAggregate.GetFullyQualifiedTitle(Title, Type, Start, End);
        public string Type { get; }
	}
}      
