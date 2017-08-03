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
		private readonly ILogger<EntriesController> _logger;

		public EntriesController(
			ILogger<EntriesController> logger,
			IEntryService entryService, 
			ISkillService skillService,
			ISelfAssessmentService selfAssessmentService,
			IUserService userService) {
			_logger = logger;
			_entryService = entryService;
            _skillService = skillService;
			_selfAssessmentService = selfAssessmentService;
			_userService = userService;
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

		[HttpPost("{entryId}/remove")]
		// POST entries/{entryId}/remove
		// Remove the entry for the user.
		public async Task<ActionResult> RemoveEntry(Guid entryId) {
			var currentUser = await _userService.GetUserAsync(User);
			
			// Remove any self assessments.
			var entryDto = await _entryService.GetEntryAsync(currentUser, entryId);
			var latestSelfAssessments = _selfAssessmentService.RemoveSelfAssessments(currentUser, entryDto.SkillSetId, entryDto.AssessmentBundle, entryDto);

			// Remove the entry.
			_entryService.RemoveEntry(currentUser, entryId);

			return Json(latestSelfAssessments);
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
			if (entryDto.Id != Guid.Empty && (await _entryService.GetEntryAsync(currentUser, entryDto.Id) == null)) {
				_logger.LogInformation($"{caller} called with EntryId of {entryDto.Id}");
				return false;
			}

			if (entryDto.EntryType != null) {
				var entryTypes = await _entryService.GetEntryTypesAsync(skillSet.Id);
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
			}
			if (string.IsNullOrWhiteSpace(entryDto.Where)) {
				entryDto.Where = "Unknown";
			}
			return true;
		}

		[HttpGet]
		// GET entries
		// Gets all the entries for the current user.
		public async Task<ActionResult> Entries(int skillSetId, int skip = 0, int take = 20) {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"Entries called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			// Get the entries.
			var entries = await _entryService.GetEntriesAsync(currentUser, skillSetId, skip, take);

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

		[HttpGet("types")]
		// GET types
		// Gets all the entry types for the specified skillset.
		public async Task<ActionResult> EntryTypes(int skillSetId) {
			// Get the entry types.
			var types = await _entryService.GetEntryTypesAsync(skillSetId);

			return Json(types);
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
	}
}

