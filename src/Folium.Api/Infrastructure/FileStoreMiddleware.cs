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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Folium.Api.Services;
using Folium.Api.FileHandlers;
using Microsoft.AspNetCore.Http.Features;

namespace Folium.Api.Infrastructure {
    public class FileStoreMiddleware {
        private readonly RequestDelegate _next;
        private List<IFileHandler> _fileHandlers;
        private IUserService _userService;

        public FileStoreMiddleware(
            RequestDelegate next,
            IEnumerable<IFileHandler> fileHandlers,
            IUserService userService) {
            _next = next;
            _fileHandlers = fileHandlers.OrderByDescending(h => h.Priority).ToList();
            _userService = userService;
        }

        public async Task InvokeAsync(HttpContext context) {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 100_000_000;

            // Get the file handler which can process this request.
            var fileHandler = _fileHandlers.FirstOrDefault(h => h.CanProcessRequest(context));

            // Check we have a file handler. 
            if(fileHandler != null) {
                // Check we are authorised to handle the file.
                if (await fileHandler.IsAuthorisedToProcessRequestAsync(context)) {
                    // Process the request.
                    await fileHandler.ProcessRequestAsync(context);
                    return;
                }
            }
            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}