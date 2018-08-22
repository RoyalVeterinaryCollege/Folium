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
using System.Collections.Generic;
using Folium.Api.Models;
using System.Linq;
using System;

namespace Folium.Api.Dtos {
    public class SkillDto {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<SkillDto> ChildSkills { get; set; }
        public bool CanSelfAssess { get; set; } 
        public int SkillSetId { get; set; } 
        public bool CanSelfCount { get; set; } 
        public int? SelfAssessmentScaleId { get; set; }

        public SkillDto(Skill skill, IReadOnlyList<Skill> skills){
            Id = skill.Id;
            Name = skill.Name;
            Description = skill.Description;
            ChildSkills = skills.Where(s => s.ParentSkillId == skill.Id).Select(s => new SkillDto(s, skills)).ToList();
            CanSelfAssess = skill.CanSelfAssess;
            SkillSetId = skill.SkillSetId;
            CanSelfCount = skill.CanSelfCount;
            SelfAssessmentScaleId = skill.SelfAssessmentScaleId;
        }
    }
}