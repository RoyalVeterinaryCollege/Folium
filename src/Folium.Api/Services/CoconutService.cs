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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CommonDomain.Persistence;
using Dapper;
using Folium.Api.Dtos;
using Folium.Api.Models.Entry;
using Folium.Api.Models.File;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Folium.Api.Services {
    public interface ICoconutService {
        Task RequestEntryFileEncodingAsync(string config);
    }

    public class CoconutService : ICoconutService {
        IOptions<Configuration> _applicationConfiguration;
        private readonly ILogger<CoconutService> _logger;
        private readonly HttpClient _httpClient;

        public CoconutService(
            IOptions<Configuration> applicationConfiguration,
            HttpClient httpClient,
            ILogger<CoconutService> logger) {
            _applicationConfiguration = applicationConfiguration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RequestEntryFileEncodingAsync(string config) {
            try {
                var apiKey = _applicationConfiguration.Value.CoconutAPIKey;
                var url = $"{_applicationConfiguration.Value.CoconutApiUrl}/v1/job";

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{apiKey}:")));
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var data = new StringContent(config);
                var response = await _httpClient.PostAsync(url, data);
                var responseCode = response.StatusCode;
                if (response.IsSuccessStatusCode) {
                    // Do nothing
                    return;
                } else {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<CoconutError>(responseJson);
                    _logger.LogError("Coconut request failed with config {0}. The error code received was {1} and message {2}", config, error.ErrorCode, error.Message);
                    throw new HttpRequestException(error.Message);
                }
            } catch (HttpRequestException e) {
                _logger.LogError(e, "Coconut request failed with config: {0}", config);
            }
        }
        internal class CoconutJob {
            public int Id { get; set; }
            public string Status { get; set; }
        }

        internal class CoconutError {
            public string ErrorCode { get; set; }
            public string Message { get; set; }
            public string Status { get; set; }
        }
    }
}