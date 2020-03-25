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
using Folium.Api.Extensions;
using Folium.Api.Models;
using Folium.Api.Models.File;
using Folium.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace Folium.Api.FileHandlers
{
    /// <summary>
    /// Abstract File Handler which provides implementations for saving and requesting files.
    /// </summary>
    public abstract class FileHandler : IFileHandler {
        internal readonly IHostingEnvironment _hostingEnvironment;
        internal readonly IFileService _fileService;
        internal readonly IConstructAggregates _factory;
        internal readonly IRepository _repository;

        public FileHandler(
            IHostingEnvironment hostingEnvironment,
            IFileService fileService,
            IConstructAggregates factory,
            IRepository repository) {
            _hostingEnvironment = hostingEnvironment; ;
            _fileService = fileService;
            _factory = factory;
            _repository = repository;
        }

        public virtual async Task SaveFileAsync(User user, ITusFile file, CancellationToken cancellationToken) {
            var maybeFileId = file.Id;
            var metadata = await file.GetMetadataAsync(cancellationToken);
            var fileName = await file.Name(cancellationToken);
            var filePath = BuildNewFilePath(file, metadata);
            var fileType = await file.Type(cancellationToken);
            
            if (Guid.TryParse(maybeFileId, out Guid fileId)) {
                using (var fileContents = await file.GetContentAsync(cancellationToken)) {
                    await _fileService.CreateFileAsync(fileId, user.Id, fileName, filePath, fileType, fileContents);
                }
            }
        }

        public virtual async Task ProcessRequestAsync(HttpContext context) {
            if(IsDeleteRequest(context)) {
                await DeleteFileAsync(context);
                return;
            }
            if (IsPostRequest(context)) {
                await UploadFileAsync(context);
            }
            await RequestFileAsync(context);
        }

        internal virtual Task UploadFileAsync(HttpContext context) {
            return Task.CompletedTask;
        }

        internal virtual async Task RequestFileAsync(HttpContext context) {
            var fileId = GetFileIdFromRequest(context.Request);

            var fileDetail = await _fileService.GetFileDetailAsync(Guid.Parse(fileId));

            if (!File.Exists(fileDetail.FilePath)) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File with id {fileId} was not found.", context.RequestAborted);
                return;
            }

            if (IsGetRequest(context)) {
                await context.StreamFileAsync(fileDetail.FilePath, fileDetail.Type, fileDetail.Filename);
                return;
            }
            if (IsHeadRequest(context)) {
                context.Response.Headers.Add("Content-Length", fileDetail.Size.ToString());
                return;
            }
        }

        internal virtual async Task DeleteFileAsync(HttpContext context) {
            var maybeFileId = GetFileIdFromRequest(context.Request);

            if (Guid.TryParse(maybeFileId, out Guid fileId)) {
                _fileService.DeleteFile(fileId);
            } else { 
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync($"Invalid file id {maybeFileId}.", context.RequestAborted);
            }
        }

        internal virtual string GetFileIdFromRequest(HttpRequest request) {
            // Valid request paths end with the fileId.
            return request.Path.ToString().Split('/').Reverse().ElementAt(0);
        }

        internal virtual bool IsDeleteRequest(HttpContext context) {
            return context.Request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
        }

        internal virtual bool IsPostRequest(HttpContext context) {
            return context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase);
        }
        internal virtual bool IsGetRequest(HttpContext context) {
            return context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase);
        }
        internal virtual bool IsHeadRequest(HttpContext context) {
            return context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
        }

        public abstract int Priority { get; }

        public abstract Task<bool> IsAuthorisedToSaveFileAsync(User user, Dictionary<string, Metadata> fileMetadata);

        public abstract bool CanSaveFile(Dictionary<string, Metadata> fileMetadata);

        public abstract Task<bool> IsAuthorisedToProcessRequestAsync(HttpContext context);

        public abstract bool CanProcessRequest(HttpContext context);
        
        internal abstract string BuildNewFilePath(ITusFile file, Dictionary<string, Metadata> fileMetadata);

        internal abstract string GetFilePathFromRequest(HttpRequest request);
    }
}
