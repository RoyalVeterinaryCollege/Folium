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
using Folium.Api.Dtos;
using Folium.Api.Extensions;
using Folium.Api.Models;
using Folium.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
    /// Handles files attached to entries.
    /// Is responsible for:
    ///     * validating that a request to save a file is authorised
    ///     * validating that a request to stream a file is authorised
    ///     * saving the file to the correct location on disk
    /// </summary>
    public class EntryFileHandler : FileHandler {
        internal readonly IEntryService _entryService;
        internal readonly IUserService _userService;
        private readonly ILogger<EntryFileHandler> _logger;

        public override int Priority => 0;

        internal string RequestPath => "/file-uploads/entries";

        public EntryFileHandler(
            IHostingEnvironment hostingEnvironment,
            IFileService fileService,
            IConstructAggregates factory,
            IRepository repository,
            IEntryService entryService,
            IUserService userService,
            ILogger<EntryFileHandler> logger) : base(hostingEnvironment, fileService, factory, repository) {
            _entryService = entryService;
            _userService = userService;
            _logger = logger;
        }

        public override bool CanSaveFile(Dictionary<string, Metadata> fileMetadata) {
            return fileMetadata.ContainsKey("entryId");
        }

        public override async Task<bool> IsAuthorisedToSaveFileAsync(User user, Dictionary<string, Metadata> fileMetadata) {
            var maybeEntryId = GetEntryIdFromMetadata(fileMetadata);
            
            Guid entryId;

            if (!Guid.TryParse(maybeEntryId, out entryId)) {
                // Not a valid entry id format.
                return false;
            }

            // Get the entry.
            var entry = await _entryService.GetEntryAsync(user, entryId);
            return await IsAuthorisedToCreateEntry(user, entry);
        }

        public override async Task SaveFileAsync(User user, ITusFile file, CancellationToken cancellationToken) {
            var maybeFileId = file.Id;
            var metadata = await file.GetMetadataAsync(cancellationToken);
            var fileName = await file.Name(cancellationToken);
            var filePath = BuildNewFilePath(file, metadata);
            var fileType = await file.Type(cancellationToken);
            var maybeEntryId = GetEntryIdFromMetadata(metadata);
            var onComment = await file.onEntryComment(cancellationToken);
            
            if (Guid.TryParse(maybeFileId, out Guid fileId) && Guid.TryParse(maybeEntryId, out Guid entryId)) {
                using (var fileContents = await file.GetContentAsync(cancellationToken)) {
                    await _fileService.CreateEntryFileAsync(fileId, user.Id, fileName, filePath, fileType, fileContents, entryId, onComment);
                }
            }
        }

        public override bool CanProcessRequest(HttpContext context) {
            return context.Request.Path.StartsWithSegments(new PathString(RequestPath));
        }

        public override async Task<bool> IsAuthorisedToProcessRequestAsync(HttpContext context) {
            if (IsPostRequest(context)) {
                return false; // Don't allow upload requests.
            }

            // Only for authenticated users.
            if (context.User != null && context.User.Identity.IsAuthenticated) {
                // Get the current user.
                var user = await _userService.GetUserAsync(context.User);
                // Authorised if the user is the person who created the file.
                var entryIdOnPath = GetEntryIdFromRequest(context.Request);
                if (string.IsNullOrEmpty(entryIdOnPath)) {
                    // No entry id on request.
                    return false;
                }
                
                if (!Guid.TryParse(entryIdOnPath, out var entryId)) {
                    // Not a valid entry id format.
                    return false;
                }

                // Get the entry.
                var entry = await _entryService.GetEntryAsync(user, entryId);

                if (IsDeleteRequest(context)) {
                    var fileId = GetFileIdFromRequest(context.Request);
                    return await _fileService.IsFileCreatorAsync(Guid.Parse(fileId), user.Id);
                }
                return await IsAuthorisedToCreateEntry(user, entry);
            } else {
                return false;
            }
        }
        
        private async Task<bool> IsAuthorisedToCreateEntry(User user, EntryDto entry) {
            if (entry.Author.Id != user.Id) {
                // This is not the author viewing an attachment on the entry.
                // Collaborators are also allowed.
                if (!entry.Shared) {
                    // Entry not shared, block.
                    return false;
                }
                var collaborators = await _entryService.GetCollaboratorsAsync(entry.Id);
                if (!collaborators.Any(c => c.Id == user.Id)) {
                    // User is not a collaborator on this entry, block.
                    return false;
                }
            }
            return true;
        }

        internal override async Task DeleteFileAsync(HttpContext context) {
            var maybeFileId = GetFileIdFromRequest(context.Request);
            var maybeEntryId = GetEntryIdFromRequest(context.Request);
            
            if (Guid.TryParse(maybeFileId, out Guid fileId) && Guid.TryParse(maybeEntryId, out Guid entryId)) {
                _fileService.DeleteEntryFile(fileId, entryId);
            } else { 
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync($"Invalid file id {maybeFileId} and/or entry id {maybeEntryId}.", context.RequestAborted);
            }
        }

        internal override string BuildNewFilePath(ITusFile file, Dictionary<string, Metadata> fileMetadata) {
            var entryId = GetEntryIdFromMetadata(fileMetadata);
            var fileId = Guid.Parse(file.Id).ToString();
            return GetFilePath(entryId, fileId, fileId);
        }

        internal override string GetFilePathFromRequest(HttpRequest request) {
            var fileId = GetFileIdFromRequest(request);
            var entryId = GetEntryIdFromRequest(request);
            return GetFilePath(entryId, fileId, fileId);
        }

        private string GetFileDirectory(string entryId, string fileId) {
            // Directory is at {contentroot}/file-uploads/entries/{entryId}/{fileId}
            return $"{_hostingEnvironment.ContentRootPath}{Path.DirectorySeparatorChar}file-uploads{Path.DirectorySeparatorChar}entries{Path.DirectorySeparatorChar}{entryId}{Path.DirectorySeparatorChar}{fileId}{Path.DirectorySeparatorChar}";
        }

        internal string GetFilePath(string entryId, string fileId, string filename) {
            // Directory is at {webroot}/file-uploads/entries/{entryId}/{fileId}
            return $"{GetFileDirectory(entryId,fileId)}{filename}";
        }

        internal string GetEntryIdFromMetadata(Dictionary<string, Metadata> fileMetadata) {
            return fileMetadata["entryId"].GetString(Encoding.UTF8);
        }

        internal string GetEntryIdFromRequest(HttpRequest request) {
            // Valid request paths are /file-uploads/entries/{entryId}/{fileId}
            return request.Path.ToString().Replace(RequestPath + "/", "").Split('/')[0];
        }
        
    }
}
