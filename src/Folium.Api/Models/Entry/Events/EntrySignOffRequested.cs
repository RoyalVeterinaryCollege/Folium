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

namespace Folium.Api.Models.Entry.Events {
	internal class EntrySignOffRequested {
		public EntrySignOffRequested(List<int> authorisedUserIds, DateTime when, string message) {
            AuthorisedUserIds = authorisedUserIds;
			When = when;
            Message = message;
		}
        /// <summary>
        /// The users who have been authorised to sign-off the entry.
        /// </summary>
		public List<int> AuthorisedUserIds { get; }
        /// <summary>
        /// When the request was made.
        /// </summary>
		public DateTime When { get; }
        /// <summary>
        /// The optional message to send to the users requested tosign-off the entry.
        /// </summary>
        public string Message { get; }
    }
}