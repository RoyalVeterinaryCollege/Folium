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
using System.Collections.ObjectModel;
using EventSaucing.Aggregates;
using Folium.Api.Models.Entry.Events;

namespace Folium.Api.Models.Entry {
	public class EntryAggregate : Aggregate {
		public EntryAggregate(Guid id) {
			base.Id = id;
		}
		public int SkillSetId { get; set; }
		public string Description { get; private set; } 
        public string Title { get; private set; }
		public string Where { get; private set; }
		public DateTime When { get; private set; }
		public int UserId { get; private set; } 
        public DateTime CreatedAt { get; private set; } 
        public DateTime LastUpdatedAt { get; private set; }
		public int? TypeId { get; private set; }
		private readonly List<Dictionary<int, SelfAssessment>> _assessmentBundle = new List<Dictionary<int, SelfAssessment>>();

		/// <summary>
		/// Gets all the assessment bundles, the most recent of which will be the last.
		/// </summary>
		public ReadOnlyCollection<Dictionary<int, SelfAssessment>> AssessmentBundle => _assessmentBundle.AsReadOnly();

		private bool _isCreated;
		private bool _isRemoved;

		public void Create(int skillSetId, string title, string description, int userId, string where, DateTime when, int? entryTypeId = null) {
			if (_isCreated || _isRemoved) return;
			RaiseEvent(new EntryCreated(skillSetId, title, description, userId, where, when, DateTime.UtcNow, DateTime.UtcNow));
			if (entryTypeId.HasValue) {
				RaiseEvent(new EntryCreatedWithType(entryTypeId.Value));
			}
		}
		public void Update(string title, string description, string where, DateTime when) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new EntryUpdated(title, description, where, when, DateTime.UtcNow));
		}
		public void Remove() {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new EntryRemoved());
		}
		public void AddAssessmentBundle(Dictionary<int, SelfAssessment> selfAssessments) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new AssessmentBundleAdded(selfAssessments));
		}
		public void UpdateAssessmentBundle(Dictionary<int, SelfAssessment> selfAssessments) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new AssessmentBundleUpdated(selfAssessments));
		}

		#region Events

		void Apply(EntryCreated @event) {
			_isCreated = true;
			SkillSetId = @event.SkillSetId;
			Title = @event.Title;
			Description = @event.Description;
			Where = @event.Where;
			When = @event.When;
			UserId = @event.UserId;
			CreatedAt = @event.CreatedAt;
			LastUpdatedAt = @event.LastUpdatedAt;
		}

		void Apply(EntryCreatedWithType @event) {
			TypeId = @event.TypeId;
		}

		void Apply(AssessmentBundleAdded @event) {
			_assessmentBundle.Add(@event.SelfAssessments);
		}
		void Apply(EntryUpdated @event) {
			Title = @event.Title;
			Description = @event.Description;
			Where = @event.Where;
			When = @event.When;
			LastUpdatedAt = @event.LastUpdatedAt;
		}
		void Apply(EntryRemoved @event) {
			_isRemoved = true;
		}
		void Apply(AssessmentBundleUpdated @event) {
			_assessmentBundle.Clear();
			_assessmentBundle.Add(@event.SelfAssessments);
		}
		#region Deprecated Events
		void Apply(SkillsBundleAdded @event) {
			_assessmentBundle.Add(@event.SelfAssessments);
		}

		void Apply(SkillsBundleUpdated @event) {
			_assessmentBundle.Clear();
			_assessmentBundle.Add(@event.SelfAssessments);
		}

		#endregion Deprecated Events

		#endregion Events
	}
}      
