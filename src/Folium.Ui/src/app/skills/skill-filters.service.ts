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
import { Injectable, EventEmitter } from "@angular/core";

import { SkillFilterFacet } from "../core/dtos";

@Injectable()
export class SkillFiltersService {
    private _filterFacets: SkillFilterFacet[] = [];
    private filterFacetUpdated$: EventEmitter<SkillFilterFacet> = new EventEmitter<SkillFilterFacet>();
    private _searchTerms: string[] = [];
    private searchTerms$: EventEmitter<string[]> = new EventEmitter<string[]>();

    constructor() { }

    get onFilterFacetUpdated() {
        return this.filterFacetUpdated$;
    }

    get filterFacets() {
        return this._filterFacets;
    }

    get onSearchTermsChanged() {
        return this.searchTerms$;
    }

    get searchTerms() {
        return this._searchTerms;
    }

    addFilterFacet(facet: SkillFilterFacet) {
        if (!this._filterFacets.includes(facet)) {
            this._filterFacets = [...this._filterFacets, facet];
            this.filterFacetUpdated$.emit(facet);
        }
    }

    removeFilterFacet(facet: SkillFilterFacet) {
        facet.selected = false;
        let index = this._filterFacets.findIndex(f => f.id === facet.id);
        if (index >= 0) {
            this._filterFacets = [
                ...this._filterFacets.slice(0, index),
                ...this._filterFacets.slice(index + 1)
            ];
            this.filterFacetUpdated$.emit(facet);
        }
    }

    clearFilterFacets() {
        while (this._filterFacets.length > 0) {
            this.removeFilterFacet(this._filterFacets[0]);
        }
    }

    addSearch(term: string) {
        this.clearSearch();
        term.split(" ").forEach(t => {
            if (!this._searchTerms.includes(t)) {
                this._searchTerms = [...this._searchTerms, t];
            }
        })
        this.searchTerms$.emit(this._searchTerms);
    }

    clearSearch() {
        this._searchTerms = [];
        this.searchTerms$.emit(this._searchTerms);
    }
}