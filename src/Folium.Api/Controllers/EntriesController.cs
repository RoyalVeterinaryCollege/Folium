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
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Folium.Api.Services;
using System.Threading.Tasks;
using Folium.Api.Dtos;
using Folium.Api.Extensions;
using Folium.Api.Infrastructure;
using Folium.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Folium.Api.Controllers {
	[Route("[controller]")]
	[Authorize, NoCache]
	public class EntriesController : Controller {
        private readonly IEntryService _entryService;
		private readonly IUserService _userService;
		private readonly ISkillService _skillService;
		private readonly ISelfAssessmentService _selfAssessmentService;
		private readonly IFileService _fileService;
		private readonly ITutorGroupService _tutorGroupService;
		private readonly ILogger<EntriesController> _logger;

		public EntriesController(
			ILogger<EntriesController> logger,
			IEntryService entryService, 
			ISkillService skillService,
			ISelfAssessmentService selfAssessmentService,
			IUserService userService,
			IFileService fileService,
			ITutorGroupService tutorGroupService) {
			_logger = logger;
			_entryService = entryService;
            _skillService = skillService;
			_selfAssessmentService = selfAssessmentService;
			_userService = userService;
			_fileService = fileService;
			_tutorGroupService = tutorGroupService;
		}

		[HttpPost("create")]
		// POST entries/create
		// Creates a new entry for the user.
		public async Task<ActionResult> CreateEntry([FromBody]EntryDto entryDto) {
			var currentUser = await _userService.GetUserAsync(User);
			if (!(await IsValidEntry("CreateEntry", currentUser, entryDto))) {
				return new BadRequestResult();
			}

			// Create the entry.
			entryDto.When = new DateTime(
				entryDto.When.Year, 
				entryDto.When.Month, 
				entryDto.When.Day, 
				entryDto.When.Hour, 
				entryDto.When.Minute, 
				entryDto.When.Second); // Remove the miliseconds.
			var newEntry = _entryService.CreateEntry(currentUser, entryDto);

			_selfAssessmentService.CreateSelfAssessments(
				currentUser, 
				entryDto.SkillSetId, 
				entryDto.AssessmentBundle, 
				newEntry);

			return Json(newEntry);
		}

		[HttpPost("{entryId}/comment")]
		// POST entries/{entryId}/comment
		// Comment on an entry.
		public async Task<ActionResult> Comment([FromBody]EntryCommentDto entryCommentDto) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryCommentDto.EntryId);
			if(entry == null) {
				_logger.LogInformation($"Comment action called with entry id of {entryCommentDto.EntryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}
			// Validate the dto.
			if (!IsValidComment("Comment", currentUser, entryCommentDto)) {
				return new BadRequestResult();
			}
			entryCommentDto.CreatedAt = DateTime.UtcNow;
			var newId = _entryService.CreateComment(entryCommentDto);

			return Json(newId);
		}

		[HttpPost("{entryId}/update")]
		// POST entries/{entryId}/update
		// Update the entry for the user.
		public async Task<ActionResult> UpdateEntry([FromBody]EntryDto entryDto) {
			var currentUser = await _userService.GetUserAsync(User);
			if(!(await IsValidEntry("UpdateEntry", currentUser, entryDto))) {
				return new BadRequestResult();
			}

			// Update any self assessments.
			var currentEntry = await _entryService.GetEntryAsync(currentUser, entryDto.Id);
			if (!(await IsValidEntry("UpdateEntry", currentUser, entryDto))) {
				return new BadRequestResult();
			}

			if (currentEntry.SignedOff) {
				// Can't edit a signed off entry.
				return new BadRequestResult();
			}

			var removedSelfAssessments = currentEntry.AssessmentBundle
				.Where(p => !entryDto.AssessmentBundle.ContainsKey(p.Key))
				.ToDictionary(p => p.Key, p => p.Value);
			// Remove any self assessments, we will get the latest self assessments back.
			var latestSelfAssessments = _selfAssessmentService.RemoveSelfAssessments(
				currentUser, 
				entryDto.SkillSetId,
				removedSelfAssessments, 
				entryDto);
			// Add all the self assessments, this will update any existing ones too.
			_selfAssessmentService.CreateSelfAssessments(
				currentUser,
				entryDto.SkillSetId,
				entryDto.AssessmentBundle,
				entryDto);

			// Update the entry.
			_entryService.UpdateEntry(currentUser, entryDto);
			return Json(latestSelfAssessments);
        }

        [HttpPost("{entryId}/skill-grouping")]
        // POST entries/{entryId}/skill-grouping
        // Change the skill grouping for the entry.
        public async Task<ActionResult> ChangeEntrySkillGrouping(Guid entryId, [FromBody]dynamic data) {
            var currentUser = await _userService.GetUserAsync(User);

            var entryDto = await _entryService.GetEntryAsync(currentUser, entryId);

            if (!(await IsValidEntry("ChangeEntrySkillGrouping", currentUser, entryDto))) {
                return new BadRequestResult();
            }

            // change the skill grouping.
            _entryService.ChangeEntrySkillGrouping(entryId, (int)data.skillGroupingId);
            return new OkResult();
        }

        [HttpPost("{entryId}/remove")]
		// POST entries/{entryId}/remove
		// Remove the entry for the user.
		public async Task<ActionResult> RemoveEntry(Guid entryId) {
			var currentUser = await _userService.GetUserAsync(User);

			var entryDto = await _entryService.GetEntryAsync(currentUser, entryId);

			if (!(await IsValidEntry("RemoveEntry", currentUser, entryDto))) {
				return new BadRequestResult();
			}

			// Remove any self assessments.
			var latestSelfAssessments = _selfAssessmentService.RemoveSelfAssessments(currentUser, entryDto.SkillSetId, entryDto.AssessmentBundle, entryDto);

			// Remove any files.
			await _fileService.DeleteAllEntryFilesAsync(entryId);

			// Remove the entry.
			_entryService.RemoveEntry(entryId);

			return Json(latestSelfAssessments);
		}

		[HttpPost("{entryId}/share")]
		// POST entries/{entryId}/share
		// Share the entry for the user.
		public async Task<ActionResult> ShareEntry([FromBody]ShareEntryDto shareEntryDto) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, shareEntryDto.EntryId);
			if (entry == null) {
				_logger.LogInformation($"ShareEntry action called with entry id of {shareEntryDto.EntryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}

			if (entry.Author.Id != currentUser.Id) {
				_logger.LogInformation($"ShareEntry action called with entry id of {shareEntryDto.EntryId} by user id of {currentUser.Id}, which was not the creator of the entry, which is user id {entry.Author.Id}");
				return new BadRequestResult();
			}

			var existingCollaborators = await _entryService.GetCollaboratorsAsync(shareEntryDto.EntryId);

			// Just in case the list container the current user or any existing collaborators, remove them.
			shareEntryDto.CollaboratorIds.Remove(currentUser.Id);
			foreach (var collaborator in existingCollaborators) {
				shareEntryDto.CollaboratorIds.Remove(collaborator.Id);
			}
			
			// Remove the entry.
			_entryService.ShareEntry(currentUser, shareEntryDto);

			return new OkResult();
		}

		[HttpPost("{entryId}/collaborators/{userId}/remove")]
		// POST entries/{entryId}/collaborators/{userId}/remove
		// Removes a collaborator.
		public async Task<ActionResult> RemoveCollaborator(Guid entryId, int userId) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryId);
			if (entry == null) {
				_logger.LogInformation($"RemoveCollaborator action called with entry id of {entryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}

			if (entry.Author.Id != currentUser.Id) {
				_logger.LogInformation($"RemoveCollaborator action called with entry id of {entryId} by user id of {currentUser.Id}, which was not the creator of the entry, which is user id {entry.Author.Id}");
				return new BadRequestResult();
			}

			var existingCollaborators = (await _entryService.GetCollaboratorsAsync(entryId)).ToList();

			if (existingCollaborators.All(u => u.Id != userId)) {
				_logger.LogInformation($"RemoveCollaborator action called with entry id of {entryId} by user id of {currentUser.Id}, wishing to remove user id {userId}, which doesn't exist in the list of collaborators");
				return new BadRequestResult();
			}

			// Ensure they are not on the list for signing off the entry, as the entry also needs to be shared with them
			if (entry.IsSignOffCompatible && !entry.SignedOff) {
				var existingAuthorisedSignOffUsers = await _entryService.GetEntrySignOffUsersAsync(entryId);
				if (existingAuthorisedSignOffUsers.Any(u => u.Id == userId)) {
					_logger.LogInformation($"RemoveCollaborator action called with entry id of {entryId} by user id of {currentUser.Id}, wishing to remove user id {userId}, which needs the entry shared with them as they are authorised to sign-off");
					return new BadRequestResult();
				}
			}

			// Remove the collaborator.
			_entryService.RemoveCollaborator(currentUser, entryId, userId);

			return new OkResult();
		}

		[HttpGet("{entryId}/collaborators")]
		// POST entries/{entryId}/collaborators
		// Gets the collaborators.
		public async Task<ActionResult> Collaborators(Guid entryId) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryId);
			if (entry == null) {
				_logger.LogInformation($"Collaborators action called with entry id of {entryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}
			
			var existingCollaborators = await _entryService.GetCollaboratorsAsync(entryId);

			return Json(existingCollaborators);
		}

		[HttpPost("{entryId}/request-sign-off")]
		// POST entries/{entryId}/request-sign-off
		// Request the entry to be signed off.
		public async Task<ActionResult> RequestSignOff([FromBody]EntrySignOffRequestDto signOffRequestDto) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, signOffRequestDto.EntryId);
			if (entry == null) {
				_logger.LogInformation($"RequestSignOff action called with entry id of {signOffRequestDto.EntryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}

			if (entry.Author.Id != currentUser.Id) {
				_logger.LogInformation($"RequestSignOff action called with entry id of {signOffRequestDto.EntryId} by user id of {currentUser.Id}, which was not the creator of the entry, which is user id {entry.Author.Id}");
				return new BadRequestResult();
			}

			// Check only valid users have been requested to sign-off the entry.
			if(entry.EntryType.Template.signOff == null) {
				_logger.LogInformation($"RequestSignOff action called with entry id of {signOffRequestDto.EntryId} by user id of {currentUser.Id}, but the entry type {entry.EntryType.Name} does not allow sign-off.");
				return new BadRequestResult();
			}
			if (entry.EntryType.Template.signOff.allowedBy == "tutor") {
				// Only allow sign-off by tutor.
				var tutors = _tutorGroupService.GetAllTutors(currentUser);
				if (signOffRequestDto.AuthorisedUserIds.Any(userId => !tutors.Any(t => t.Id == userId))) {
					_logger.LogInformation($"RequestSignOff action called with entry id of {signOffRequestDto.EntryId} by user id of {currentUser.Id}, but the sign-off list contained users who are not tutors.");
					return new BadRequestResult();
				}
			}

			var existingAuthorisedSignOffUsers = await _entryService.GetEntrySignOffUsersAsync(signOffRequestDto.EntryId);

			// Just in case the list contains any existing authorised users, remove them.
			foreach (var user in existingAuthorisedSignOffUsers) {
				signOffRequestDto.AuthorisedUserIds.Remove(user.Id);
			}

			// Request sign-off.
			_entryService.RequestSignOff(currentUser, signOffRequestDto);

			return new OkResult();
		}

		[HttpPost("{entryId}/request-sign-off/users/{userId}/remove")]
		// POST entries/{entryId}/request-sign-off/users/{userId}/remove
		// Removes an entry sign-off user.
		public async Task<ActionResult> RemoveSignOffUser(Guid entryId, int userId) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryId);
			if (entry == null) {
				_logger.LogInformation($"RemoveSignOffUser action called with entry id of {entryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}

			if (entry.Author.Id != currentUser.Id) {
				_logger.LogInformation($"RemoveSignOffUser action called with entry id of {entryId} by user {currentUser.Id}, which was not the creator of the entry, which is user id {entry.Author.Id}");
				return new BadRequestResult();
			}

			if (!entry.SignOffRequested) {
				_logger.LogInformation($"RemoveSignOffUser action called with entry id of {entryId} by user {currentUser.Id}, when the entry is not set to require sign-off.");
				return new BadRequestResult();
			}

			var existingSignOffUsers = (await _entryService.GetEntrySignOffUsersAsync(entryId)).ToList();

			if (existingSignOffUsers.All(u => u.Id != userId)) {
				_logger.LogInformation($"RemoveSignOffUser action called with entry id of {entryId} by user id of {currentUser.Id}, wishing to remove user id {userId}, which doesn't exist in the list of existing sign-off users");
				return new BadRequestResult();
			}

			// Remove the collaborator.
			_entryService.RemoveEntrySignOffUser(entryId, userId);

			return new OkResult();
		}

		[HttpGet("{entryId}/request-sign-off/users")]
		// GET entries/{entryId}/request-sign-off/users
		// Gets the entry sign-off users.
		public async Task<ActionResult> SignOffUsers(Guid entryId) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryId);
			if (entry == null) {
				_logger.LogInformation($"SignOffUsers action called with entry id of {entryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}

			var existingSignOffUsers = await _entryService.GetEntrySignOffUsersAsync(entryId);

			return Json(existingSignOffUsers);
		}

		[HttpPost("{entryId}/sign-off")]
		// POST entries/{entryId}/sign-off
		// Sign-off the entry.
		public async Task<ActionResult> SignOff([FromBody]EntryCommentDto entryCommentDto) {
			var currentUser = await _userService.GetUserAsync(User);

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryCommentDto.EntryId);
			if(entry == null) {
				_logger.LogInformation($"SignOff action called with entry id of {entryCommentDto.EntryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}
			if (entry.SignedOff) {
				_logger.LogInformation($"SignOff action called with entry id of {entryCommentDto.EntryId} which is already signed off.");
				return new BadRequestResult();
			}
			if (!entry.SignOffRequested) {
				_logger.LogInformation($"SignOff action called with entry id of {entryCommentDto.EntryId} by user {currentUser.Id}, when the entry is not set to require sign-off.");
				return new BadRequestResult();
			}

			// Validate the user is authorised to sign-off.
			var existingSignOffUser = await _entryService.GetEntrySignOffUsersAsync(entryCommentDto.EntryId, includeCourseAdmins: true);

			if (!existingSignOffUser.Any(u => u.Id == currentUser.Id)) {
				_logger.LogInformation($"SignOff action called with entry id of {entryCommentDto.EntryId} by user {currentUser.Id}, who is not in the list of authorised users to sign-off this entry.");
				return new BadRequestResult();
			}

			// Validate the dto.
			if (!IsValidComment("SignOff", currentUser, entryCommentDto)) {
				return new BadRequestResult();
			}

			entryCommentDto.CreatedAt = DateTime.UtcNow;
			var newId = _entryService.SignOffEntry(entryCommentDto);

			return Json(newId);
		}

		private bool IsValidComment(string caller, User currentUser, EntryCommentDto entryCommentDto) {
			// Validate the dto.
			if (entryCommentDto.Author == null) {
				_logger.LogInformation($"{caller} action called with entry id of {entryCommentDto.EntryId} by user id {currentUser.Id} with an empty author");
				return false;
			}
			if (entryCommentDto.Author.Id != currentUser.Id) {
				_logger.LogInformation($"{caller} action called with entry id of {entryCommentDto.EntryId} by user id {currentUser.Id} with a different author id of {entryCommentDto.Author.Id}");
				return false;
			}
			if (string.IsNullOrWhiteSpace(entryCommentDto.Comment)) {
				_logger.LogInformation($"{caller} action called with entry id of {entryCommentDto.EntryId} by user id {currentUser.Id} with an empty comment");
				return false;
			}
			return true;
		}

		private async Task<bool> IsValidEntry(string caller, User currentUser, EntryDto entryDto) {
			if (entryDto.SkillSetId == 0) {
				_logger.LogInformation($"{caller} called with invalid SkillSetId of {entryDto.SkillSetId}");
				return false;
			}
			var skillSet = await _skillService.GetSkillSetAsync(entryDto.SkillSetId);
			if (skillSet == null) {
				_logger.LogInformation($"{caller} called with invalid SkillSetId of {entryDto.SkillSetId}");
				return false;
			}
			var existingEntry = entryDto.Id != Guid.Empty ? await _entryService.GetEntryAsync(currentUser, entryDto.Id) : null;
			if (entryDto.Id != Guid.Empty && existingEntry == null) {
				_logger.LogInformation($"{caller} called with EntryId of {entryDto.Id}");
				return false;
			}
			if (entryDto.Id != Guid.Empty && existingEntry.Author.Id != currentUser.Id) {
				_logger.LogInformation($"{caller} called with EntryId of {entryDto.Id} that was created by user {entryDto.Author.Id} but being edited by {currentUser.Id}");
				return false;
			}

			if (entryDto.EntryType != null) {
				var entryTypes = await _entryService.GetEntryTypesAsync(new[] { skillSet.Id });
				if (entryDto.Id == Guid.Empty) {
					// this is a new entry, don't allow retired entry types.
					entryTypes = entryTypes.Where(t => t.Retired == false);
				}
				if (entryTypes.All(t => t.Id != entryDto.EntryType.Id)) {
					_logger.LogInformation($"{caller} called with invalid entry type of {entryDto.EntryType.Id}");
					return false;
				}
			}

			if (entryDto.When == DateTime.MinValue) {
				entryDto.When = DateTime.UtcNow;
			}
			if (string.IsNullOrWhiteSpace(entryDto.Title)) {
				entryDto.Title = "Untitled Entry";
			} else {
                if(entryDto.Title.Length > 1000) {
                    _logger.LogInformation($"{caller} called with title of length {entryDto.Title.Length}");
                    return false;
                }
            }
			if (string.IsNullOrWhiteSpace(entryDto.Where)) {
				entryDto.Where = "Unknown";
            }
            else {
                if (entryDto.Where.Length > 1050) {
                    _logger.LogInformation($"{caller} called with where of length {entryDto.Where.Length}");
                    return false;
                }
            }
            return true;
		}

		[HttpGet]
		// GET entries
		// Gets all the entries the current user can see.
		public async Task<ActionResult> Entries(int skip = 0, int take = 20) {
			var currentUser = await _userService.GetUserAsync(User);
			// Get the entries.
			var entries = await _entryService.GetAllEntriesAsync(currentUser, skip, take);

			return Json(entries);
		}

		[HttpGet("my")]
		// GET entries
		// Gets all the entries created by the current users.
		public async Task<ActionResult> MyEntries(int skip = 0, int take = 20, EntryService.EntriesFilter? filter = null) {
            var currentUser = await _userService.GetUserAsync(User);
			// Get the entries.
			var entries = await _entryService.GetMyEntriesAsync(currentUser, skip, take, filter);

			return Json(entries);
		}

		[HttpGet("shared")]
		// GET entries
		// Gets all the entries that are shared with the current user.
		public async Task<ActionResult> EntriesSharedWithMe(int skip = 0, int take = 20, EntryService.EntriesFilter? filter = null) {
			var currentUser = await _userService.GetUserAsync(User);
			// Get the entries.
			var entries = await _entryService.GetEntriesSharedWithMeAsync(currentUser, skip, take, filter);

			return Json(entries);
		}

		[HttpGet("shared/{userId}")]
		// GET entries
		// Gets just the entries by the specified user that are shared with the current user.
		public async Task<ActionResult> EntriesSharedWithMeByUser(int userId, int skip = 0, int take = 20) {
			var currentUser = await _userService.GetUserAsync(User);
			User user;
			var userToView = _userService.GetUser(userId);
			if (userToView == null) return Json(null);
			if (await _userService.CanViewUserDataAsync(currentUser, userToView)) {
				user = userToView;
			} else {
				return Json(null);
			}

			// Get the entries.
			var entries = await _entryService.GetEntriesSharedWithMeByUserAsync(currentUser, skip, take, sharedByUser: user);

			return Json(entries);
		}

		[HttpGet("{entryId}")]
		// GET entries/{entryId}
		// Gets the requested entry.
		public async Task<ActionResult> Entry(Guid entryId) {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"Entry called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			// Get the entry.
			var entry = await _entryService.GetEntryAsync(currentUser, entryId);

			return Json(entry);
        }

        [HttpGet("{entryId}/summary")]
        // GET entries/{entryId}/summary
        // Gets the requested entry summary.
        public async Task<ActionResult> EntrySummary(Guid entryId) {
            var currentUser = await _userService.GetUserAsync(User);
            if (currentUser == null) {
                _logger.LogInformation($"EntrySummary called with invalid user {User.Email()}");
                return new BadRequestResult();
            }

            // Get the entry summary.
            var entry = await _entryService.GetEntrySummaryAsync(currentUser, entryId);

            return Json(entry);
        }

        [HttpGet("types")]
		// GET types
		// Gets all the entry types for the specified skillset.
		public async Task<ActionResult> EntryTypes(List<int> skillSetIds) {
			// Get the entry types.
			var types = await _entryService.GetEntryTypesAsync(skillSetIds);

			return Json(types.Where(t => t.Retired == false).ToList());
		}

		[HttpGet("where")]
		// GET entries/where
		// Gets a list of places where entries have been recorded, a StartsWith parameter can be supplied to reduce the list.
		public async Task<ActionResult> Where(string startsWith = "") {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"Where called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			// Get the places.
			var wheres = await _entryService.GetPlacesAsync(currentUser, startsWith);

			return Json(wheres);
		}

		[HttpGet("{entryId}/files")]
		// GET entries/{entryId}/files
		// Gets the files attached to the entry.
		public async Task<ActionResult> Files(Guid entryId) {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"Files called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			// Get the entry to ensure the current user is able to access it.
			var entry = await _entryService.GetEntryAsync(currentUser, entryId);
			if (entry == null)
			{
				_logger.LogInformation($"Files action called with entry id of {entryId} which was not valid when being requested by user id of {currentUser.Id}");
				return new BadRequestResult();
			}

			// Get the files on the entry.
			var files = await _entryService.GetEntryFilesAsync(entry.Id);

			return Json(files);
		}
	}
}

