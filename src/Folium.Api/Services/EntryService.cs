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

namespace Folium.Api.Services {
    public interface IEntryService {
		EntryDto CreateEntry(User user, EntryDto entryDto);
	    Task<IEnumerable<EntrySummaryDto>> GetEntriesAsync(User user, int skillSetId, int skip, int take);
	    Task<EntryDto> GetEntryAsync(User user, Guid entryId);
	    Task<EntryDto> GetEntryAsync(Guid entryId, bool includeAssessmentBundle = false);

		EntryDto UpdateEntry(User user, EntryDto entryDto);
	    void RemoveEntry(User user, Guid entryId);
		Task<IEnumerable<WhereDto>> GetPlacesAsync(User user, string startsWith = "");
		Task<IEnumerable<EntryTypeDto>> GetEntryTypesAsync(int skillSetId);

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
		public void RemoveEntry(User user, Guid entryId) {
			var entryAggregate = _repository.GetById<EntryAggregate>(entryId);
			entryAggregate.Remove();
			_repository.Save(entryAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
		}

		public async Task<IEnumerable<EntrySummaryDto>> GetEntriesAsync(User user, int skillSetId, int skip, int take) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var entries = await connection.QueryAsync<EntrySummaryDto>(@" 
                    SELECT [Id]
						,[Title]
						,[Where]
						,[When]
						,[TypeName] AS [Type]
                    FROM [dbo].[EntryProjector.Entry]
                    WHERE SkillSetId = @SkillSetId 
						AND UserId = @UserId
					ORDER BY [When] DESC
					OFFSET (@Skip) ROWS FETCH NEXT (@Take) ROWS ONLY",
					new {
						SkillSetId = skillSetId,
						UserId = user.Id,
						Skip = skip,
						Take = take
					});
				return entries.ToList();
			}
		}

	    public async Task<EntryDto> GetEntryAsync(User user, Guid entryId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				// Get the entry.
				var data = connection.Query<EntryDto, EntryTypeDto, EntryDto>(@" 
                    SELECT *
                    FROM [dbo].[EntryProjector.Entry]
					LEFT JOIN [dbo].[EntryType]
					ON [EntryType].Id = [EntryProjector.Entry].[TypeId]
                    WHERE [EntryProjector.Entry].[Id] = @EntryId 
						AND [UserId] = @UserId
					",
					(entryDto, entryTypeDto) => { entryDto.EntryType = entryTypeDto; return entryDto; },
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

	    public async Task<IEnumerable<EntryTypeDto>> GetEntryTypesAsync(int skillSetId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();

				var types = await connection.QueryAsync<EntryTypeDto>(@" 
                    SELECT *
                    FROM [dbo].[EntryType]
                    WHERE [SkillSetId] = @SkillSetId
					AND [Retired] = 0",
					new {
						SkillSetId = skillSetId
					});
				return types.ToList();
			}
		}
    }
}