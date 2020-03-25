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
using Folium.Api.Services;
using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using tusdotnet.Models;

namespace Folium.Api.FileHandlers
{
    /// <summary>
    /// _t_10x45
    /// Handles files attached to entries.
    /// Is responsible for:
    ///     * validating that a request to save a file is authorised
    ///     * validating that a request to stream a file is authorised
    ///     * saving the file to the correct location on disk
    /// </summary>
    public class EntryImageThumbnailFileHandler : EntryFileHandler {
        public override int Priority => 10;

        public EntryImageThumbnailFileHandler(
            IHostingEnvironment hostingEnvironment,
            IFileService fileService,
            IConstructAggregates factory,
            IRepository repository,
            IEntryService entryService,
            IUserService userService,
            ILogger<EntryImageThumbnailFileHandler> logger) : base(hostingEnvironment, fileService, factory, repository, entryService, userService, logger) {
        }

        public override bool CanProcessRequest(HttpContext context) {
            // We can process this request if the base can process and the request ends in a thumbnail image request (conatains _t_).
            return base.CanProcessRequest(context) && context.Request.Path.Value.Split('/').Reverse().ElementAt(0).Contains("_t_");
        }

        public override bool CanSaveFile(Dictionary<string, Metadata> fileMetadata) {
            return false;
        }
        
        internal override async Task RequestFileAsync(HttpContext context) {
            // First check if the thumbnail already exists.
            var filePath = GetFilePathFromRequest(context.Request);
            var requestedFile = context.Request.Path.Value.Split('/').Reverse().ElementAt(0);
            // Requested file will be in the format {fileId}_t_{width}x{height}
            var fileId = requestedFile.Split('_')[0];
            // The incorrect fileId will be within the path, replace it.
            filePath = filePath.Replace(requestedFile + Path.DirectorySeparatorChar, fileId + Path.DirectorySeparatorChar);

            if (!File.Exists(filePath)) {

                var size = requestedFile.Split('_')[2];
                if (!int.TryParse(size.Split('x')[0], out int width) || !int.TryParse(size.Split('x')[1], out int height)) {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync($"Requested size is not valid.", context.RequestAborted);
                    return;
                }
                var entryId = GetEntryIdFromRequest(context.Request);
                var fileDetail = await _entryService.GetEntryFileDetailAsync(Guid.Parse(entryId), Guid.Parse(fileId));

                if (fileDetail == null) {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"File with id {fileId} was not found.", context.RequestAborted);
                    return;
                }

                // Check the filetype.
                var isImage = fileDetail.Type.StartsWith("image", StringComparison.OrdinalIgnoreCase);
                var isVideo = fileDetail.Type.StartsWith("video", StringComparison.OrdinalIgnoreCase);
                if (!isVideo && !isImage) {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync($"Requested file is not valid.", context.RequestAborted);
                    return;
                }

                if (isVideo && !fileDetail.IsAudioVideoEncoded) {
                    // Videos have an associated image with a different file name to use.
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"Video thumbnail for file id {fileId} is not yet available.", context.RequestAborted);
                    return;
                }

                // Video thumbnails have .jpg extension.
                var imageFilePath = isVideo ? fileDetail.FilePath + ".jpg" : fileDetail.FilePath;

                // Create the thumbnail.

                // Read from file
                using (var imageFileStream = File.OpenRead(imageFilePath)) {
                    // Create the requested thumbnail.
                    using (var image = new MagickImage(imageFileStream)) {
                        image.Thumbnail(width, height);

                        // Save the result
                        image.Write(filePath, MagickFormat.Jpeg);
                    }
                }
            }

            await context.StreamFileAsync(filePath, "image/jpeg");
        }

    }
}
