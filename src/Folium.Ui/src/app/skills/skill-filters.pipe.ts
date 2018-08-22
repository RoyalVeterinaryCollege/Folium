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
import { Pipe, PipeTransform } from "@angular/core";

import { SkillGroup, SkillFilterFacet } from "../core/dtos";

@Pipe({
  name: "filter"
})
export class SkillFiltersPipe implements PipeTransform {
  transform(skillGroups: SkillGroup[], filters: SkillFilterFacet[] = [], searchTerms: string[] = []) {
    if(!skillGroups || skillGroups.length == 0) return skillGroups;
    // Group the filter facets by thier filter groups.
    // To be a filter match the skill id must be in all the groups.
    let filterGroups:  { [id: number]: number[]; } = { };
    filters.forEach(f => {
      if (filterGroups[f.skillFilterId]) {
        // Filter group exists, add the the skill ids to the list.
        filterGroups[f.skillFilterId] = [...filterGroups[f.skillFilterId], ...f.matchedSkillIds];
      } else {
        // Group doesn"t exist, create it.
        filterGroups[f.skillFilterId] = [...f.matchedSkillIds];
      }
    });
    skillGroups.forEach(skillGroup => {
      this.transform(skillGroup.childGroups, filters, searchTerms);
      skillGroup.skills.forEach(s => {
        let hide = (searchTerms.length > 0 && searchTerms.some(t => !s.name.toLowerCase().includes(t.toLowerCase())));
        if (!hide) { // short circuit checking the filters if we already need to hide.
          for (let filterGroup in filterGroups) {
            if (filterGroups.hasOwnProperty(filterGroup)) {
              hide = hide
                || ((filterGroups[filterGroup].indexOf(s.id) === -1)
                    && s.childSkills.every(c => filterGroups[filterGroup].indexOf(c.id) === -1));
            }
          }
        }
        s.assessment.hidden =  hide;
      });
    });

    return skillGroups;
  }

  getSkillTotalFromGroup(skillGroup: SkillGroup) {
    let total = skillGroup.skills.length;
    skillGroup.childGroups.forEach(group => {
      total += this.getSkillTotalFromGroup(group);
    });
    return total;
  }
}