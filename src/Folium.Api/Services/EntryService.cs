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
using Folium.Api.Models;
using System.Linq;
using System.Threading.Tasks;
using CommonDomain.Persistence;
using Dapper;
using Folium.Api.Dtos;
using Folium.Api.Extensions;
using Folium.Api.Models.Entry;
using Newtonsoft.Json;
using System.Data;

namespace Folium.Api.Services {
    public interface IEntryService {
		EntryDto CreateEntry(User user, EntryDto entryDto);
	    Task<IEnumerable<EntrySummaryDto>> GetEntriesAsync(User user, int skip, int take);
	    Task<EntryDto> GetEntryAsync(User user, Guid entryId);
	    Task<EntryDto> GetEntryAsync(Guid entryId, bool includeAssessmentBundle = false);
        Task<EntrySummaryDto> GetEntrySummaryAsync(User user, Guid entryId);

        EntryDto UpdateEntry(User user, EntryDto entryDto);
	    void RemoveEntry(Guid entryId);
		Task<IEnumerable<WhereDto>> GetPlacesAsync(User user, string startsWith = "");
		Task<IEnumerable<EntryTypeDto>> GetEntryTypesAsync(IEnumerable<int> skillSetIds);
	    void RemoveCollaborator(User user, Guid entryId, int collaboratorId);
	    void ShareEntry(User user, ShareEntryDto shareEntryDto);
	    int CreateComment(EntryCommentDto entryCommentDto);
	    Task<IEnumerable<UserDto>> GetCollaboratorsAsync(Guid entryId);
        IEnumerable<UserDto> GetCollaborators(Guid entryId, IDbTransaction transaction = null);

    }
    public class EntryService : IEntryService {
        private readonly IDbService _dbService;
		private readonly IConstructAggregates _factory;
		private readonly IRepository _repository;
		public EntryService(
			IDbService dbService,
			IConstructAggregates factory,
			IRepository repository) {
            _dbService = dbService;
			_factory = factory;
			_repository = repository;
		}

        public EntryDto CreateEntry(User user, EntryDto entryDto) {
			var id = Guid.NewGuid();
			var entryAggregate = (EntryAggregate)_factory.Build(typeof(EntryAggregate), id, null);
			entryAggregate.OnFirstCreated();
			entryAggregate.Create(entryDto.SkillSetId, entryDto.Title, entryDto.DescriptionString(), user.Id, entryDto.Where, entryDto.When, entryDto.EntryType?.Id);
			entryAggregate.AddAssessmentBundle(
				entryDto.AssessmentBundle.ToDictionary(
					s => s.Key, 
					s => new SelfAssessment {
						EntryId = id,
						Score = s.Value.Score,
						SelfAssessmentLevelId = s.Value.LevelId,
						SkillId = s.Key,
						UserId = user.Id
					}));
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
	        entryDto.Id = id;
	        return entryDto;
		}

