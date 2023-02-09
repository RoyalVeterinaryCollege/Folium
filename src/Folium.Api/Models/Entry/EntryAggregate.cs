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
using System.Threading.Tasks;
using EventSaucing.Aggregates;
using Folium.Api.Models.Entry.Events;
using Microsoft.Extensions.Logging;

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
        public int? SkillGroupingId { get; private set; }
        private readonly List<Dictionary<int, SelfAssessment>> _assessmentBundle = new List<Dictionary<int, SelfAssessment>>();
		private readonly HashSet<int> _sharedWith = new HashSet<int>();
		public bool IsShared => _sharedWith.Count > 0;
		readonly Dictionary<int, EntryComment> _comments = new Dictionary<int, EntryComment>();
		private readonly HashSet<int> _authorisedUsersToSignOff = new HashSet<int>();
		public int? SignedOffBy { get; private set; }
		public DateTime? SignedOffAt { get; private set; }
		public bool IsPendingSignOff => _authorisedUsersToSignOff.Count > 0 && SignedOffBy.HasValue;

		public ReadOnlyCollection<EntryComment> Comments => _comments.Values.ToList().AsReadOnly();

		/// <summary>
		/// Gets all the assessment bundles, the most recent of which will be the last.
		/// </summary>
		public ReadOnlyCollection<Dictionary<int, SelfAssessment>> AssessmentBundle => _assessmentBundle.AsReadOnly();
		public ReadOnlyCollection<int> SharedWith => _sharedWith.ToList().AsReadOnly();
		public ReadOnlyCollection<int> AuthorisedUsersToSignOff => _authorisedUsersToSignOff.ToList().AsReadOnly();

		readonly Dictionary<Guid, EntryFile> _files = new Dictionary<Guid, EntryFile>();
		public ReadOnlyCollection<EntryFile> Files => _files.Values.ToList().AsReadOnly();

		private bool _isCreated;
		private bool _isRemoved;

		public void Create(int skillSetId, string title, string description, int userId, string where, DateTime when, int? entryTypeId = null, int? skillGroupingId = null) {
			if (_isCreated || _isRemoved) return;
			RaiseEvent(new EntryCreated(skillSetId, title, description, userId, where, when, DateTime.UtcNow, DateTime.UtcNow, skillGroupingId));
			if (entryTypeId.HasValue) {
				RaiseEvent(new EntryCreatedWithType(entryTypeId.Value));
			}
		}
		public void Update(string title, string description, string where, DateTime when, int? skillGroupingId = null) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new EntryUpdated(title, description, where, when, DateTime.UtcNow, skillGroupingId));
        }
        public void ChangeSkillGrouping(int skillGroupingId) {
            if (!_isCreated || _isRemoved) return;
            RaiseEvent(new EntrySkillGroupingChanged(skillGroupingId));
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
		public void ShareWith(int userId, List<int> collaboratorIds, string message) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new EntryShared(userId, collaboratorIds, message));
		}
		public void RemoveShareWith(int userId, int collaboratorId) {
			if (!_isCreated || _isRemoved) return;
			if (!_sharedWith.Contains(collaboratorId)) return;
			RaiseEvent(new EntryCollaboratorRemoved(userId, collaboratorId));
		}
		public void RequestSignOffBy(int userId, List<int> authorisedUserIds, string message) {
			if (!_isCreated || _isRemoved) return;
			RaiseEvent(new EntrySignOffRequested(authorisedUserIds, DateTime.UtcNow, message));
			// Also share the entry.
			RaiseEvent(new EntryShared(userId, authorisedUserIds, message));
		}
		public int SignOff(string comment, int createdBy, DateTime createdAt, List<Guid> fileIds = null) {
			var newId = _comments.Count + 1;
			RaiseEvent(new EntryCommentCreated(newId, comment, createdBy, createdAt));
			if (fileIds != null && fileIds.Count > 0) {
				RaiseEvent(new EntryCommentCreatedWithFiles(newId, fileIds));
			}
			RaiseEvent(new EntrySignedOff(createdBy, createdAt, newId));
			return newId;
		}
		public void RemoveSignOffUser(int userId) {
			if (!_isCreated || _isRemoved) return;
			if (!_authorisedUsersToSignOff.Contains(userId)) return;
			RaiseEvent(new EntrySignOffUserRemoved(userId));
		}
		public int CreateComment(string comment, int createdBy, DateTime createdAt, List<Guid> fileIds = null) {
			var newId = _comments.Count + 1;
			RaiseEvent(new EntryCommentCreated(newId, comment, createdBy, createdAt));
			if(fileIds != null && fileIds.Count > 0) {
				RaiseEvent(new EntryCommentCreatedWithFiles(newId, fileIds));
			}
			return newId;
		}
		public void AddFile(Guid fileId, int createdBy, bool onComment, string fileName, string filePath, string fileType, long fileSize) {
			if (!_isCreated || _isRemoved || _files.ContainsKey(fileId)) return;
			RaiseEvent(new EntryFileCreated(fileId, createdBy, DateTime.UtcNow, onComment, fileName, filePath, fileType, fileSize));
		}
		public void AudioVideoFileEncoded(Guid fileId, string encodedAudioVideoDirectoryPath) {
			if (!_isCreated || _isRemoved) return;
			if (_files.ContainsKey(fileId)) {
				var file = _files[fileId];
				RaiseEvent(new EntryAudioVideoFileEncoded(fileId, file.CreatedBy,file.CreatedAt, file.OnComment, file.FileName, file.FilePath, file.Type, file.Size, encodedAudioVideoDirectoryPath));
			}
		}
		public void RemoveFile(Guid fileId) {
			if (!_isCreated || _isRemoved) return;
			if (_files.ContainsKey(fileId)) {
				var file = _files[fileId];
				RaiseEvent(new EntryFileRemoved(fileId, file.CreatedBy, file.CreatedAt, file.OnComment, file.FileName, file.FilePath, file.Type, file.Size));
			}
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
            SkillGroupingId = @event.SkillGroupingId;
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
        void Apply(EntrySkillGroupingChanged @event) {
            SkillGroupingId = @event.SkillGroupingId;
        }
		void Apply(EntryRemoved @event) {
			_isRemoved = true;
		}
		void Apply(AssessmentBundleUpdated @event) {
			_assessmentBundle.Clear();
			_assessmentBundle.Add(@event.SelfAssessments);
		}
		void Apply(EntryShared @event) {
			foreach (var userId in @event.CollaboratorIds) {
				if (!_sharedWith.Contains(userId)) {
					_sharedWith.Add(userId);
				}
			}
		}
		void Apply(EntryCollaboratorRemoved @event) {
			_sharedWith.Remove(@event.CollaboratorId);
		}
		void Apply(EntrySignOffRequested @event) {
			foreach (var authorisedUserId in @event.AuthorisedUserIds) {
				if (!_authorisedUsersToSignOff.Contains(authorisedUserId)) {
					_authorisedUsersToSignOff.Add(authorisedUserId);
				}
			}
		}
		void Apply(EntrySignOffUserRemoved @event) {
			_authorisedUsersToSignOff.Remove(@event.RemoveUserId);
		}
		void Apply(EntrySignedOff @event) {
			SignedOffAt = @event.When;
			SignedOffBy = @event.AuthorisedUserId;
		}
		void Apply(EntryCommentCreated @event) {
            if (!_comments.ContainsKey(@event.Id)) {
                _comments.Add(@event.Id, new EntryComment {
                    Id = @event.Id,
                    Comment = @event.Comment,
                    CreatedAt = @event.CreatedAt,
                    CreatedBy = @event.CreatedBy
                });
            }
        }
		void Apply(EntryFileCreated @event) {
			if(!_files.ContainsKey(@event.FileId)) { 
				_files.Add(@event.FileId, new EntryFile {
					EntryId = Id,
					FileId = @event.FileId,
					CreatedAt = @event.CreatedAt,
					CreatedBy = @event.CreatedBy,
					FileName = @event.FileName,
					FilePath = @event.FilePath,
					Type = @event.Type,
					OnComment = @event.OnComment,
				});
			}
		}
		void Apply(EntryAudioVideoFileEncoded @event) {
			if (_files.ContainsKey(@event.FileId)) {
				var file = _files[@event.FileId];
				file.IsVideoEncoded = true;
				file.EncodedVideoDirectoryPath = @event.EncodedAudioVideoDirectoryPath;
			}
		}
		void Apply(EntryFileRemoved @event) {
			if(_files.ContainsKey(@event.FileId)) { 
				_files.Remove(@event.FileId);
			}
		}
		void Apply(EntryCommentCreatedWithFiles @event) {
			var comment = _comments[@event.CommentId];
			comment.FileIds = @event.FileIds;
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
