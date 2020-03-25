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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace Folium.Api.FileHandlers
{
    /// <summary>
    /// TinyMce handler for files.
    /// Is responsible for:
    ///     * streaming files from the requested location on disk.
    ///     * saving files to the default location.
    /// </summary>
    public class TinyMceFileHandler : FileHandler {
        
        internal string RequestPath => "/file-uploads/tinymce";

        public override int Priority => -10;

        public TinyMceFileHandler(
            IHostingEnvironment hostingEnvironment,
            IFileService fileService,
            IConstructAggregates factory,
            IRepository repository) : base(hostingEnvironment, fileService, factory, repository) {
        }

        public override Task<bool> IsAuthorisedToSaveFileAsync(User user, Dictionary<string, Metadata> fileMetadata) {
            return Task.Run(() => false); // Don't process tus file saves.
        }

        public override bool CanSaveFile(Dictionary<string, Metadata> fileMetadata) {
            return false; // Don't process tus file saves.
        }

        public override Task<bool> IsAuthorisedToProcessRequestAsync(HttpContext context) {
             return Task.Run(() => {
                 if(base.IsDeleteRequest(context)) {
                     return false; // Don't allow deletes.
                 }

                 if (base.IsPostRequest(context)) {
                     // Only for authenticated users.
                     if (context.User != null && context.User.Identity.IsAuthenticated) {
                         return true;
                     }
                     return false;
                 } else {
                     return true;
                 }
             });
        }

        public override bool CanProcessRequest(HttpContext context) {
            return context.Request.Path.StartsWithSegments(new PathString(RequestPath));
        }

        internal override async Task UploadFileAsync(HttpContext context) {
            var form = await context.Request.ReadFormAsync();
            if (form.Files.Any()) {
                var filePath = this.GetFilePathFromRequest(context.Request);
                var directory = Path.GetDirectoryName(filePath);
                var filename = Guid.NewGuid().ToString();
                // Copy the file to the new location.
                var newFilePath = Path.Combine(directory, filename);
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                using (var fileStream = File.Create(newFilePath)) {
                    await form.Files[0].CopyToAsync(fileStream);
                }
                string jsonString = JsonConvert.SerializeObject(new { location = $"file-uploads/tinymce/{filename}"});

                context.Response.ContentType = "application/json";
                context.Response.ContentLength = jsonString.Length;
                await context.Response.WriteAsync(jsonString, Encoding.UTF8);

                return;
            }
        }

        internal override async Task RequestFileAsync(HttpContext context) {
            var fileId = GetFileIdFromRequest(context.Request);
            var filePath = GetFilePathFromRequest(context.Request);
            if (!File.Exists(filePath)) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File with id {fileId} was not found.", context.RequestAborted);
                return;
            }

            using (var fileStream = File.OpenRead(filePath)) {
                await fileStream.CopyToAsync(context.Response.Body, 81920, context.RequestAborted);
            }
            return;
        }

        internal override string BuildNewFilePath(ITusFile file, Dictionary<string, Metadata> fileMetadata) {
            return GetFilePath(file.Id);
        }

        internal override string GetFilePathFromRequest(HttpRequest request) {
            var fileId = GetFileIdFromRequest(request);
            return GetFilePath(fileId);
        }
        
        private string GetFileDirectory() {
            return $"{_hostingEnvironment.ContentRootPath}{Path.DirectorySeparatorChar}file-uploads{Path.DirectorySeparatorChar}tinymce{Path.DirectorySeparatorChar}";
        }

        private string GetFilePath(string fileId) {
            // Directory is at {webroot}/files/{fileId}
            return $"{GetFileDirectory()}{fileId}";
        }
    }
}
