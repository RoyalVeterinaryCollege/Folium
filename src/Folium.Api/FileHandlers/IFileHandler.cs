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
using Folium.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace Folium.Api.FileHandlers
{
    public interface IFileHandler {

        /// <summary>
        /// The priority of the FileHandler.
        /// </summary>
        int Priority {
            get;
        }
        
        /// <summary>
        /// Whether the user is authorised to save the file.
        /// </summary>
        /// <param name="fileMetadata"></param>
        /// <returns></returns>
        Task<bool> IsAuthorisedToSaveFileAsync(User user, Dictionary<string, Metadata> fileMetadata);

        /// <summary>
        /// Can this handler save the file, based on the provided metadata.
        /// </summary>
        /// <param name="fileMetadata"></param>
        /// <returns></returns>
        bool CanSaveFile(Dictionary<string, Metadata> fileMetadata);

        /// <summary>
        /// Save the tus file. 
        /// </summary>
        /// <param name="file">The file to save.</param>
        /// <param name="CancellationToken">The web request cancellation token..</param>
        Task SaveFileAsync(User user, ITusFile file, CancellationToken cancellationToken);

        /// <summary>
        /// Whether the user is authorised to make the request.
        /// </summary>
        /// <returns></returns>
        Task<bool> IsAuthorisedToProcessRequestAsync(HttpContext context);

        /// <summary>
        /// Can this file handler process the request, based on the request path.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        bool CanProcessRequest(HttpContext context);

        /// <summary>
        /// Process the request for the file, performing any business logic.
        /// </summary>
        /// <param name="context"></param>
        Task ProcessRequestAsync(HttpContext context);
    }
}
