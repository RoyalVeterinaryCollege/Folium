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
using Folium.Api.Models.Placement;

namespace Folium.Api.Services {
    public interface IPlacementService {
	    PlacementDto CreatePlacement(User user, PlacementDto placementDto);
		PlacementDto UpdatePlacement(User user, PlacementDto placementDto);
		void RemovePlacement(User user, Guid placementId);
		Task<IEnumerable<PlacementDto>> GetPlacementsAsync(int userId, int skip, int take);
	    Task<PlacementDto> GetPlacementAsync(Guid placementId);
		Task<IEnumerable<EntrySummaryDto>> GetPlacementEntriesAsync(User currentUser, Guid placementId, int skip, int take);
    }
    public class PlacementService : IPlacementService {
        private readonly IDbService _dbService;
		private readonly IConstructAggregates _factory;
		private readonly IRepository _repository;
		public PlacementService(
			IDbService dbService,
			IConstructAggregates factory,
			IRepository repository) {
            _dbService = dbService;
			_factory = factory;
			_repository = repository;
		}

        public PlacementDto CreatePlacement(User user, PlacementDto placementDto) {
			var id = Guid.NewGuid();
			var placementAggregate = (PlacementAggregate)_factory.Build(typeof(PlacementAggregate), id, null);
			placementAggregate.OnFirstCreated();
			placementAggregate.Create(placementDto.UserId, placementDto.Title, placementDto.Start, placementDto.End, placementDto.Reference, user.Id, placementDto.Type);
			_repository.Save(placementAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			placementDto.Id = id;
	        placementDto.FullyQualifiedTitle = PlacementAggregate.GetFullyQualifiedTitle(placementDto.Title, placementDto.Type, placementDto.Start, placementDto.End);
	        return placementDto;
		}

		public PlacementDto UpdatePlacement(User user, PlacementDto placementDto) {
			var placementAggregate = _repository.GetById<PlacementAggregate>(placementDto.Id);
			placementAggregate.Update(placementDto.Title, placementDto.Start, placementDto.End, placementDto.Reference, user.Id, placementDto.Type);
			_repository.Save(placementAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			return placementDto;
		}
		public void RemovePlacement(User user, Guid placementId) {
			var placementAggregate = _repository.GetById<PlacementAggregate>(placementId);
			placementAggregate.Remove(user.Id);
			_repository.Save(placementAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public async Task<IEnumerable<PlacementDto>> GetPlacementsAsync(int userId, int skip, int take) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var placements = await connection.QueryAsync<PlacementDto>(@" 
                    SELECT 
						[Placements].*
						,ISNULL([MaybeEntryCount], 0) AS [EntryCount]
					FROM (
						SELECT *
						FROM [dbo].[PlacementProjector.Placement]
						WHERE [UserId] = @UserId
						ORDER BY [Start] DESC
						OFFSET (@Skip) ROWS FETCH NEXT (@Take) ROWS ONLY) AS [Placements]
					LEFT JOIN (
						SELECT 
							PlacementId
							,COUNT(*) AS [MaybeEntryCount]
						FROM [dbo].[PlacementProjector.Entry]
						WHERE [UserId] = @UserId
						GROUP BY [PlacementId]) AS [EntryCounts]
						ON [Placements].[Id] = [EntryCounts].[PlacementId];",
					new {
						UserId = userId,
						Skip = skip,
						Take = take
					});
				return placements.ToList();
			}
		}

	    public async Task<PlacementDto> GetPlacementAsync(Guid placementId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				// Get the placement.
				return await connection.QueryFirstOrDefaultAsync<PlacementDto>(@" 
                    SELECT*
                    FROM [dbo].[PlacementProjector.Placement]
                    WHERE [Id] = @PlacementId
					",
					new {
						PlacementId = placementId
					});
			}
		}

	    public async Task<IEnumerable<EntrySummaryDto>> GetPlacementEntriesAsync(User currentUser, Guid placementId, int skip, int take) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var entries = await connection.QueryAsync<EntrySummaryDto, UserDto, EntryTypeDto, EntrySummaryDto>(@" 
                    SELECT [Entry].[Id]
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
                    FROM [dbo].[PlacementProjector.Entry] [Entry]
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
							ON [Entry].[TypeId] = [CourseAdmin].[EntryTypeId]
                    WHERE [PlacementId] = @PlacementId
					ORDER BY [When] DESC
					OFFSET (@Skip) ROWS FETCH NEXT (@Take) ROWS ONLY",
					(entrySummaryDto, userDto, EntryTypeDto) => {
						entrySummaryDto.Author = userDto;
						entrySummaryDto.EntryType = EntryTypeDto;
						return entrySummaryDto;
					},
					new {
						CurrentUserId = currentUser.Id,
						PlacementId = placementId,
						Skip = skip,
						Take = take
					});
				return entries.ToList();
			}
		}
    }
}