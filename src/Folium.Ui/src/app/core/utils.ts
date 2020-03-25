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
export class Utils {    
    static deepClone(objectToBeCloned) {
        // Basis.
        if (!(objectToBeCloned instanceof Object)) {
            return objectToBeCloned;
        }
        
        if (objectToBeCloned instanceof Date) {
            return new Date(objectToBeCloned.valueOf());
        }

        let objectClone = new objectToBeCloned.constructor();

        // Clone each property.
        for (let prop in objectToBeCloned) {
            objectClone[prop] = this.deepClone(objectToBeCloned[prop]);
        }

        return objectClone;
    }
    static toDictionary<TItem>(
        array: TItem[],
        getKey: (item: TItem) => number): { [id: number]: TItem };
    static toDictionary<TItem, TValue>(
        array: TItem[],
        getKey: (item: TItem) => number,
        getValue: (item: TItem) => TValue): { [id: number]: TValue };
    static toDictionary<TItem>(
        array: TItem[],
        getKey: (item: TItem) => string): { [id: string]: TItem };
    static toDictionary<TItem, TValue>(
        array: TItem[],
        getKey: (item: TItem) => string,
        getValue: (item: TItem) => TValue): { [id: string]: TValue };
    static toDictionary<TItem>(
        array: TItem[],
        getKey: (item: TItem) => number | string,
        getValue?: (item: TItem) => any): any {
        var result = <any>{};
        if (array) {
            if (getValue) {
                array.forEach(_ => result[getKey(_)] = getValue(_));
            }
            else {
                array.forEach(_ => result[getKey(_)] = _);
            }
        }
        return result;
    }

    static getDateArray = function(startDate: Date, endDate:Date) {
        let result = new Array<Date>();

        // Strip hours minutes seconds etc.
        let currentDate = new Date(Date.UTC(
            startDate.getFullYear(),
            startDate.getMonth(),
            startDate.getDate()
        ));

        while (currentDate <= endDate) {
            result.push(currentDate);

            currentDate = new Date(Date.UTC(
                currentDate.getFullYear(),
                currentDate.getMonth(),
                currentDate.getDate() + 1, // Will increase month if over range
            ));
        }

        return result;
    }
}
