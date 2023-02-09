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
using Folium.Api.Models.Entry;
using Folium.Api.Services;
using Hangfire.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using static Folium.Api.FileHandlers.EntryAudioVideoFileHandler;

namespace Folium.Api.FileHandlers
{
    /// <summary>
    /// Video file handler for video files on Entries.
    /// Is responsible for:
    ///     * requesting new video encodings.    
    /// </summary>
    public class EntryAudioVideoFileHandler : EntryFileHandler {
        
        public override int Priority => 10;

        private readonly IOptions<Configuration> _applicationConfiguration;
        private readonly ICoconutService _coconutService;
        private readonly ILogger<EntryAudioVideoFileHandler> _logger;

        private const string CoconutRequestPath = "/coconut";
        //private string CoconutConfig(string apiUrl, string hash, Guid entryId, Guid fileId) => $@"

        //    set input = {Url}{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}
        //    set storage = {Url}{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}
        //    set notification = {Url}{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}
        //    set outputs=
        //    -> mp4:720p  = {apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}&{FormatKey}=mp4

        //    -> jpg:480x = {apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}&{FormatKey}=jpg";



        private const string HashKey = "hash";
        private const string FormatKey = "format";
        private const string EntryIdKey = "entry_id";
        private const string FileIdKey = "file_id";
        

        public string CoconutConfig(string apiUrl, string hash, Guid entryId, Guid fileId)
        {
            var requestObject = new RequestObj
            {
                input = new Dictionary<string, string>
                {
                    ["url"] = $@"{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}"
                },
                notification = new Dictionary<string, string>
                {
                    ["type"] = "http",
                    ["url"] = $@"{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}"
                   
                },
                outputs = new Dictionary<string, Dictionary<string, string>>
                {
                    ["mp4:720p"] = new Dictionary<string, string>()
                    {
                        ["url"] = $@"{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}&{FormatKey}=mp4"
                    },
                    ["webm"] = new Dictionary<string, string>()
                    {
                        ["url"] = $@"{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}&{FormatKey}=webm"
                    },
                    ["jpg:480x"] = new Dictionary<string, string>()
                    {
                        ["url"] = $@"{apiUrl}?{HashKey}={hash}&{EntryIdKey}={entryId}&{FileIdKey}={fileId}&{FormatKey}=jpg"
                    }
                }
            };
            return JsonConvert.SerializeObject(requestObject);
        }

public EntryAudioVideoFileHandler(
            IHostingEnvironment hostingEnvironment,
            IFileService fileService,
            IConstructAggregates factory,
            IRepository repository,
            IEntryService entryService,
            IUserService userService,
            ICoconutService coconutService,
            ILogger<EntryAudioVideoFileHandler> logger,
            IOptions<Configuration> applicationConfiguration) : base(hostingEnvironment, fileService, factory, repository, entryService, userService, logger) {
            _applicationConfiguration = applicationConfiguration;
            _coconutService = coconutService;
            _logger = logger;
        }
        
        public override bool CanSaveFile(Dictionary<string, Metadata> fileMetadata) {
            return fileMetadata.ContainsKey("type") && (fileMetadata["type"].GetString(Encoding.UTF8).StartsWith("video", StringComparison.OrdinalIgnoreCase) || fileMetadata["type"].GetString(Encoding.UTF8).StartsWith("audio", StringComparison.OrdinalIgnoreCase));
        }

        public override async Task SaveFileAsync(User user, ITusFile file, CancellationToken cancellationToken) {
            await base.SaveFileAsync(user, file, cancellationToken);

            var maybeFileId = file.Id;
            var metadata = await file.GetMetadataAsync(cancellationToken);
            var maybeEntryId = GetEntryIdFromMetadata(metadata);
            var coconutRequestUrl = $"{_applicationConfiguration.Value.CoconutWebHookBaseUrl}{CoconutRequestPath}";
            if (Guid.TryParse(maybeFileId, out Guid fileId) && Guid.TryParse(maybeEntryId, out Guid entryId)) {
                // Request coconut encoding.
                var coconutConfig = CoconutConfig(coconutRequestUrl, GetRequestHash(fileId, entryId), entryId, fileId);
                try {
                    await _coconutService.RequestEntryFileEncodingAsync(coconutConfig);
                } catch(HttpRequestException) {
                    // This will have been logged, do nothing.
                }
            }
        }

        public override bool CanProcessRequest(HttpContext context) {
            if (IsPostRequest(context) || IsGetRequest(context) || IsHeadRequest(context)) {
                // We only handle requests on the coconut request path, or for entry files with a format querystring.
                return IsCoconutRequest(context.Request) || (context.Request.Query.ContainsKey(FormatKey) && base.CanProcessRequest(context));
            } else {
                return false; // Don't handle other requests.
            }
        }

        public override Task<bool> IsAuthorisedToProcessRequestAsync(HttpContext context) {
            // Entry file or coconut request?
            if (IsCoconutRequest(context.Request)) {
                // A Coconut request is only authorised if it:
                // * Has a file id param
                // * Has an entry id param
                // * Has a hash param
                // * The hash param correctly matches the generated HMAC
                var fileIdParam = context.Request.Query[FileIdKey];
                var entryIdParam = context.Request.Query[EntryIdKey];
                var hashParam = context.Request.Query[HashKey];

                if(fileIdParam == StringValues.Empty || entryIdParam == StringValues.Empty || hashParam == StringValues.Empty) {
                    return Task.FromResult(false);
                }

                if (Guid.TryParse(fileIdParam, out Guid fileId) && Guid.TryParse(entryIdParam, out Guid entryId)) {
                    var hash = GetRequestHash(fileId, entryId);
                    return Task.FromResult(hashParam.Equals(hash));
                }
                return Task.FromResult(false);

            } else {
                // Entry file request.
                // No Authorization is current done on these requests as they will be coming from a standard http get for the video/audio
                // Just check we are only requesting either mp4 or webm.
                string format = context.Request.Query[FormatKey];
                return Task.FromResult(format.Equals("mp4") || format.Equals("webm"));
            }
        }

