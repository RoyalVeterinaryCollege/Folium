import { inherits } from "util";
import { Entry, EntrySummary } from "../core/dtos";

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

export const enum EntryFilter {
  NotShared = 1,
  Shared = 2,
  PendingSignOff = 3,
  SignedOff = 4
}

export interface Filter {
  label: string;
  filter: EntryFilter;
  isValidEntryForFilter(entry: Entry | EntrySummary): boolean;
}

export class NoFilter implements Filter {
  label: string = '<button class="btn btn-anchor">All Entries</button>';
  filter: EntryFilter;
  isValidEntryForFilter(entry: Entry | EntrySummary): boolean {
    return true;
  }
}
export class PendingSignOffFilter implements Filter {
  label: string = '<button class="btn btn-anchor"><i class="fas fa-external-link-alt"></i> Pending Sign-off</button>';
  filter: EntryFilter = EntryFilter.PendingSignOff;
  isValidEntryForFilter(entry: Entry | EntrySummary): boolean {
    return entry.signOffRequested && !entry.signedOff;
  }
}
export class SignedOffFilter implements Filter {
  label: string = '<button class="btn btn-anchor"><i class="fas fa-check-square"></i> Signed off</button>';
  filter: EntryFilter = EntryFilter.SignedOff;
  isValidEntryForFilter(entry: Entry | EntrySummary): boolean {
    return entry.signedOff;
  }
}
export class SharedFilter implements Filter {
  label: string = '<button class="btn btn-anchor"><i class="fas fa-share-alt"></i> Shared</button>';
  filter: EntryFilter = EntryFilter.Shared;
  isValidEntryForFilter(entry: Entry | EntrySummary): boolean {
    return entry.shared;
  }
}
export class NotSharedFilter implements Filter {
  label: string = '<button class="btn btn-anchor"><i class="fas fa-circle fa-xs"></i> Not Shared</button>';
  filter: EntryFilter = EntryFilter.NotShared;
  isValidEntryForFilter(entry: Entry | EntrySummary): boolean {
    return !entry.shared;
  }
}
