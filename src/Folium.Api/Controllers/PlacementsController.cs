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
			if (!(IsValidPlacement("CreatePlacement", currentUser, placementDto))) {
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
			if (!(IsValidPlacement("UpdatePlacement", currentUser, placementDto))) {
				return new BadRequestResult();
			}
            // Check the same user is updating the placement.
            var currentPlacement = await _placementService.GetPlacementAsync(placementDto.Id);
            if(currentPlacement.CreatedBy != currentUser.Id) {
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
			if (!(IsValidPlacement("RemovePlacement", currentUser, placementDto)) || placementDto.CreatedBy != currentUser.Id) {
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
            var user = await _userService.GetUserAsync(User);
            if (userId.HasValue && user.Id != userId.Value) {
                var userToView = _userService.GetUser(userId.Value);
                if (userToView == null) {
                    return Json(null);
                }
                if (await _userService.CanViewUserDataAsync(user, userToView)) {
                    user = userToView;
                }
                else {
                    return Json(null);
                }
            }
            
			// Get the placements.
			var placements = await _placementService.GetPlacementsAsync(user.Id, skip, take);

			return Json(placements);
		}

		[HttpGet("{placementId}")]
		// GET placements/{placementId}
		// Gets the requested placement.
		public async Task<ActionResult> Placement(Guid placementId, int? userId = null) {
            var user = await _userService.GetUserAsync(User);
            if (userId.HasValue && user.Id != userId.Value) {
                var userToView = _userService.GetUser(userId.Value);
                if (userToView == null) return Json(null);
                if (await _userService.CanViewUserDataAsync(user, userToView)) {
                    user = userToView;
                }
                else {
                    return Json(null);
                }
            }

			var placement = await _placementService.GetPlacementAsync(placementId);
            			
			return Json(placement);
		}

		[HttpGet("{placementId}/entries")]
		// GET placements/{placementId}/entries
		// Gets the requested placement entries.
		public async Task<ActionResult> PlacementEntries(Guid placementId, int? userId = null, int skip = 0, int take = 20) {
            var currentUser = await _userService.GetUserAsync(User);
			if (userId.HasValue && currentUser.Id != userId.Value) {
                var userToView = _userService.GetUser(userId.Value);
                if (userToView == null) return Json(null);
                if (!await _userService.CanViewUserDataAsync(currentUser, userToView)) {
                    return Json(null);
                }
            }
            
			var entries = await _placementService.GetPlacementEntriesAsync(currentUser, placementId, skip, take);

			return Json(entries);
		}

		private bool IsValidPlacement(string caller, User currentUser, PlacementDto placementDto) {
            if (placementDto == null) {
                _logger.LogWarning($"{caller} called with empty PlacementDto.");
                return false;
            }
            if (_userService.GetUser(placementDto.UserId) == null) {
				_logger.LogWarning($"{caller} called with invalid userId of {placementDto.UserId}.");
				return false;
			}
			if (currentUser.Id != placementDto.UserId && !currentUser.IsSystemUser) {	
				_logger.LogWarning($"{caller} called by user {currentUser.Id} attempting to create/update a placement for {placementDto.UserId} which is forbidden as they are not authorised.");
				return false;
			}
			if (string.IsNullOrWhiteSpace(placementDto.Title)) {
				_logger.LogWarning($"{caller} called with invalid title.");
				return false;
            } else {
                if (placementDto.Title.Length > 1000) {
                    _logger.LogInformation($"{caller} called with title of length {placementDto.Title.Length}");
                    return false;
                }
            }
            if (placementDto.Start == DateTime.MinValue) {
				_logger.LogWarning($"{caller} called with invalid start date of {placementDto.Start}.");
				return false;
			}
			if (placementDto.End == DateTime.MinValue) {
				_logger.LogWarning($"{caller} called with invalid end date of {placementDto.Start}.");
				return false;
			}
			return true;
		}
	}
}

