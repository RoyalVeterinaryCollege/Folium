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
using System;
using Newtonsoft.Json;

namespace Folium.Api.Dtos {
    public class EntryFileDto {
        public Guid EntryId { get; set; }
        public Guid FileId { get; set; }
        public string Filename { get; set; }
        public bool OnComment { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
        public int? CommentId { get; set; }
        public long Size { get; set; }
        public string RequestPath => $"file-uploads/entries/{EntryId}/{FileId}/{FileId}";
        public bool IsAudioVideoEncoded { get; set; }
    }
}