		public EntryDto UpdateEntry(User user, EntryDto entryDto) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryDto.Id);
			entryAggregate.Update(entryDto.Title, entryDto.DescriptionString(), entryDto.Where, entryDto.When);
			entryAggregate.UpdateAssessmentBundle(
				entryDto.AssessmentBundle.ToDictionary(
					s => s.Key,
					s => new SelfAssessment {
						EntryId = entryDto.Id,
						Score = s.Value.Score,
						SelfAssessmentLevelId = s.Value.LevelId,
						SkillId = s.Key,
						UserId = user.Id
					}));
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			return entryDto;
		}

		public void RemoveEntry(Guid entryId) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryId);
			entryAggregate.Remove();
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public void ShareEntry(User user, ShareEntryDto shareEntryDto) {
			var entryAggregate = _repository.GetById<EntryAggregate>(shareEntryDto.EntryId);
			entryAggregate.ShareWith(user.Id, shareEntryDto.CollaboratorIds, shareEntryDto.Message);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public void RemoveCollaborator(User user, Guid entryId, int collaboratorId) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryId);
			entryAggregate.RemoveShareWith(collaboratorId, collaboratorId);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public int CreateComment(EntryCommentDto entryCommentDto) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryCommentDto.EntryId);
			var newId = entryAggregate.CreateComment(entryCommentDto.Comment, entryCommentDto.Author.Id, entryCommentDto.CreatedAt);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			return newId;
		}

		public async Task<IEnumerable<EntrySummaryDto>> GetEntriesAsync(User user, int skip, int take) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var entries = await connection.QueryAsync<EntrySummaryDto, UserDto, EntrySummaryDto>(@" 
                    SELECT [Entry].[Id]
						,[Entry].[Title]
						,[Entry].[Where]
						,[Entry].[When]
						,[Entry].[TypeName] AS [Type]
						,[Entry].[Shared]
                        ,[Entry].[SkillSetId]
						,[User].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[User]
					ON [Entry].[UserId] = [User].[Id]
					LEFT JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
					ON [Entry].[Id] = [Sharing].[EntryId]
					AND [Sharing].[UserId] = @UserId
                    WHERE ([Entry].[UserId] = @UserId OR [Sharing].[UserId] IS NOT NULL)
					ORDER BY [When] DESC
					OFFSET (@Skip) ROWS FETCH NEXT (@Take) ROWS ONLY",
					(entrySummaryDto, userDto) => {
						entrySummaryDto.Author = userDto;
						return entrySummaryDto;
					},
					new {
						UserId = user.Id,
						Skip = skip,
						Take = take
					});
				return entries.ToList();
			}
		}

        public async Task<EntrySummaryDto> GetEntrySummaryAsync(User user, Guid entryId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var entries = await connection.QueryAsync<EntrySummaryDto, UserDto, EntrySummaryDto>(@" 
                    SELECT [Entry].[Id]
						,[Entry].[Title]
						,[Entry].[Where]
						,[Entry].[When]
						,[Entry].[TypeName] AS [Type]
						,[Entry].[Shared]
                        ,[Entry].[SkillSetId]
						,[User].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[User]
					ON [Entry].[UserId] = [User].[Id]
					LEFT JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
					ON [Entry].[Id] = [Sharing].[EntryId]
					AND [Sharing].[UserId] = @UserId
                    WHERE [Entry].[Id] = @EntryId
					AND ([Entry].[UserId] = @UserId OR [Sharing].[UserId] IS NOT NULL)",
                    (entrySummaryDto, userDto) => {
                        entrySummaryDto.Author = userDto;
                        return entrySummaryDto;
                    },
                    new {
                        EntryId = entryId,
                        UserId = user.Id
                    });
                return entries.FirstOrDefault();
            }
        }

        public async Task<EntryDto> GetEntryAsync(User user, Guid entryId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				// Get the entry.
				var data = await connection.QueryAsync<EntryDto, EntryTypeDto, UserDto, EntryDto>(@" 
                    SELECT [Entry].*, [EntryType].*, [User].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[User]
					ON [Entry].[UserId] = [User].[Id]
					LEFT JOIN [dbo].[EntryType]
					ON [EntryType].Id = [Entry].[TypeId]
					LEFT JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
					ON [Entry].[Id] = [Sharing].[EntryId]
                    WHERE [Entry].[Id] = @EntryId 
						AND ([Entry].[UserId] = @UserId OR [Sharing].[UserId] = @UserId)
					",
					(entryDto, entryTypeDto, userDto) => {
						entryDto.EntryType = entryTypeDto;
						entryDto.Author = userDto;
						return entryDto; },
					new {
						EntryId = entryId,
						UserId = user.Id
					});
				var entry = data.FirstOrDefault();
				if (entry == null) return null;
				
				// Get any skills bundled with the entry.
				var selfAssessments = await connection.QueryAsync<SelfAssessmentDto>(@" 
                    SELECT *,[SelfAssessmentLevelId] AS LevelId, @CreatedAt AS CreatedAt
                    FROM [dbo].[EntryProjector.SelfAssessment]
                    WHERE [EntryId] = @EntryId
					",
					new {
						EntryId = entryId,
						CreatedAt = entry.When
					});

				entry.AssessmentBundle = selfAssessments.ToDictionary(s => s.SkillId);

				// Get any comments with the entry.
				var comments = await connection.QueryAsync<EntryCommentDto, UserDto, EntryCommentDto>(@" 
                    SELECT *
                    FROM [dbo].[EntryProjector.EntryComment] [Comment]
					INNER JOIN [dbo].[User]
						ON [Comment].[CreatedBy] = [User].[Id]
                    WHERE [EntryId] = @EntryId
					",
					(entryCommentDto, userDto) => { entryCommentDto.Author = userDto; return entryCommentDto; },
					new {
						EntryId = entryId
					});

				entry.Comments = comments.ToList();

				return entry;
			}
		}

		public async Task<EntryDto> GetEntryAsync(Guid entryId, bool includeAssessmentBundle = false) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				// Get the entry.
				var entry = await connection.QueryFirstOrDefaultAsync<EntryDto>(@" 
                    SELECT [Id]
						,[SkillSetId]
						,[Title]
						,[Description]
						,[Where]
						,[When]
						,[UserId]
						,[LastUpdatedAt]
						,[Shared]
                    FROM [dbo].[EntryProjector.Entry]
                    WHERE [Id] = @EntryId
					",
					new {
						EntryId = entryId
					});
				if (entry == null) return null;

				if (includeAssessmentBundle) {
					// Get any skills bundled with the entry.
					var selfAssessments = await connection.QueryAsync<SelfAssessmentDto>(@" 
                    SELECT *,[SelfAssessmentLevelId] AS LevelId, @CreatedAt AS CreatedAt
                    FROM [dbo].[EntryProjector.SelfAssessment]
                    WHERE [EntryId] = @EntryId
					",
						new {
							EntryId = entryId,
							CreatedAt = entry.When
						});

					entry.AssessmentBundle = selfAssessments.ToDictionary(s => s.SkillId);
				}

				return entry;
			}
		}
		public async Task<IEnumerable<WhereDto>> GetPlacesAsync(User user, string startsWith = "") {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();

				var encodedLike = startsWith.Replace("[", "[[]").Replace("%", "[%]") + "%";
				
				var places = await connection.QueryAsync<WhereDto>(@" 
                    SELECT [Name]
						,[UsageCount]
                    FROM [dbo].[WhereProjector.Where]
                    WHERE [UserId] = @UserId
						AND [Name] LIKE @StartsWith
					ORDER BY [UsageCount] DESC",
					new {
						UserId = user.Id,
						StartsWith = encodedLike
					});
				return places.ToList();
			}
		}

	    public async Task<IEnumerable<EntryTypeDto>> GetEntryTypesAsync(IEnumerable<int> skillSetIds) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();

				var types = await connection.QueryAsync<EntryTypeDto>(@" 
                    SELECT *
                    FROM [dbo].[EntryType]
                    WHERE [SkillSetId] IN @SkillSetIds",
					new {
						SkillSetIds = skillSetIds
					});
				return types.ToList();
			}
		}

		public async Task<IEnumerable<UserDto>> GetCollaboratorsAsync(Guid entryId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				
				var collaborators = await connection.QueryAsync<UserDto>(@" 
                    SELECT *
                    FROM [dbo].[User]
					INNER JOIN [dbo].[EntryProjector.SharedWith] [SharedWith]
					ON [User].[Id] = [SharedWith].[UserId]
                    WHERE [EntryId] = @EntryId",
					new {
						EntryId = entryId
					});
				return collaborators.ToList();
			}
		}
        public IEnumerable<UserDto> GetCollaborators(Guid entryId, IDbTransaction transaction = null) {
            IDbConnection connection = null;
            if (transaction == null) {
                connection = _dbService.GetConnection();
            } else {
                connection = transaction.Connection;
            }
            var closedConnection = connection.State == ConnectionState.Closed;
            if (closedConnection) { 
                connection.Open();
            }
            try {
                var collaborators = connection.Query<UserDto>(@" 
                    SELECT *
                    FROM [dbo].[User]
					INNER JOIN [dbo].[EntryProjector.SharedWith] [SharedWith]
					ON [User].[Id] = [SharedWith].[UserId]
                    WHERE [EntryId] = @EntryId",
                    new
                    {
                        EntryId = entryId
                    });
                return collaborators.ToList();
            }
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }
	}
}