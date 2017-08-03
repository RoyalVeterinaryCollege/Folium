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
	public class PlacementsController : Controller {
        private readonly IPlacementService _placementService;
		private readonly IUserService _userService;
		private readonly ILogger<PlacementsController> _logger;

		public PlacementsController(
			ILogger<PlacementsController> logger,
			IPlacementService placementService, 
			IUserService userService) {
			_logger = logger;
			_placementService = placementService;
			_userService = userService;
		}

		[HttpPost("create")]
		// POST placements/create
		// Creates a new placement for the user.
		public async Task<ActionResult> CreatePlacement([FromBody]PlacementDto placementDto) {
			var currentUser = await _userService.GetUserAsync(User);
			if (!(await IsValidPlacement("CreatePlacement", currentUser, placementDto))) {
				return new BadRequestResult();
			}

			// Create the placement.
			var newPlacement = _placementService.CreatePlacement(currentUser, placementDto);
			
			return Json(newPlacement);
		}

		[HttpPost("{placementId}/update")]
		// POST placements/{placementId}/update
		// Update the placement for the user.
		public async Task<ActionResult> UpdatePlacement([FromBody]PlacementDto placementDto) {
			var currentUser = await _userService.GetUserAsync(User);
			if (!(await IsValidPlacement("UpdatePlacement", currentUser, placementDto))) {
				return new BadRequestResult();
			}
			// Update the placement.
			_placementService.UpdatePlacement(currentUser, placementDto);
			return Json(placementDto);
		}

		[HttpPost("{placementId}/remove")]
		// POST placements/{placementId}/remove
		// Remove the placement for the user.
		public async Task<ActionResult> RemovePlacement(Guid placementId) {
			var currentUser = await _userService.GetUserAsync(User);

			var placementDto = await _placementService.GetPlacementAsync(placementId);
			// validate to check the current user can modify the placement.
			if (!(await IsValidPlacement("RemovePlacement", currentUser, placementDto))) {
				return new BadRequestResult();
			}
			
			// Remove the placement.
			_placementService.RemovePlacement(currentUser, placementId);

			return new OkResult();
		}

		[HttpGet]
		// GET placements
		// Gets all the placements for the user.
		public async Task<ActionResult> Placements(int? userId = null, int skip = 0, int take = 20) {
			var currentUser = await _userService.GetUserAsync(User);
			var user = userId.HasValue ? await _userService.GetUserAsync(userId.Value) : currentUser;
			if (user == null) {
				_logger.LogInformation($"Placements called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			if (currentUser.Id != user.Id) {
				// If a different user is accessing the placement then check they are authorised.
				if (!await IsAuthorisedAsync(currentUser)) {
					_logger.LogInformation($"Placements called by user {currentUser.Id} attempting to view a placements for {userId} which is forbidden as they are not authorised.");
					return new BadRequestResult();
				}
			}

			// Get the placements.
			var placements = await _placementService.GetPlacementsAsync(user.Id, skip, take);

			return Json(placements);
		}

		[HttpGet("{placementId}")]
		// GET placements/{placementId}
		// Gets the requested placement.
		public async Task<ActionResult> Placement(Guid placementId) {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"Placement called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			var placement = await _placementService.GetPlacementAsync(placementId);

			if (currentUser.Id != placement.UserId) {
				// If a different user is accessing the placement then check they are authorised.
				if (!await IsAuthorisedAsync(currentUser)) {
					_logger.LogInformation($"Placement called by user {currentUser.Id} attempting to view a placements for {placement.UserId} which is forbidden as they are not authorised.");
					return new BadRequestResult();
				}
			}
			
			return Json(placement);
		}

		[HttpGet("{placementId}/entries")]
		// GET placements/{placementId}/entries
		// Gets the requested placement entries.
		public async Task<ActionResult> PlacementEntries(Guid placementId, int skip = 0, int take = 20) {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"PlacementEntries called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			var placement = await _placementService.GetPlacementAsync(placementId);

			if (currentUser.Id != placement.UserId) {
				// If a different user is accessing the placement then check they are authorised.
				if (!await IsAuthorisedAsync(currentUser)) {
					_logger.LogInformation($"PlacementEntries called by user {currentUser.Id} attempting to view a placements for {placement.UserId} which is forbidden as they are not authorised.");
					return new BadRequestResult();
				}
			}

			var entries = await _placementService.GetPlacementEntriesAsync(placementId, skip, take);

			return Json(entries);
		}

		private async Task<bool> IsValidPlacement(string caller, User currentUser, PlacementDto placementDto) {
			if (await _userService.GetUserAsync(placementDto.UserId) == null) {
				_logger.LogInformation($"{caller} called with invalid userId of {placementDto.UserId}.");
				return false;
			}
			if (currentUser.Id != placementDto.UserId) {
				// If a different user is accessing the placement then check they are authorised.
				if (!await IsAuthorisedAsync(currentUser)) {
					_logger.LogInformation($"{caller} called by user {currentUser.Id} attempting to create/update a placement for {placementDto.UserId} which is forbidden as they are not authorised.");
					return false;
				}
			}
			if (string.IsNullOrWhiteSpace(placementDto.Title)) {
				_logger.LogInformation($"{caller} called with invalid title.");
				return false;
			}
			if (placementDto.Start == DateTime.MinValue) {
				_logger.LogInformation($"{caller} called with invalid start date of {placementDto.Start}.");
				return false;
			}
			if (placementDto.End == DateTime.MinValue) {
				_logger.LogInformation($"{caller} called with invalid end date of {placementDto.Start}.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Validates that the provided user is authorised to access any placements.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		private async Task<bool> IsAuthorisedAsync(User user) {
			// TODO: Allow other users with correct claims to also be authorised.
			var systemUser = await _userService.GetOrCreateSystemUserAsync();
			return user.Id == systemUser.Id;
		}
	}
}

