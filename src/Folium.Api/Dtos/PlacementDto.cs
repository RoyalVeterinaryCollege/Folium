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

namespace Folium.Api.Dtos {
    public class PlacementDto {
		public Guid Id { get; set; }
		public int UserId { get; set; }
		public string Title { get; set; }
		public string FullyQualifiedTitle { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public string Reference { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedAt { get; set; }
		public int LastUpdatedBy { get; set; }
		public DateTime LastUpdatedAt { get; set; }
		public int EntryCount { get; set; }
        public string Type { get; set; }
    }
}