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
import { Pipe, PipeTransform, SecurityContext } from "@angular/core";
import { DomSanitizer } from "@angular/platform-browser";

/** Credit goes to https://github.com/tvicpe/nl2br-pipe **/

@Pipe({ name: 'nlToBr' })
export class NlToBrPipe implements PipeTransform {

  constructor(private sanitizer: DomSanitizer) {
  }

  transform(value: string, sanitizeBeforehand?: boolean): string {
    if (typeof value !== 'string') {
      return value;
    }
    let result: any;
    const textParsed = value.replace(/(?:\r\n|\r|\n)/g, '<br />');

    if (sanitizeBeforehand) {
      result = this.sanitizer.sanitize(SecurityContext.HTML, textParsed);
    } else {
      result = textParsed;
    }

    return result;
  }
}
