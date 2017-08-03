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
using System.Linq;
using EventSaucing.Aggregates;
using Folium.Api.Extensions;
using Folium.Api.Models.SelfAssessing.Events;

namespace Folium.Api.Models.SelfAssessing {
	public class SelfAssessingAggregate : Aggregate {
		public SelfAssessingAggregate(Guid id) {
			base.Id = id;
		}
		public int UserId { get; private set; }
		public int SkillSetId { get; private set; }
		public DateTime CreatedAt { get; private set; }
		// self assessments are indexed by the skill id they refer to and then ordered by the epoch time they were created.
		private readonly Dictionary<int, SortedList<double, SelfAssessment>> _selfAssessments = new Dictionary<int, SortedList<double, SelfAssessment>>();

		/// <summary>
		/// Gets the most recent self assessments, indexed by Skill Id.
		/// </summary>
		public ReadOnlyDictionary<int, SelfAssessment> SelfAssessments => _selfAssessments.Select(i => i.Value.Values.LastOrDefault()).ToDictionary(i => i.SkillId).ToReadOnly();
		
		public void Create(int userId, int skillSetId) {
			RaiseEvent(new SelfAssessingStarted(userId, skillSetId, DateTime.UtcNow));
		}
		public void AddSelfAssessment(int skillSetId, SelfAssessment selfAssessment) {
			// Check if the skill already has a self assessment.
			if (!_selfAssessments.ContainsKey(selfAssessment.SkillId)) {
				RaiseEvent(new SkillSelfAssessmentCreated(skillSetId, selfAssessment));
				// If this is a self assessment associated with an entry then raise an event for that.
				if (selfAssessment.EntryId.HasValue) {
					RaiseEvent(new EntrySelfAssessmentCreated(skillSetId, selfAssessment));
				}
			} else {
				var skillSelfAssessments = _selfAssessments[selfAssessment.SkillId];
				// Check if the assessment is more recent than the current or the same time (this can happen if attached to an entry).
				if (skillSelfAssessments.LastOrDefault().Key <= selfAssessment.CreatedAt.ToUnixTimeMilliseconds()) {
					RaiseEvent(new SkillSelfAssessmentUpdated(skillSetId, selfAssessment));
				}
				// If this is a self assessment associated with an entry then raise an event for that.
				if (selfAssessment.EntryId.HasValue) {
					if (skillSelfAssessments.ContainsKey(selfAssessment.CreatedAt.ToUnixTimeMilliseconds())) {
						RaiseEvent(new EntrySelfAssessmentUpdated(skillSetId, selfAssessment));
					}
					else {
						RaiseEvent(new EntrySelfAssessmentAdded(skillSetId, selfAssessment));
					}
				}
			}
		}
		/// <summary>
		/// Removes the self assessment and returns the most recent self assessment.
		/// </summary>
		/// <param name="skillSetId"></param>
		/// <param name="selfAssessment"></param>
		/// <returns></returns>
		public SelfAssessment RemoveSelfAssessment(int skillSetId, SelfAssessment selfAssessment) {
			// Only self assessments associated with entries can be removed.
			if (!selfAssessment.EntryId.HasValue) return null;
			RaiseEvent(new EntrySelfAssessmentRemoved(skillSetId, selfAssessment));

			// Get the skill self assessments.
			if (!_selfAssessments.ContainsKey(selfAssessment.SkillId)) return null;
			// Check if the assessment is the most recent.
			var skillSelfAssessments = _selfAssessments[selfAssessment.SkillId]; 
			if (skillSelfAssessments.LastOrDefault().Key.Equals(selfAssessment.CreatedAt.ToUnixTimeMilliseconds()) && skillSelfAssessments.Count > 1) {
				var previousAssessment = skillSelfAssessments[skillSelfAssessments.Keys[skillSelfAssessments.Count - 2]];
				RaiseEvent(new SkillSelfAssessmentRemoved(skillSetId, selfAssessment, previousAssessment));
				// This is the most recent assessment, return the previous assessment as this will now be the latest.
				return previousAssessment;
			} else {
				RaiseEvent(new SkillSelfAssessmentRemoved(skillSetId, selfAssessment));
				// This isn't the most recent assessment, so return the latest self assessment if there is more than 1, i.e this isn't the last.
				return skillSelfAssessments.Count > 1 ? skillSelfAssessments.LastOrDefault().Value : null;
			}
		}
		
		#region Events

		void Apply(SelfAssessingStarted @event) {
			UserId = @event.UserId;
			SkillSetId = @event.SkillSetId;
			CreatedAt = @event.CreatedAt;
		}

		void Apply(SkillSelfAssessmentCreated @event) {
			if (!_selfAssessments.ContainsKey(@event.SelfAssessment.SkillId)) {
				_selfAssessments.Add(@event.SelfAssessment.SkillId, new SortedList<double, SelfAssessment> {{@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds(), @event.SelfAssessment}});
			}
		}
		void Apply(SkillSelfAssessmentUpdated @event) {
			if(@event.SelfAssessment.EntryId.HasValue) return; // Don't make any changes for assessments linked to entries, they have their own events.
			if (_selfAssessments[@event.SelfAssessment.SkillId].ContainsKey(@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds())) {
				// Self assessment already exists, update.
				_selfAssessments[@event.SelfAssessment.SkillId][@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds()] = @event.SelfAssessment;
			} else {
				// Add the new Self assessment.
				_selfAssessments[@event.SelfAssessment.SkillId].Add(@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds(), @event.SelfAssessment);
			}
		}
		void Apply(SkillSelfAssessmentRemoved @event) {
			if (_selfAssessments.ContainsKey(@event.SelfAssessment.SkillId)) {
				// Remove the self assessment.
				_selfAssessments[@event.SelfAssessment.SkillId].Remove(@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds());
				// Remove the skill index, if it is empty.
				if (_selfAssessments[@event.SelfAssessment.SkillId].Count == 0) {
					_selfAssessments.Remove(@event.SelfAssessment.SkillId);
				}
			}
		}
		void Apply(EntrySelfAssessmentCreated @event) {
			// noop - handled by SkillSelfAssessmentCreated.
		}
		void Apply(EntrySelfAssessmentAdded @event) {
			if (_selfAssessments.ContainsKey(@event.SelfAssessment.SkillId) && !_selfAssessments[@event.SelfAssessment.SkillId].ContainsKey(@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds())) {
				// Add the new Self assessment.
				_selfAssessments[@event.SelfAssessment.SkillId].Add(@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds(), @event.SelfAssessment);
			}
		}
		void Apply(EntrySelfAssessmentUpdated @event) {
			if (_selfAssessments.ContainsKey(@event.SelfAssessment.SkillId)) {
				if (_selfAssessments[@event.SelfAssessment.SkillId].ContainsKey(@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds())) {
					// Self assessment already exists, update.
					_selfAssessments[@event.SelfAssessment.SkillId][@event.SelfAssessment.CreatedAt.ToUnixTimeMilliseconds()] = @event.SelfAssessment;
				}
			}
		}
		void Apply(EntrySelfAssessmentRemoved @event) {
			// noop - handled by SkillSelfAssessmentRemoved.
		}

		#endregion Events
	}
}      
