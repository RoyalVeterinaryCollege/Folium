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
using Folium.Api.Infrastructure;
using System.Threading.Tasks;
using Folium.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Folium.Api.Extensions;
using Microsoft.Extensions.Logging;

namespace Folium.Api.Controllers {
    [Route("")]
    [Authorize]
    public class SkillsController : Controller {
        private readonly ISkillService _skillService;
        private readonly ISelfAssessmentService _selfAssessmentService;
        private readonly ITaxonomyService _taxonomyService;
		private readonly IUserService _userService;
		private readonly ILogger<SkillsController> _logger;

		public SkillsController(
			ILogger<SkillsController> logger,
			ISkillService skillService,
            ITaxonomyService taxonomyService,
            ISelfAssessmentService selfAssessmentService,
			IUserService userService) {
            _skillService = skillService;
            _taxonomyService = taxonomyService;
            _selfAssessmentService = selfAssessmentService;
			_userService = userService;
			_logger = logger;
		}                   

        [NoCache]
        // GET skill-set
        [HttpGet("skill-set")]
        public async Task<ActionResult> SkillSets() {
			var currentUser = await _userService.GetUserAsync(User);
			var sets = (await _skillService.GetSkillSetsAsync(currentUser.Id))
                .Where(s => s.SelfAssignable == true)
                .Select(s => new SkillSetDto(s)).ToList();
            return Json(sets);
        }

        [NoCache]
        // GET skill-set/{skillSetId}
        [HttpGet("skill-set/{skillSetId}")]
        public async Task<ActionResult> SkillSet(int skillSetId, int userId = -1) {
			/* TODO: Check for default userId and use current user. */
			var skills = (await _skillService.GetSkillsAsync(skillSetId)).Where(s => s.Removed == false).ToList();
            var hierarchyTerms = (await _taxonomyService.GetHierarchyTermsAsync(skillSetId));
            var skillTerms = await _taxonomyService.GetHierarchySkillTermsAsync(skillSetId);
            var groups = hierarchyTerms
                .Where(t => t.ParentTaxonomyTermId == null)
                .OrderBy(t => t.Name)
                .Select(t => new SkillGroupDto(t, hierarchyTerms, skillTerms, skills))
                .ToList();

            return Json(groups);
        }

        [NoCache]
        // GET skill-set/{skillSetId}/groups
        [HttpGet("skill-set/{skillSetId}/filters")]
        public async Task<ActionResult> SkillFilters(int skillSetId) {
            var filters = await _taxonomyService.GetSkillFilterTaxonomysAsync(skillSetId);
            var filterTerms = await _taxonomyService.GetSkillFilterTermsAsync(skillSetId);
            var skillTerms = await _taxonomyService.GetSkillFilterSkillTermsAsync(skillSetId);
            var groups = filters
                .OrderBy(t => t.Name)
                .Select(t => new SkillFilterDto(t, filterTerms.Where(f => f.TaxonomyId == t.Id).OrderBy(f => f.Name).ToList(), skillTerms))
                .ToList();

            return Json(groups);
        }

        [NoCache]
        // GET skill-set/{skillSetId}/self-assessment-scales
        [HttpGet("skill-set/{skillSetId}/self-assessment-scales")]
        public async Task<ActionResult> SelfAssessmentScales(int skillSetId) {
            var scales = (await _selfAssessmentService.GetSelfAssessmentScalesAsync(skillSetId))
                .SelectMany(s => s.Levels.Select(l => new SelfAssessmentScaleDto(s, l)))
                .ToList();
            return Json(scales);
		}

		[NoCache]
		// GET skill-set/{skillSetId}
		[HttpGet("skill-set/{skillSetId}/self-assessments")]
		public async Task<ActionResult> SkillSelfAssessments(int skillSetId, int userId = -1) {
			/* TODO: Check for default userId and use current user. */
			var currentUser = await _userService.GetUserAsync(User);
			var selfAssessments = (await _selfAssessmentService.GetSelfAssessmentsAsync(skillSetId, currentUser.Id))
				.Select(s => new SelfAssessmentDto(s))
				.ToList(); ;

			return Json(selfAssessments);
		}

		[NoCache] 
		[HttpPost("skill-set/{skillSetId}/self-assessments/create")]
		// POST skill-set/{skillSetId}/self-assessments/create
		// Creates a new self assessment for the user.
		public async Task<ActionResult> CreateSelfAssessment(int skillSetId, [FromBody]SelfAssessmentDto selfAssessmentDto) {
			var currentUser = await _userService.GetUserAsync(User);
			if (currentUser == null) {
				_logger.LogInformation($"CreateSelfAssessment called with invalid user {User.Email()}");
				return new BadRequestResult();
			}

			// Validation.
			if (selfAssessmentDto.SkillId == 0) {
				_logger.LogInformation($"CreateSelfAssessment called with invalid SkillId of {selfAssessmentDto.SkillId}");
				return new BadRequestResult();
			}
			var skill = await _skillService.GetSkillAsync(skillSetId, selfAssessmentDto.SkillId);
			if (skill == null) {
				_logger.LogInformation($"CreateSelfAssessment called with invalid SkillId of {selfAssessmentDto.SkillId} with SkillSetId of {skillSetId}");
				return new BadRequestResult();
			}
			if (!skill.CanSelfAssess || !skill.SelfAssessmentScaleId.HasValue) {
				_logger.LogInformation($"CreateSelfAssessment called with SkillId of {selfAssessmentDto.SkillId} which cannot be self assessed.");
				return new BadRequestResult();
			}
			var selfAssessmentScales = await _selfAssessmentService.GetSelfAssessmentScalesAsync(skillSetId);
			var selfAssessment = selfAssessmentScales.FirstOrDefault(s => s.Id == skill.SelfAssessmentScaleId.Value);
			var selfAssessmentLevel = selfAssessment.Levels.FirstOrDefault(l => l.Id == selfAssessmentDto.LevelId);
			if (selfAssessmentLevel == null) {
				_logger.LogInformation($"CreateSelfAssessment called with SkillId of {selfAssessmentDto.SkillId} with an invalid LevelId of {selfAssessmentDto.LevelId}");
				return new BadRequestResult();
			}
			if (selfAssessmentLevel.Score != selfAssessmentDto.Score) {
				_logger.LogInformation($"CreateSelfAssessment called with SkillId of {selfAssessmentDto.SkillId} with LevelId of {selfAssessmentDto.LevelId} with an invalid score of {selfAssessmentDto.Score}");
				return new BadRequestResult();
			}

			// Create the selfAssessment.
			_selfAssessmentService.CreateSelfAssessments(currentUser, skillSetId, new Dictionary<int, SelfAssessmentDto> {{skill.Id, selfAssessmentDto}});

			return new OkResult();
		}
	}
}

