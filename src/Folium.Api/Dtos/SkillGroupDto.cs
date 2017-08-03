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
    public class SkillGroupDto{
        public int Id { get; set; }
        public string Name { get; set; }
        public IReadOnlyList<SkillGroupDto> ChildGroups { get; set; }
        public IReadOnlyList<SkillDto> Skills { get; set; }

        public SkillGroupDto(TaxonomyTerm taxonomyTerm, IReadOnlyList<TaxonomyTerm> taxonomyTerms, IReadOnlyList<SkillTaxonomyTerm> skillTaxonomyTerms, IReadOnlyList<Skill> skills){
            Id = taxonomyTerm.Id;
            Name = taxonomyTerm.Name;
            ChildGroups = taxonomyTerms.Where(t => t.ParentTaxonomyTermId == taxonomyTerm.Id).Select(t => new SkillGroupDto(t, taxonomyTerms, skillTaxonomyTerms, skills)).ToList();
            Skills = skills.Join(
                skillTaxonomyTerms.Where(s => s.TaxonomyTermId == taxonomyTerm.Id), 
                s => s.Id, 
                t => t.SkillId, 
                (s, t) => new SkillDto(s, skills)
            ).ToList();
        }
    }
}