        internal override async Task UploadFileAsync(HttpContext context) {
            var fileId = context.Request.Query[FileIdKey];
            var entryId = context.Request.Query[EntryIdKey];
            var format = context.Request.Query[FormatKey];
            if (context.Request.HasFormContentType) {
                var form = await context.Request.ReadFormAsync();
                var filename = $"{fileId}.{format}";
                string filePath;
                switch (format.ToString()) {
                    case "mp4":
                    case "webm":
                        filePath = GetEncodedAudioVideoFilePath(entryId, filename);
                        break;
                    case "jpg":
                        filePath = base.GetFilePath(entryId, fileId, filename);
                        break;
                    default:
                        // Do nothing.
                        return;
                }
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                using (var fileStream = File.Create(filePath)) {
                    form.Files[0].CopyTo(fileStream);
                }
            } else {
                // This must be a post for the webhook from coconut.
                var serializer = new JsonSerializer();
                Response response;
                using (var streamReader = new HttpRequestStreamReader(context.Request.Body, Encoding.UTF8)) {
                    using (var jsonReader = new JsonTextReader(streamReader)) {
                        response = serializer.Deserialize<Response>(jsonReader);
                    }
                }

                if (response.@event == "job.completed") {
                    var entryAggregate = _repository.GetById<EntryAggregate>(Guid.Parse(entryId));
                    var filePath = GetEncodedAudioVideoFilePath(entryId, fileId);
                    var directory = Path.GetDirectoryName(filePath);
                    entryAggregate.AudioVideoFileEncoded(Guid.Parse(fileId), directory);
                    _repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
                } else {
                    // Log the errors.
                    _logger.LogError("Coconut encoding job with id {0} responded with errors {1}", response.job_id, JsonConvert.SerializeObject((object)response.@event));
                }
            }
        }

        internal override async Task RequestFileAsync(HttpContext context) {
            var fileId = GetFileIdFromRequest(context.Request);
            var entryId = GetEntryIdFromRequest(context.Request);

            var fileDetail = await _entryService.GetEntryFileDetailAsync(Guid.Parse(entryId), Guid.Parse(fileId));

            if (IsGetRequest(context)) {
                string filePath;
                string type;
                switch (context.Request.Query[FormatKey]) {
                    case "mp4":
                        filePath = $"{fileDetail.EncodedAudioVideoDirectoryPath}{Path.DirectorySeparatorChar}{fileId}.mp4";
                        type = "video/mp4";
                        break;
                    case "webm":
                        filePath = $"{fileDetail.EncodedAudioVideoDirectoryPath}{Path.DirectorySeparatorChar}{fileId}.webm";
                        type = "video/webm";
                        break;
                    default:
                        filePath = fileDetail.FilePath;
                        type = fileDetail.Type;
                        break;
                }

                await context.StreamFileAsync(filePath, type);
                return;
            }
            if(IsHeadRequest(context)) {
                context.Response.Headers.Add("Content-Length", fileDetail.Size.ToString());
                return;
            }
        }

        internal new string GetEntryIdFromRequest(HttpRequest request) {
            // Check if we have the entry id is on the querystring
            if (request.Query.ContainsKey(EntryIdKey)) {
                return request.Query[EntryIdKey];
            }
            return base.GetEntryIdFromRequest(request);
        }
        
        internal new string GetFileIdFromRequest(HttpRequest request) {
            // Check if we have the file id is on the querystring
            if(request.Query.ContainsKey(FileIdKey)) {
                return request.Query[FileIdKey];
            }
            return base.GetFileIdFromRequest(request);
        }

        /// <summary>
        /// Generate a HMAC.
        /// </summary>
        private string GetRequestHash(Guid fileId, Guid entryId) {
            var encoding = new UTF8Encoding();

            var textBytes = encoding.GetBytes(fileId.ToString() + entryId.ToString());
            var keyBytes = encoding.GetBytes(_applicationConfiguration.Value.CoconutAPIKey);
                        
            using (var hash = new HMACSHA256(keyBytes)) {
                var hashBytes = hash.ComputeHash(textBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private bool IsCoconutRequest(HttpRequest request) {
            return request.Path.StartsWithSegments(new PathString(CoconutRequestPath));
        }

        private string GetEncodedAudioVideoFilePath(string entryId, string filename) {
            // Directory is at {contentroot}/file-uploads/encoded-av/{entryId}/{filename]
            return $"{_hostingEnvironment.ContentRootPath}{Path.DirectorySeparatorChar}file-uploads{Path.DirectorySeparatorChar}encoded-av{Path.DirectorySeparatorChar}{entryId}{Path.DirectorySeparatorChar}{filename}";
        }

        internal class Response {
            public List<Output> outputs { get; set; }
            public string job_id { get; set; }
            public dynamic @event { get; set; }
        }

        public class Output
        {
            public string key { get; set; }
            public string type { get; set; }
            public string format { get; set; }
            public string url { get; set; }
            public string status { get; set; }
        }

        public class RequestObj
        {
            public Dictionary<string, string> input { get; set; }
            public Dictionary<string, string> notification { get; set; }
            public Dictionary<string, string> storage { get; set; }
            public Dictionary<string, Dictionary<string, string>> outputs { get; set; }
        }
    }
}
