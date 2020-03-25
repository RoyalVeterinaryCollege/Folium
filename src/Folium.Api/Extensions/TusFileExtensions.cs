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
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;

namespace Folium.Api.Extensions {
    public static class TusFileExtensions {
        public static async Task<string> Name(this ITusFile file, CancellationToken cancellationToken) {
            var metadata = await file.GetMetadataAsync(cancellationToken);

            return metadata.ContainsKey("name")
                ? metadata["name"].GetString(Encoding.UTF8)
                : file.Id;
        }
        public static async Task<string> Type(this ITusFile file, CancellationToken cancellationToken) {
            var metadata = await file.GetMetadataAsync(cancellationToken);

            return metadata.ContainsKey("filetype")
                ? metadata["filetype"].GetString(Encoding.UTF8)
                : "application/octet-stream";
        }
        public static async Task<bool> onEntryComment(this ITusFile file, CancellationToken cancellationToken) {
            var metadata = await file.GetMetadataAsync(cancellationToken);

            return metadata.ContainsKey("onComment");
        }

        public static async Task<Guid?> EntryId(this ITusFile file, CancellationToken cancellationToken) {
            var metadata = await file.GetMetadataAsync(cancellationToken);

            var maybeEntryId = metadata.ContainsKey("entryId")
                ? metadata["entryId"].GetString(Encoding.UTF8)
                : null;

            if (maybeEntryId == null) return null;
            
            Guid entryId;
            if(Guid.TryParse(maybeEntryId, out entryId)) {
                return entryId;
            }
            return null;
        }
    }
}