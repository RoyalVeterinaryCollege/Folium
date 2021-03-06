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

namespace Folium.Api.Dtos.Reporting {
    public class EntryEngagementResultSetDto {
		public EntryEngagementCriteriaDto Criteria { get; set; }
        public List<EntryEngagementResultDto> DataSet { get; set; }
        public List<ReportUserDto> Users { get; set; }
    }
    public class EntryEngagementResultDto {
        public int UserId { get; set; }
        public int EntryTypeId { get; set; }
        public DateTime When { get; set; }
        public int SharedCount { get; set; }
        public int SharedWithTutorCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsSignOffCompatible { get; set; }
        public int SignOffRequestCount { get; set; }
        public bool SignedOff { get; set; }
    }
}