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
using System.IO;
using EventSaucing.Aggregates;
using Folium.Api.Models.File.Events;
using Folium.Api.Models.Placement.Events;

namespace Folium.Api.Models.File {
	public class FileAggregate : Aggregate {
		public FileAggregate(Guid id) {
			base.Id = id;
		}
		public int CreatedBy { get; set; }
		public DateTime CreatedAt { get; set; }
        public string Filename { get; set; }
        public string FilePath { get; set; }
        public string Type { get; set; }
		public long Size { get; set; }

		private bool _isCreated;
		private bool _isRemoved;

		public void Create(int createdBy, string filename, string filePath, string type, long size) {
			if (_isCreated || _isRemoved) return;

			RaiseEvent(new FileCreated(createdBy, DateTime.UtcNow, filename, filePath, type, size));
		}

		public void Remove() {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new FileRemoved(CreatedBy, CreatedAt, Filename, FilePath, Type, Size));
		}

		#region Events

		void Apply(FileCreated @event) {
			_isCreated = true;
			CreatedBy = @event.CreatedBy;
            CreatedAt = @event.CreatedAt;
            Filename = @event.Filename;
            Type = @event.Type;
		}

		void Apply(FileRemoved @event) {
			_isRemoved = true;
		}
		#endregion Events
	}
}      
