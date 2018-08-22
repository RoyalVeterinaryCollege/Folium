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
using EventSaucing.Aggregates;
using Folium.Api.Models.Placement.Events;

namespace Folium.Api.Models.Placement {
	public class PlacementAggregate : Aggregate {
		public PlacementAggregate(Guid id) {
			base.Id = id;
		}
		public int UserId { get; set; }
		public string Title { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public string Reference { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedAt { get; set; }
		public int LastUpdatedBy { get; set; }
		public DateTime LastUpdatedAt { get; set; }
        public string Type { get; set; }

        public static string GetFullyQualifiedTitle(string title, string type, DateTime start, DateTime end) {
			return $"{(string.IsNullOrEmpty(type) ? "" : (type + ": "))}{title} ({start.ToString("d MMM yy")}-{end.ToString("d MMM yy")})";
		}

		private bool _isCreated;
		private bool _isRemoved;

		public void Create(int userId, string title, DateTime start, DateTime end, string reference, int createdBy, string type = null) {
			if (_isCreated || _isRemoved) return;
			RaiseEvent(new PlacementCreated(userId, title, start, end, reference, createdBy, DateTime.UtcNow, createdBy, DateTime.UtcNow, type));
		}
		public void Update(string title, DateTime start, DateTime end, string reference, int updatedBy, string type = null) {
			if (!_isCreated || _isRemoved) return;
			var originalFullyQualifiedTitle = GetFullyQualifiedTitle(Title, Type, Start, End);
			var newFullyQualifiedTitle = GetFullyQualifiedTitle(title, type, start, end);
			RaiseEvent(new PlacementUpdated(UserId, title, start, end, reference, CreatedBy, CreatedAt, updatedBy, DateTime.UtcNow, type));
			if (!originalFullyQualifiedTitle.Equals(newFullyQualifiedTitle)) {
				RaiseEvent(new PlacementNameUpdated(UserId, title, start, end, reference, CreatedBy, CreatedAt, updatedBy, DateTime.UtcNow, originalFullyQualifiedTitle, type));
			}
		}
		public void Remove(int updatedBy) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new PlacementRemoved(UserId, Title, Start, End, Reference, CreatedBy, CreatedAt, updatedBy, DateTime.UtcNow, Type));
		}

		#region Events

		void Apply(PlacementCreated @event) {
			_isCreated = true;
			UserId = @event.UserId;
			Title = @event.Title;
			Start = @event.Start;
			End = @event.End;
			Reference = @event.Reference;
			CreatedBy = @event.CreatedBy;
			CreatedAt = @event.CreatedAt;
			LastUpdatedBy = @event.LastUpdatedBy;
			LastUpdatedAt = @event.LastUpdatedAt;
            Type = @event.Type;
		}
		
		void Apply(PlacementUpdated @event) {
			Title = @event.Title;
			Start = @event.Start;
			End = @event.End;
			Reference = @event.Reference;
			LastUpdatedBy = @event.LastUpdatedBy;
			LastUpdatedAt = @event.LastUpdatedAt;
            Type = @event.Type;
		}
		void Apply(PlacementNameUpdated @event) {
			// noop.
		}
		void Apply(PlacementRemoved @event) {
			_isRemoved = true;
			LastUpdatedBy = @event.LastUpdatedBy;
			LastUpdatedAt = @event.LastUpdatedAt;
		}
		#endregion Events
	}
}      
