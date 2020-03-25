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
using System.Threading.Tasks;
using CommonDomain.Persistence;
using Dapper;
using Folium.Api.Dtos;
using Folium.Api.Models.Entry;
using Folium.Api.Models.File;
using Microsoft.Extensions.Logging;

namespace Folium.Api.Services {
    public interface IFileService {
        Task CreateFileAsync(Guid fileId, int userId, string fileName, string filePath, string fileType, Stream file);
        Task CreateEntryFileAsync(Guid fileId, int userId, string fileName, string filePath, string fileType, Stream file, Guid entryId, bool onComment);
        Task<FileDto> GetFileDtoAsync(Guid fileId);
        Task<bool> IsFileCreatorAsync(Guid fileId, int userId);
        void DeleteFile(Guid fileId);
        void DeleteEntryFile(Guid fileId, Guid entryId);
        Task DeleteAllEntryFilesAsync(Guid entryId);
        Task<FileDetail> GetFileDetailAsync(Guid fileId);
    }

    public class FileService : IFileService {
        private readonly IDbService _dbService;
        private readonly IConstructAggregates _factory;
        private readonly IRepository _repository;
        private readonly ILogger<FileService> _logger;

        public FileService(
            IDbService dbService,
            IConstructAggregates factory,
            IRepository repository,
            ILogger<FileService> logger) {
            _dbService = dbService;
            _factory = factory;
            _repository = repository;
            _logger = logger;
        }

        public async Task CreateFileAsync(Guid fileId, int userId, string fileName, string filePath, string fileType, Stream file) {
            var fileAggregate = _repository.GetById<FileAggregate>(fileId);
            if (fileAggregate.Version > 0) {
                return; // Aggregate already exists.
            }

            fileAggregate = (FileAggregate)_factory.Build(typeof(FileAggregate), fileId, null);

            // Call the FileAggregate to record the file creation.
            fileAggregate.OnFirstCreated();
            fileAggregate.Create(userId, fileName, filePath, fileType, file.Length);
            _repository.Save(fileAggregate, commitId: Guid.NewGuid(), updateHeaders: null);

            // Copy the file to the new location.
            var path = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            using (var fileStream = File.Create(filePath)) {
                await file.CopyToAsync(fileStream);
            }
        }

        public async Task CreateEntryFileAsync(Guid fileId, int userId, string fileName, string filePath, string fileType, Stream file, Guid entryId, bool onComment) {
            // Create the File.
            await CreateFileAsync(fileId, userId, fileName, filePath, fileType, file);

            var entryAggregate = _repository.GetById<EntryAggregate>(entryId);

            // Call the EntryAggregate to record the file creation.            
            if (entryAggregate.Files.Count(f => f.FileId == fileId) == 0) {
                entryAggregate.AddFile(fileId, userId, onComment, fileName, filePath, fileType, file.Length);
                _repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
            }
        }

        public void DeleteFile(Guid fileId) { 
            var fileAggregate = _repository.GetById<FileAggregate>(fileId);
            var filePath = fileAggregate.FilePath;
            fileAggregate.Remove();
            DeleteFile(filePath);
            _repository.Save(fileAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
        }

        public void DeleteEntryFile(Guid fileId, Guid entryId) {            
            // Delete the file.
            DeleteFile(fileId);

            // Remove the file from the Entry Aggregate.
            var entryAggregate = _repository.GetById<EntryAggregate>(entryId);
            var file = entryAggregate.Files.FirstOrDefault(f => f.FileId == fileId);
            var filePath = file.FilePath;
            var encodedVideoDirectoryPath = file.EncodedVideoDirectoryPath;
            entryAggregate.RemoveFile(fileId);

            var fileDirectoryPath = Path.GetDirectoryName(filePath);

            // Delete all other files in the folder (thumbnails etc) and the containing folder.
            DeleteDirectory(fileDirectoryPath);

            // If there is also associated audiovideo encodings, delete that content too.
            if (!string.IsNullOrEmpty(encodedVideoDirectoryPath)) {
                var files = Directory.GetFiles(encodedVideoDirectoryPath, fileId + "*");
                foreach (var matchedFilePath in files) {
                    DeleteFile(matchedFilePath);
                }
            }
            _repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
        }
        
        public async Task DeleteAllEntryFilesAsync(Guid entryId) {
            List<Guid> entryFileIds;
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();

                var results = await connection.QueryAsync<Guid>(@" 
                    SELECT [EntryFile].[FileId]
                    FROM [dbo].[EntryProjector.EntryFile] [EntryFile]
                    WHERE [EntryId] = @EntryId",
                    new {
                        EntryId = entryId
                    });
                entryFileIds = results.ToList();
            }

            foreach (var fileId in entryFileIds) {
                DeleteEntryFile(fileId, entryId);
            }
        }

        public async Task<FileDto> GetFileDtoAsync(Guid fileId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                // Get the file.
                var data = await connection.QueryAsync<FileDto>(@" 
                    SELECT [File].*
                    FROM [dbo].[FileProjector.File] [File]
                    WHERE [File].[Id] = @FileId
					",
                    new {
                        FileId = fileId
                    });
                return data.FirstOrDefault();
            }
        }

        public async Task<FileDetail> GetFileDetailAsync(Guid fileId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                // Get the file path.
                var data = await connection.QueryAsync<FileDetail>(@" 
                    SELECT [File].*
                    FROM [dbo].[FileProjector.File] [File]
                    WHERE [File].[Id] = @FileId
					",
                    new {
                        FileId = fileId
                    });
                return data.FirstOrDefault();
            }
        }

        public async Task<bool> IsFileCreatorAsync(Guid fileId, int userId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                // Get the file.
                var data = await connection.QueryAsync<bool>(@" 
                    SELECT CASE WHEN [CreatedBy] = @UserId THEN 1 ELSE 0 END
                    FROM [dbo].[FileProjector.File] [File]
                    WHERE [File].[Id] = @FileId
					",
                    new {
                        FileId = fileId,
                        UserId = userId
                    });
                var result = data.FirstOrDefault();

                return result;
            }
        }

        private void DeleteFile(string filePath) { 
            if(File.Exists(filePath)) { 
                try { 
                    File.Delete(filePath);
                }
                catch(Exception e) {
                    // Warn that the file didn't delete.
                    _logger.LogWarning(e, $"Unable to delete File at path {filePath}.");
                }
            } else {
                // Warn that the file wasn't there to delete.
                _logger.LogWarning($"Unable to delete File at path {filePath} as it does not exist.");
            }
        }

        private void DeleteDirectory(string directoryPath) {
            try {
                Array.ForEach(Directory.GetFiles(directoryPath), DeleteFile);
                Directory.Delete(directoryPath);
            } catch (Exception e) {
                // Warn that the directory didn't delete.
                _logger.LogWarning(e, $"Unable to delete files and the directory at path {directoryPath}.");
            }
        }
    }
}