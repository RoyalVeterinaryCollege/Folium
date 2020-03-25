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
using Folium.Api.FileHandlers;
using Folium.Api.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace Folium.Api.Infrastructure
{
    public interface ITusEventHandler {
        Events Events { get; }
        Task OnAuthorizeAsync(AuthorizeContext context);
        Task OnBeforeCreateAsync(BeforeCreateContext context);
        Task OnBeforeDeleteAsync(BeforeDeleteContext context);
        Task OnFileCompleteAsync(FileCompleteContext context);
        Task OnDeleteCompleteAsync(DeleteCompleteContext context);
    }

    public class TusEventHandler : ITusEventHandler {

        private readonly List<IFileHandler> _fileHandlers;
        private readonly IUserService _userService;
        private readonly ILogger<TusEventHandler> _logger;

        public TusEventHandler(
            IEnumerable<IFileHandler> fileHandlers,
            IUserService userService,
            ILogger<TusEventHandler> logger) {
            _fileHandlers = fileHandlers.OrderByDescending(h => h.Priority).ToList();
            _userService = userService;
            _logger = logger;
        }

        public Events Events => new Events {
            OnAuthorizeAsync = OnAuthorizeAsync,
            OnFileCompleteAsync = OnFileCompleteAsync,
            OnBeforeCreateAsync = OnBeforeCreateAsync,
            OnBeforeDeleteAsync = OnBeforeDeleteAsync,
            OnDeleteCompleteAsync = OnDeleteCompleteAsync
        };

        public Task OnAuthorizeAsync(AuthorizeContext context) {
            if (!context.HttpContext.User.Identity.IsAuthenticated) {
                context.FailRequest(HttpStatusCode.Unauthorized);
            }
            return Task.CompletedTask;
        }


        public async Task OnFileCompleteAsync(FileCompleteContext context) {
            // File has been uploaded.
            // Get the file handler to save the file. This will move it from the TUS upload folder to the final place on disk and create a file aggregate.
            
            // Get the file and its metadata.
            var file = await context.GetFileAsync();
            System.Diagnostics.Debug.WriteLine("OnFileCompleteAsync for file id " + file.Id);
            var metadata = await file.GetMetadataAsync(context.CancellationToken);

            // Get the handler for the file.
            var fileHandler = _fileHandlers.FirstOrDefault(h => h.CanSaveFile(metadata));

            // Get the current user.
            var user = await _userService.GetUserAsync(context.HttpContext.User);

            await fileHandler.SaveFileAsync(user, file, context.CancellationToken);
            await ((TusDiskStore)context.Store).DeleteFileAsync(file.Id, context.CancellationToken);
        }

        public async Task OnBeforeCreateAsync(BeforeCreateContext context) {
            // Before the file is uploaded, check we have a handler to process it and that the user is authorised to save the file.

            // Get the metadata for the file.
            var metadata = context.Metadata;

            // Get the first handler for the file.
            var fileHandler = _fileHandlers.FirstOrDefault(h => h.CanSaveFile(metadata));

            // Check we have a handler to process the file.
            if (fileHandler == null) {
                // No handlers can save the file.
                context.FailRequest(HttpStatusCode.BadRequest, "No handlers support saving this file.");
                return;
            }

            // Get the current user.
            var user = await _userService.GetUserAsync(context.HttpContext.User);

            // Validate we can save the file.
            var canSaveFile = await fileHandler.IsAuthorisedToSaveFileAsync(user, metadata);
            if (!canSaveFile) {
                context.FailRequest(HttpStatusCode.Unauthorized, "Not authorised to save the file.");
                return;
            }
        }

        public Task OnBeforeDeleteAsync(BeforeDeleteContext context) {
            return Task.CompletedTask;
        }

        public Task OnDeleteCompleteAsync(DeleteCompleteContext context) {
            return Task.CompletedTask;
        }
    }
}
