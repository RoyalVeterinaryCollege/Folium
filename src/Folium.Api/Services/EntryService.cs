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
		Task<IEnumerable<EntrySummaryDto>> GetAllEntriesAsync(User currentUser, int skip, int take, EntryService.EntriesFilter? filter = null);
		Task<IEnumerable<EntrySummaryDto>> GetMyEntriesAsync(User currentUser, int skip, int take, EntryService.EntriesFilter? filter = null);
		Task<IEnumerable<EntrySummaryDto>> GetEntriesSharedWithMeAsync(User currentUser, int skip, int take, EntryService.EntriesFilter? filter = null);
		Task<IEnumerable<EntrySummaryDto>> GetEntriesSharedWithMeByUserAsync(User currentUser, int skip, int take, User sharedByUser, EntryService.EntriesFilter? filter = null);
		Task<int> GetSharedEntryCountAsync(User user, User sharedWith);
        Task<EntryDto> GetEntryAsync(User user, Guid entryId);
	    Task<EntryDto> GetEntryAsync(Guid entryId, bool includeAssessmentBundle = false);
        Task<EntrySummaryDto> GetEntrySummaryAsync(User user, Guid entryId);
        EntryDto UpdateEntry(User user, EntryDto entryDto);
        void ChangeEntrySkillGrouping(Guid entryId, int skillGroupingId);
        void RemoveEntry(Guid entryId);
		EntryCommentDto GetEntryComment(Guid entryId, int commentId, IDbTransaction transaction = null);
		Task<IEnumerable<WhereDto>> GetPlacesAsync(User user, string startsWith = "");
		Task<IEnumerable<EntryTypeDto>> GetEntryTypesAsync(IEnumerable<int> skillSetIds);
	    void RemoveCollaborator(User user, Guid entryId, int collaboratorId);
	    void ShareEntry(User user, ShareEntryDto shareEntryDto);
	    int CreateComment(EntryCommentDto entryCommentDto);
	    Task<IEnumerable<UserDto>> GetCollaboratorsAsync(Guid entryId);
        IEnumerable<UserDto> GetCollaborators(Guid entryId, IDbTransaction transaction = null);
        UserDto GetEntryAuthor(Guid entryId, IDbTransaction transaction = null);
        EntryTypeDto GetEntryType(Guid entryId);
		Task<IEnumerable<EntryFileDto>> GetEntryFilesAsync(Guid entryId);
		Task<EntryFileDetail> GetEntryFileDetailAsync(Guid entryId, Guid fileId);
		Task<IEnumerable<UserDto>> GetEntrySignOffUsersAsync(Guid entryId, bool includeCourseAdmins = false);
		void RequestSignOff(User user, EntrySignOffRequestDto entrySignOffRequestDto);
		void RemoveEntrySignOffUser(Guid entryId, int userId);
		int SignOffEntry(EntryCommentDto entryCommentDto);
	}
    public class EntryService : IEntryService {
		public enum EntriesFilter {			
			NotShared = 1,
			Shared = 2,
			PendingSignOff = 3,
			SignedOff = 4
		}
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
			entryAggregate.Create(entryDto.SkillSetId, entryDto.Title, entryDto.DescriptionString(), user.Id, entryDto.Where, entryDto.When, entryDto.EntryType?.Id, entryDto.SkillGroupingId);
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

        public void ChangeEntrySkillGrouping(Guid entryId, int skillGroupingId) {
            var entryAggregate = _repository.GetById<EntryAggregate>(entryId);
            entryAggregate.ChangeSkillGrouping(skillGroupingId);
            _repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
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
			var newId = entryAggregate.CreateComment(entryCommentDto.Comment, entryCommentDto.Author.Id, entryCommentDto.CreatedAt, entryCommentDto.FileIds);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			return newId;
		}

		public void RequestSignOff(User user, EntrySignOffRequestDto entrySignOffRequestDto) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entrySignOffRequestDto.EntryId);
			entryAggregate.RequestSignOffBy(user.Id, entrySignOffRequestDto.AuthorisedUserIds, entrySignOffRequestDto.Message);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public void RemoveEntrySignOffUser(Guid entryId, int userId) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryId);
			entryAggregate.RemoveSignOffUser(userId);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public int SignOffEntry(EntryCommentDto entryCommentDto) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryCommentDto.EntryId);
			var newId = entryAggregate.SignOff(entryCommentDto.Comment, entryCommentDto.Author.Id, entryCommentDto.CreatedAt, entryCommentDto.FileIds);
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			return newId;
		}

		public async Task<IEnumerable<EntrySummaryDto>> GetAllEntriesAsync(User currentUser, int skip, int take, EntriesFilter? filter = null) {
			return await GetEntriesAsync(currentUser, skip, take, filter:filter);
		}
		public async Task<IEnumerable<EntrySummaryDto>> GetMyEntriesAsync(User currentUser, int skip, int take, EntriesFilter? filter = null) {
			return await GetEntriesAsync(currentUser, skip, take, sharedWithMe: false, filter: filter);
		}

		public async Task<IEnumerable<EntrySummaryDto>> GetEntriesSharedWithMeAsync(User currentUser, int skip, int take, EntriesFilter? filter = null) {
			return await GetEntriesAsync(currentUser, skip, take, sharedWithMe: true, filter: filter);
		}

		public async Task<IEnumerable<EntrySummaryDto>> GetEntriesSharedWithMeByUserAsync(User currentUser, int skip, int take, User sharedByUser, EntriesFilter? filter = null) {
			return await GetEntriesAsync(currentUser, skip, take, sharedWithMe: true, sharedByUser: sharedByUser, filter: filter);
		}

		private async Task<IEnumerable<EntrySummaryDto>> GetEntriesAsync(User currentUser, int skip, int take, bool? sharedWithMe = null, User sharedByUser = null, EntriesFilter? filter = null) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var sql = @" 
                    SELECT 
						[Entry].[Id]
						,[Entry].[Title]
						,[Entry].[Where]
						,[Entry].[When]
						,[Entry].[TypeName] AS [Type]
						,[Entry].[Shared]
                        ,[Entry].[SkillSetId]
						,[Entry].[SignedOff]
						,[Entry].[IsSignOffCompatible]
						,[Entry].[SignOffRequested]
						,CASE WHEN ([SignOffRequest].[EntryId] IS NOT NULL OR [CourseAdmin].[EntryTypeId] IS NOT NULL) THEN 1 ELSE 0 END AS [IsAuthorisedToSignOff]
						,[User].*
						,[EntryType].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[User]
							ON [Entry].[UserId] = [User].[Id]
					LEFT JOIN [dbo].[EntryType]
							ON [EntryType].Id = [Entry].[TypeId]
					LEFT JOIN [dbo].[EntryProjector.SignOffRequest] [SignOffRequest]
							ON [Entry].Id = [SignOffRequest].[EntryId]
							AND [SignOffRequest].[UserId] = @CurrentUserId
					LEFT JOIN (
						SELECT DISTINCT [SkillSetEntryType].EntryTypeId
						FROM [dbo].[SkillSetEntryType]
						INNER JOIN [dbo].[CourseSkillSet]
								ON [SkillSetEntryType].[SkillSetId] = [CourseSkillSet].[SkillSetId]
						INNER JOIN [dbo].[CourseAdministrator]
								ON [CourseSkillSet].[CourseId] = [CourseAdministrator].[CourseId]
						WHERE [CourseAdministrator].[UserId] = @CurrentUserId) AS [CourseAdmin]
							ON [Entry].[TypeId] = [CourseAdmin].[EntryTypeId]";
                sql = sharedByUser == null
                    ? sql + @"
                    LEFT JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
							ON [Entry].[Id] = [Sharing].[EntryId]
							AND [Sharing].[UserId] = @CurrentUserId
                    WHERE ([Entry].[UserId] = @CurrentUserId OR [Sharing].[UserId] IS NOT NULL)"
					: sql + @"
                    INNER JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
							ON [Entry].[Id] = [Sharing].[EntryId]
							AND [Sharing].[UserId] = @CurrentUserId
                    WHERE [Entry].[UserId] = @SharedByUserUserId";

				// Add sql for the sharing param.
				if(sharedWithMe.HasValue) {
					sql = sql +
							(sharedWithMe.Value ?
							@" AND [Entry].[UserId] != @CurrentUserId" :
							@" AND [Entry].[UserId] = @CurrentUserId");
				}

				if (filter.HasValue) {
					switch (filter.Value) {
						case EntriesFilter.NotShared:
							sql = sql + @" AND [Entry].[Shared] = 0";
							break;
						case EntriesFilter.Shared:
							sql = sql + @" AND [Entry].[Shared] = 1";
							break;
						case EntriesFilter.PendingSignOff:
							sql = sql + @" AND [Entry].[SignedOff] = 0 AND [Entry].[SignOffRequested] = 1";
							break;
						case EntriesFilter.SignedOff:
							sql = sql + @" AND [Entry].[SignedOff] = 1";
							break;
						default:
							break;
					}
				}

                sql = sql + @"
					ORDER BY [When] DESC
					OFFSET (@Skip) ROWS FETCH NEXT (@Take) ROWS ONLY";

                var entries = await connection.QueryAsync<EntrySummaryDto, UserDto, EntryTypeDto, EntrySummaryDto>(
                    sql,
                    (entrySummaryDto, userDto, EntryTypeDto) => {
                        entrySummaryDto.Author = userDto;
						entrySummaryDto.EntryType = EntryTypeDto;
						return entrySummaryDto;
                    },
                    new {
                        CurrentUserId = currentUser.Id,
						SharedByUserUserId = sharedByUser == null ? -1 : sharedByUser.Id,
                        Skip = skip,
                        Take = take
                    });
                return entries.ToList();
            }
        }

        public async Task<int> GetSharedEntryCountAsync(User user, User sharedWith) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
                var sql = @" 
                    SELECT COUNT(*)
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
					ON [Entry].[Id] = [Sharing].[EntryId]
					AND [Sharing].[UserId] = @UserId
                    WHERE [Entry].[UserId] = @SharedWithUserId";
                
                var entrieCount = await connection.QuerySingleAsync<int>(
                    sql,
					new {
						UserId = user.Id,
                        SharedWithUserId = sharedWith.Id
					});
				return entrieCount;
			}
		}

        public async Task<EntrySummaryDto> GetEntrySummaryAsync(User user, Guid entryId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var entries = await connection.QueryAsync<EntrySummaryDto, UserDto, EntryTypeDto, EntrySummaryDto>(@" 
                    SELECT
						[Entry].[Id]
						,[Entry].[Title]
						,[Entry].[Where]
						,[Entry].[When]
						,[Entry].[TypeName] AS [Type]
						,[Entry].[Shared]
                        ,[Entry].[SkillSetId]
						,[Entry].[SignOffRequested]
						,[Entry].[SignedOff]
						,[Entry].[IsSignOffCompatible]						
						,CASE WHEN ([SignOffRequest].[EntryId] IS NOT NULL OR [CourseAdmin].[EntryTypeId] IS NOT NULL) THEN 1 ELSE 0 END AS [IsAuthorisedToSignOff]
						,[User].*
						,[EntryType].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[User]
							ON [Entry].[UserId] = [User].[Id]
					LEFT JOIN [dbo].[EntryType]
							ON [EntryType].Id = [Entry].[TypeId]
					LEFT JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
							ON [Entry].[Id] = [Sharing].[EntryId]
							AND [Sharing].[UserId] = @UserId
					LEFT JOIN [dbo].[EntryProjector.SignOffRequest] [SignOffRequest]
							ON [Entry].Id = [SignOffRequest].[EntryId]
							AND [SignOffRequest].[UserId] = @UserId
					LEFT JOIN (
						SELECT DISTINCT [SkillSetEntryType].EntryTypeId
						FROM [dbo].[SkillSetEntryType]
						INNER JOIN [dbo].[CourseSkillSet]
								ON [SkillSetEntryType].[SkillSetId] = [CourseSkillSet].[SkillSetId]
						INNER JOIN [dbo].[CourseAdministrator]
								ON [CourseSkillSet].[CourseId] = [CourseAdministrator].[CourseId]
						WHERE [CourseAdministrator].[UserId] = @UserId) AS [CourseAdmin]
							ON [Entry].[TypeId] = [CourseAdmin].[EntryTypeId]
                    WHERE [Entry].[Id] = @EntryId
					AND ([Entry].[UserId] = @UserId OR [Sharing].[UserId] IS NOT NULL)",
                    (entrySummaryDto, userDto, EntryTypeDto) => {
                        entrySummaryDto.Author = userDto;
						entrySummaryDto.EntryType = EntryTypeDto;
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
                    SELECT
						[Entry].*
						,CASE WHEN ([SignOffRequest].[EntryId] IS NOT NULL OR [CourseAdmin].[EntryTypeId] IS NOT NULL) THEN 1 ELSE 0 END AS [IsAuthorisedToSignOff]						
						,[EntryType].*
						,[User].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
                    INNER JOIN [dbo].[User]
							ON [Entry].[UserId] = [User].[Id]
					LEFT JOIN [dbo].[EntryType]
							ON [EntryType].Id = [Entry].[TypeId]
					LEFT JOIN [dbo].[EntryProjector.SharedWith] [Sharing]
							ON [Entry].[Id] = [Sharing].[EntryId]
					LEFT JOIN [dbo].[EntryProjector.SignOffRequest] [SignOffRequest]
							ON [Entry].Id = [SignOffRequest].[EntryId]
							AND [SignOffRequest].[UserId] = @UserId
					LEFT JOIN (
						SELECT DISTINCT [SkillSetEntryType].EntryTypeId
						FROM [dbo].[SkillSetEntryType]
						INNER JOIN [dbo].[CourseSkillSet]
								ON [SkillSetEntryType].[SkillSetId] = [CourseSkillSet].[SkillSetId]
						INNER JOIN [dbo].[CourseAdministrator]
								ON [CourseSkillSet].[CourseId] = [CourseAdministrator].[CourseId]
						WHERE [CourseAdministrator].[UserId] = @UserId) AS [CourseAdmin]
							ON [Entry].[TypeId] = [CourseAdmin].[EntryTypeId]
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
                    SELECT *
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

		public EntryCommentDto GetEntryComment(Guid entryId, int commentId, IDbTransaction transaction = null) {
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
				var comment = connection.Query<EntryCommentDto, UserDto, EntryCommentDto>(@" 
                    SELECT *
                    FROM [dbo].[EntryProjector.EntryComment] [Comment]
					INNER JOIN [dbo].[User]
						ON [Comment].[CreatedBy] = [User].[Id]
                    WHERE [EntryId] = @EntryId
						AND [Comment].[Id] = @CommentId",
					(entryCommentDto, userDto) => { entryCommentDto.Author = userDto; return entryCommentDto; },
					new {
						EntryId = entryId,
						CommentId = commentId
					}, transaction);
				return comment.FirstOrDefault();
			} finally {
				if (connection != null && closedConnection && connection is IDisposable) {
					connection.Dispose();
				}
			}
		}

		public async Task<IEnumerable<UserDto>> GetEntrySignOffUsersAsync(Guid entryId, bool includeCourseAdmins = false) {
			// This will include course admins.
			var sql = @" 
                    SELECT [User].*
                    FROM [dbo].[EntryProjector.SignOffRequest] [SignOffRequest]
                    INNER JOIN [dbo].[User]
							ON [SignOffRequest].[UserId] = [User].[Id]
                    WHERE [SignOffRequest].[EntryId] = @EntryId ";
			if(includeCourseAdmins) {
				sql = sql + @" 
					UNION					
					SELECT [User].*
                    FROM [dbo].[EntryProjector.Entry] [Entry]
					INNER JOIN [dbo].[SkillSetEntryType]
							ON [Entry].[TypeId] = [SkillSetEntryType].[EntryTypeId]
					INNER JOIN [dbo].[CourseSkillSet]
							ON [SkillSetEntryType].[SkillSetId] = [CourseSkillSet].[SkillSetId]
					INNER JOIN [dbo].[CourseAdministrator]
							ON [CourseSkillSet].[CourseId] = [CourseAdministrator].[CourseId]
                    INNER JOIN [dbo].[User] 
							ON [CourseAdministrator].[UserId] = [User].[Id]
                    WHERE [Entry].[Id] = @EntryId";
			}

			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var users = await connection.QueryAsync<UserDto>(
					sql,
					new {
						EntryId = entryId
					});
				return users;
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
                    SELECT [EntryType].*, [SkillSetId]
                    FROM [dbo].[EntryType]
                    INNER JOIN [dbo].[SkillSetEntryType]
                            ON [EntryType].Id = [SkillSetEntryType].[EntryTypeId]
                    WHERE [SkillSetId] IN @SkillSetIds",
					new {
						SkillSetIds = skillSetIds
					});
				return types.ToList();
			}
		}

        public EntryTypeDto GetEntryType(Guid entryId) {
            using (var connection = _dbService.GetConnection()) {
                connection.Open();

                var type = connection.QueryFirstOrDefault<EntryTypeDto>(@" 
                    SELECT [EntryType].*
                    FROM [dbo].[EntryType]
                    INNER JOIN [dbo].[EntryProjector.Entry] [Entry]
							ON [EntryType].Id = [Entry].[TypeId]
                    WHERE [Entry].[Id] =  @EntryId",
                    new {
                        EntryId = entryId
                    });
                return type;
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
                    }, transaction);
                return collaborators.ToList();
            }
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }

        public UserDto GetEntryAuthor(Guid entryId, IDbTransaction transaction = null) {
            IDbConnection connection = null;
            if (transaction == null) {
                connection = _dbService.GetConnection();
            }
            else {
                connection = transaction.Connection;
            }
            var closedConnection = connection.State == ConnectionState.Closed;
            if (closedConnection) {
                connection.Open();
            }
            try {
                var author = connection.QuerySingle<UserDto>(@" 
                    SELECT [User].*
                    FROM [dbo].[User]
					INNER JOIN [dbo].[EntryProjector.Entry] [Entry]
					ON [User].[Id] = [Entry].[UserId]
                    WHERE [Entry].[Id] = @EntryId",
                    new {
                        EntryId = entryId
                    }, transaction);
                return author;
            }
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }

		public async Task<IEnumerable<EntryFileDto>> GetEntryFilesAsync(Guid entryId) { 
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();

				var entryFiles = await connection.QueryAsync<EntryFileDto>(@" 
                    SELECT [EntryFile].*, CommentId, [User].[FirstName] + ' ' + [User].[LastName] AS [CreatedByName]
                    FROM [dbo].[EntryProjector.EntryFile] [EntryFile]
					LEFT JOIN [dbo].[EntryProjector.EntryCommentFile] [EntryCommentFile]
							ON [EntryFile].[EntryId] = [EntryCommentFile].[EntryId]
							AND [EntryFile].[FileId] = [EntryCommentFile].[FileId]
					INNER JOIN [dbo].[User]
						ON [EntryFile].[CreatedBy] = [User].[Id]
                    WHERE [EntryFile].[EntryId] = @EntryId
					AND ([OnComment] = 0 OR ([OnComment] = 1 AND [CommentId] IS NOT NULL))",
					new {
						EntryId = entryId
					});
				return entryFiles.ToList();
			}
		}

		public async Task<EntryFileDetail> GetEntryFileDetailAsync(Guid entryId, Guid fileId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();

				var entryFileDetail = await connection.QueryAsync<EntryFileDetail>(@" 
                    SELECT [EntryFile].*
                    FROM [dbo].[EntryProjector.EntryFile] [EntryFile]
                    WHERE [EntryId] = @EntryId
					AND [FileId] = @FileId;",
					new {
						EntryId = entryId,
						FileId = fileId
					});
				return entryFileDetail.FirstOrDefault();
			}
		}

	}
}