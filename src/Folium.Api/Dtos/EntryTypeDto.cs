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
using Newtonsoft.Json;

namespace Folium.Api.Dtos {
    /// <summary>
    /// Represents an Entry Template.
    /// </summary>
    public class EntryTypeDto {
        public int Id { get; set; } 
        public string Name { get; set; } 
        public bool Retired { get; set; }
	    private string _template;
	    public dynamic Template {
		    get {
			    return _template == null ? _template : JsonConvert.DeserializeObject(_template);
		    }
		    set {
			    if (value is string) {
				    _template = value;
			    } else {
				    _template = JsonConvert.SerializeObject(value);
			    }
		    }
	    }
    }
}      
