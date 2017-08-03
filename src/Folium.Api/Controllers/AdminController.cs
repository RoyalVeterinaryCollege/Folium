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
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Folium.Api.ViewModels;
using Folium.Api.Services;
using System.Threading.Tasks;
using Folium.Api.Infrastructure;

namespace Folium.Api.Controllers {
    public class AdminController : Controller {
        private readonly ICourseService _courseService;
        private readonly ISkillService _skillService;

		public AdminController(ICourseService courseService, ISkillService skillService) {
            _courseService = courseService;
            _skillService = skillService;
		}

		[LocalConnection]
		public async Task<IActionResult> ImportSkills() {
			// This is just temporary for now, until we have a proper skills admin api and UI.
            var model = new ImportSkillSetViewModel();
            var courses = await _courseService.GetCoursesAsync(); 
            model.Courses = courses.Select(c => new SelectListItem{Text = c.Title, Value = c.Id.ToString()}).ToList();
            return View(model);
        }

        [HttpPost]
		[LocalConnection]
		public async Task<IActionResult> ImportSkills(ImportSkillSetViewModel model) {
            if(ModelState.IsValid){
                var failureMessages = await _skillService.ImportSkillsAsync(model.CourseId, model.Description, model.File.OpenReadStream());
                if(failureMessages.Any()){
                    model.FailureMessages.AddRange(failureMessages);
                } else {
                    model.SuccessMessages.Add($"The skill set has been successfully imported.");
                }
            }
            var courses = await _courseService.GetCoursesAsync(); 
            model.Courses = courses.Select(c => new SelectListItem{Text = c.Title, Value = c.Id.ToString()}).ToList();
            return View(model);
        }
    }
}

