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
using CommonDomain.Persistence;
using Folium.Api.Models;
using Folium.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace Folium.Api.FileHandlers
{
    /// <summary>
    /// Default handler for files.
    /// Is responsible for:
    ///     * streaming files from the requested location on disk.
    ///     * saving files to the default location.
    /// </summary>
    public class DefaultFileHandler : FileHandler {
        
        internal string RequestPath => "/file-uploads/";

        public override int Priority => -10;

        public DefaultFileHandler(
            IHostingEnvironment hostingEnvironment,
            IFileService fileService,
            IConstructAggregates factory,
            IRepository repository) : base(hostingEnvironment, fileService, factory, repository) {
        }

        public override Task<bool> IsAuthorisedToSaveFileAsync(User user, Dictionary<string, Metadata> fileMetadata) {
            return Task.Run(() => true);
        }

        public override bool CanSaveFile(Dictionary<string, Metadata> fileMetadata) {
            return true;
        }

        public override Task<bool> IsAuthorisedToProcessRequestAsync(HttpContext context) {
            return Task.Run(() => true);
        }

        public override bool CanProcessRequest(HttpContext context) {
            return context.Request.Path.StartsWithSegments(new PathString(RequestPath));
        }
        
        internal override string BuildNewFilePath(ITusFile file, Dictionary<string, Metadata> fileMetadata) {
            return GetFilePath(file.Id);
        }

        internal override string GetFilePathFromRequest(HttpRequest request) {
            var fileId = GetFileIdFromRequest(request);
            return GetFilePath(fileId);
        }
        
        private string GetFileDirectory() {
            // Directory is at {root}/attachments/
            return $"{_hostingEnvironment.ContentRootPath}{Path.DirectorySeparatorChar}file-uploads{Path.DirectorySeparatorChar}";
        }

        private string GetFilePath(string fileId) {
            // Directory is at {webroot}/files/{fileId}
            return $"{GetFileDirectory()}{fileId}";
        }
    }
}
