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
using Microsoft.AspNetCore.Mvc;
using Folium.Api.Services;
using Folium.Api.Infrastructure;
using System.Threading.Tasks;
using Folium.Api.Dtos;
using Folium.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using Folium.Api.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Folium.Api.Controllers {
    [Route("[controller]")]
    public class UsersController : Controller {
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        private readonly ISkillService _skillService;
			
        public UsersController(
            ILogger<UsersController> logger,
            IUserService userService,
            ISkillService skillService) {
            _logger = logger;
            _userService = userService;
            _skillService = skillService;
        }                   

        [NoCache, Authorize, HttpGet]
        // GET users
        // Gets upto 10 users that match the supplied query string, the users are returned as collaborator options.
        public async Task<ActionResult> Index(string q) {
			var currentUser = await _userService.GetUserAsync(User);
			return string.IsNullOrWhiteSpace(q) 
				? null 
				: Json(_userService.GetCollaboratorOptions(currentUser, q).Take(10));
        }

	    [NoCache, HttpGet("current")]
		// GET users/current
		// Gets the currently signed in user.
		public async Task<ActionResult> Current() {
			if (User == null || !User.Identity.IsAuthenticated) return Json(null);
			return Json(await GetCurrentUser());
		}

		[NoCache, Authorize, HttpGet("sign-in")]
        // GET users/sign-in
        // Registers that a user has signed in.
        public async Task<ActionResult> RegisterSignIn() {
            var user = await GetCurrentUser();

            if(user == null) {
                // User does not exist, create it.
                var newUser = await _userService.CreateUserAsync(new User(){
                    Email = User.Email(),
                    FirstName = User.FirstName(),
                    LastName = User.LastName()                    
                });

                return Json(newUser);
            }

            // Check if the first name & last name need updating.
            if(string.IsNullOrEmpty(user.FirstName) && string.IsNullOrEmpty(user.LastName)) {
                await _userService.UpdateUserAsync(new User(){
                    Email = User.Email(),
                    FirstName = User.FirstName(),
                    LastName = User.LastName()            
                });
            }
            
            var updatedUser = await _userService.RegisterUserSignInAsync(user);
            return Json(new UserDto(updatedUser));
        }

        [NoCache, Authorize, HttpPost("{userId}/profile-image")]
        // POST users/{userId}/profile-image
        // Creates a new profile image for the user.
        public async Task<ActionResult> UpdateUserImage(int userId, IFormFile originalPic = null, string editedPic = null) {            
            // Ensure the user being updated is the current user.
            var currentUser = await _userService.GetUserAsync(User);
            if(userId != currentUser.Id) {
                return new UnauthorizedResult();
            }
            
            // Do we have an original pic, that will need cropping, or has the image been edited already?
            if(originalPic != null) {                
                using(var imageStream = originalPic.OpenReadStream()) {
                    await _userService.UpdateUserAsync(currentUser, imageStream);
                }
            }
            if(editedPic != null) {  
                var base64Image = editedPic.Split(',')[1]; // We just want the base64 encoded part of the Data URI string, so remove the 'data:image/png;base64,' part.

                // Convert the base64 encoded image to a stream to save.
                using(var imageStream = new MemoryStream(Convert.FromBase64String(base64Image))) {
                    await _userService.UpdateUserAsync(currentUser, imageStream);
                }
            }
            if(originalPic == null && editedPic == null){
                // Remove the current profile pic.
                await _userService.RemoveUserImage(currentUser);
            }

            return Json(GetCurrentUser());
		}

		[NoCache, Authorize, HttpGet("current/courses/{courseId}/tutors")]
		// GET users/current/courses/{courseId}/tutors
		// Gets the tutors for the currently signed in user.
		public async Task<ActionResult> GetTutors(int courseId) {
			var currentUser = await _userService.GetUserAsync(User);

			var tutors = await _userService.GetTutorsAsync(currentUser, courseId);

			return Json(tutors);
		}

        [NoCache, Authorize, HttpGet("{userId}/skill-sets")]
        // GET users/{userId}/skill-sets
        public async Task<ActionResult> SkillSets(int userId) {
            var sets = (await _skillService.GetSkillSetsAsync(userId))
                .Select(s => new SkillSetDto(s)).ToList();
            return Json(sets);
        }

        [NoCache, Authorize, HttpPost("current/skill-sets/add/{skillSetId}")]
        // POST users/current/skill-sets/add/{skillSetId}
        // Adds the skill set to the users list.
        public async Task<ActionResult> AddSkillSet(int skillSetId) {
            var currentUser = await _userService.GetUserAsync(User);

            // Validation.
            var skillSet = await _skillService.GetSkillSetAsync(skillSetId);
            if (skillSet == null) {
                _logger.LogInformation($"AddSkillSet called with invalid SkillSetId of {skillSetId}");
                return new BadRequestResult();
            }

            var userSkillSets = await _skillService.GetSkillSetsAsync(currentUser.Id);
            if (userSkillSets.Any(set => set.Id == skillSetId)) {
                // User already has this skillset.
                return new OkResult();
            }

            // Add the user skillset.
            await _skillService.AddUserSkillSetAsync(currentUser.Id, skillSetId);

            return new OkResult();
        }

        [NoCache, Authorize, HttpPost("current/skill-sets/remove/{skillSetId}")]
        // POST users/current/skill-sets/remove/{skillSetId}
        // Remove the skill set from the users list.
        public async Task<ActionResult> RemoveSkillSet(int skillSetId) {
            var currentUser = await _userService.GetUserAsync(User);

            // Validation.
            var skillSet = await _skillService.GetSkillSetAsync(skillSetId);
            if (skillSet == null) {
                _logger.LogInformation($"RemoveSkillSet called with invalid SkillSetId of {skillSetId}");
                return new BadRequestResult();
            }

            var userSkillSets = await _skillService.GetSkillSetsAsync(currentUser.Id);
            if (userSkillSets.All(set => set.Id != skillSetId)) {
                // User doesn't have this skillset.
                return new OkResult();
            }

            // Remove the user skillset.
            await _skillService.RemoveUserSkillSetAsync(currentUser.Id, skillSetId);

            return new OkResult();
        }

        // Gets the currently signed in user.
        private async Task<UserDto> GetCurrentUser() {
            var user = await _userService.GetUserAsync(User);
            
            return user == null ? null : new UserDto(user);
		}

	}
}

