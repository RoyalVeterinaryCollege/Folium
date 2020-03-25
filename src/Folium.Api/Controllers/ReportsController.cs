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
using Folium.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Folium.Api.Dtos.Reporting;
using System.Collections.Generic;

namespace Folium.Api.Controllers {
    [Route("[controller]")]
    [Authorize, NoCache]
    public class ReportsController : Controller {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly IEntryService _entryService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ILogger<ReportsController> logger,
            IReportService reportService,
            ISkillService skillService,
            IEntryService entryService,
            ISelfAssessmentService selfAssessmentService,
            IUserService userService) {
            _logger = logger;
            _reportService = reportService;
            _userService = userService;
            _entryService = entryService;
        }

        [HttpGet("user-options")]
        // POST reports/user-options
        // Gets all the user options that the current user can report on
        public async Task<ActionResult> UserOptions() {
            var currentUser = await _userService.GetUserAsync(User);

            var options = await _reportService.GetReportOnOptionsAsync(currentUser);

            return Json(options);
        }
        
        [HttpGet("entry-types")]
        // GET types
        // Gets all the entry types for the specified skillset.
        public async Task<ActionResult> EntryTypes(List<int> skillSetIds) {
            // Get the entry types.
            var types = await _entryService.GetEntryTypesAsync(skillSetIds);

            return Json(types);
        }

        [HttpPost("self-assessment-engagement")]
        // POST reports/self-assessment-engagement
        public async Task<ActionResult> SelfAssessmentEngagementReport([FromBody]SelfAssessmentEngagementCriteriaDto criteria) {
            var currentUser = await _userService.GetUserAsync(User);

            // Validate the dto.
            if (criteria.Who.Count == 0) {
                _logger.LogInformation("SelfAssessmentEngagementReport action called with 0 user options to report on");
                return new BadRequestResult();
            }
            if (criteria.SkillSetId == 0) {
                _logger.LogInformation("SelfAssessmentEngagementReport action called with no skill set.");
                return new BadRequestResult();
            }

            var report = await _reportService.GetSelfAssessmentEngagementReportAsync(currentUser, criteria);

            return Json(report);
        }

        [HttpGet("self-assessment-engagement/min-date")]
        // POST reports/self-assessment-engagement/min-date
        public async Task<ActionResult> SelfAssessmentEngagementMinDate() {
            var minDate = await _reportService.GetSelfAssessmentEngagementMinDateAsync();

            return Json(minDate);
        }

        [HttpPost("entry-engagement")]
        // POST reports/entry-engagement
        public async Task<ActionResult> EntryEngagementReport([FromBody]EntryEngagementCriteriaDto criteria) {
            var currentUser = await _userService.GetUserAsync(User);

            // Validate the dto.
            if (criteria.Who.Count == 0) {
                _logger.LogInformation("EntryEngagementReport action called with 0 user options to report on");
                return new BadRequestResult();
            }

            var report = await _reportService.GetEntryEngagementReportAsync(currentUser, criteria);

            return Json(report);
        }

        [HttpGet("entry-engagement/min-date")]
        // POST reports/entry-engagement/min-date
        public async Task<ActionResult> EntryEngagementMinDate() {
            var minDate = await _reportService.GetEntryEngagementMinDateAsync();
            return Json(minDate);
        }

        [HttpPost("placement-engagement")]
        // POST reports/placement-engagement
        public async Task<ActionResult> PlacementEngagementReport([FromBody]PlacementEngagementCriteriaDto criteria) {
            var currentUser = await _userService.GetUserAsync(User);

            // Validate the dto.
            if (criteria.Who.Count == 0) {
                _logger.LogInformation("PlacementEngagementReport action called with 0 user options to report on");
                return new BadRequestResult();
            }

            var report = await _reportService.GetPlacementEngagementReportAsync(currentUser, criteria);

            return Json(report);
        }

        [HttpGet("placement-engagement/min-date")]
        // GET reports/placement-engagement/min-date
        public async Task<ActionResult> PlacementEngagementMinDate() {
            var minDate = await _reportService.GetPlacementEngagementMinDateAsync();
            return Json(minDate);
        }

        [HttpGet("placement-engagement/placement-types")]
        // GET reports/placement-engagement/types
        public async Task<ActionResult> PlacementTypes() {
            var types = await _reportService.GetPlacementTypesAsync();
            return Json(types);
        }
    }
}

