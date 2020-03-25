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
using Dapper;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonDomain.Persistence;
using EventSaucing.Aggregates;
using Folium.Api.Dtos;
using Folium.Api.Extensions;
using Folium.Api.Models.SelfAssessing;
using Microsoft.Extensions.Logging;
using CommonDomain;

namespace Folium.Api.Services {
    public interface ISelfAssessmentService {
        Task<IReadOnlyList<SelfAssessment>> GetSelfAssessmentsAsync(int skillSetId, int userId);
		Task<IReadOnlyList<SelfAssessmentScale>> GetSelfAssessmentScalesAsync(int skillSetId);
		void CreateSelfAssessments(User user, int skillSetId, Dictionary<int, SelfAssessmentDto> selfAssessments, EntryDto entry = null);
		Dictionary<int, SelfAssessment> RemoveSelfAssessments(User user, int skillSetId, Dictionary<int, SelfAssessmentDto> selfAssessments, EntryDto entry);

	}
	public class SelfAssessmentService : ISelfAssessmentService {
        private readonly IDbService _dbService;
		private readonly IConstructAggregates _factory;
		private readonly IRepository _repository;
		private readonly ILogger<SelfAssessmentService> _logger;

		public SelfAssessmentService(
			IDbService dbService,
			IConstructAggregates factory,
			IRepository repository,
			ILogger<SelfAssessmentService> logger) {
			_dbService = dbService;
			_factory = factory;
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<SelfAssessment>> GetSelfAssessmentsAsync(int skillSetId, int userId) {
            using(var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var selfAssessments = await connection.QueryAsync<SelfAssessment>(@" 
                    SELECT *
                    FROM [dbo].[SelfAssessmentProjector.SelfAssessment]
                    WHERE UserId = @UserId 
                    AND SkillSetId = @SkillSetId", 
                    new {
                        UserId = userId,
                        SkillSetId = skillSetId
                    });
                return selfAssessments.ToList();
            }
		}
		public async Task<IReadOnlyList<SelfAssessmentScale>> GetSelfAssessmentScalesAsync(int skillSetId) {
			var sql = @"
                SELECT DISTINCT {0}.*
                FROM [dbo].[SelfAssessmentScale]
                INNER JOIN [dbo].[SelfAssessmentLevel]
                        ON SelfAssessmentScale.Id = SelfAssessmentLevel.SelfAssessmentScaleId
                INNER JOIN [dbo].[Skill]
                        ON SelfAssessmentScale.Id = Skill.SelfAssessmentScaleId
                WHERE Skill.SkillSetId = @SkillSetId
            ";
			List<SelfAssessmentScale> selfAssessmentScales;
			List<SelfAssessmentLevel> selfAssessmentLevels;
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				using (var multi = await connection.QueryMultipleAsync(
					string.Format(sql, "SelfAssessmentScale") + string.Format(sql, "SelfAssessmentLevel"),
					new { SkillSetId = skillSetId })) {
					selfAssessmentScales = (await multi.ReadAsync<SelfAssessmentScale>()).ToList();
					selfAssessmentLevels = (await multi.ReadAsync<SelfAssessmentLevel>()).ToList();
				}
			}
			// Loop the scales, add the levels.
			foreach (var selfAssessmentScale in selfAssessmentScales) {
				selfAssessmentScale.Levels = selfAssessmentLevels
					.Where(s => s.SelfAssessmentScaleId == selfAssessmentScale.Id)
					.OrderBy(s => s.Score)
					.ToList();
			}
			return selfAssessmentScales;
		}

		public void CreateSelfAssessments(User user, int skillSetId, Dictionary<int, SelfAssessmentDto> selfAssessments, EntryDto entry = null) {
			if (selfAssessments.Count == 0) return;
			_logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: CreateSelfAssessments({user.Id}, {skillSetId}, {string.Join(",", selfAssessments.Keys)}, {entry?.Id}) called.\n");
			// Get the UserSkillsAggregate.
			var aggregate = GetUserSkillSetAggregate(user.Id, skillSetId);
			// Loop the self assessments, adding them to the aggregate.
			foreach (var selfAssessment in selfAssessments) {
				aggregate.AddSelfAssessment(
					skillSetId,
					new SelfAssessment
					{
						Score = selfAssessment.Value.Score,
						SelfAssessmentLevelId = selfAssessment.Value.LevelId,
						SkillId = selfAssessment.Key,
						EntryId = entry?.Id,
						UserId = user.Id,
						CreatedAt = entry?.When ?? selfAssessment.Value.CreatedAt
					});

                // There can be a number of events created when adding all the assessments to an entry and there is a 255 limit per save, so check if we are getting close.
                if(((IAggregate)aggregate).GetUncommittedEvents().Count > 200) {
                    _logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: CreateSelfAssessments({user.Id}, {skillSetId}, {string.Join(",", selfAssessments.Keys)}, {entry?.Id}) called save on aggregate: ({aggregate.Id}, {aggregate.Version}.\n");
                    _repository.Save(aggregate, commitId: Guid.NewGuid(), updateHeaders: null);
                }
            }
            if (((IAggregate)aggregate).GetUncommittedEvents().Count > 0) {
                _logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: CreateSelfAssessments({user.Id}, {skillSetId}, {string.Join(",", selfAssessments.Keys)}, {entry?.Id}) called save on aggregate: ({aggregate.Id}, {aggregate.Version}.\n");
                _repository.Save(aggregate, commitId: Guid.NewGuid(), updateHeaders: null);
            }
		}
		/// <summary>
		/// Removes all the supplied self assessments and returns a new dictionary of the latest self assessments.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="skillSetId"></param>
		/// <param name="selfAssessments"></param>
		/// <param name="entry"></param>
		/// <returns></returns>
		public Dictionary<int, SelfAssessment> RemoveSelfAssessments(User user, int skillSetId, Dictionary<int, SelfAssessmentDto> selfAssessments, EntryDto entry) {
			var latestSelfAssessments = new Dictionary<int, SelfAssessment>();
			if (selfAssessments.Count == 0) return latestSelfAssessments;
			_logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: RemoveSelfAssessments({user.Id}, {skillSetId}, {string.Join(",", selfAssessments.Keys)}, {entry.Id}) called.\n");
			// Self Assessments maybe removed, if they are attached to entries
			// Get the UserSkillsAggregate.
			var aggregate = GetUserSkillSetAggregate(user.Id, skillSetId);
			// Loop the self assessments, removing them from the aggregate.
			foreach (var selfAssessment in selfAssessments) {
				var latest = aggregate.RemoveSelfAssessment(
					skillSetId,
					new SelfAssessment
					{
						Score = selfAssessment.Value.Score,
						SelfAssessmentLevelId = selfAssessment.Value.LevelId,
						SkillId = selfAssessment.Key,
						EntryId = entry.Id,
						UserId = user.Id,
						CreatedAt = entry.When
					});
				latestSelfAssessments.Add(selfAssessment.Key, latest);

                // There can be a number of events created when adding all the assessments to an entry and there is a 255 limit per save, so check if we are getting close.
                if (((IAggregate)aggregate).GetUncommittedEvents().Count > 200) {
                    _logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: RemoveSelfAssessments({user.Id}, {skillSetId}, {string.Join(",", selfAssessments.Keys)}, {entry?.Id}) called save on aggregate: ({aggregate.Id}, {aggregate.Version}.\n");
                    _repository.Save(aggregate, commitId: Guid.NewGuid(), updateHeaders: null);
                }
            }
            if (((IAggregate)aggregate).GetUncommittedEvents().Count > 0) {
                _logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: RemoveSelfAssessments({user.Id}, {skillSetId}, {string.Join(",", selfAssessments.Keys)}, {entry?.Id}) called save on aggregate: ({aggregate.Id}, {aggregate.Version}.\n");
                _repository.Save(aggregate, commitId: Guid.NewGuid(), updateHeaders: null);
            }
			return latestSelfAssessments;
		}

		private SelfAssessingAggregate GetUserSkillSetAggregate(int userId, int skillSetId) {
			_logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: GetUserSkillSetAggregate({userId}, {skillSetId}) called.\n");
			// We use a deterministic id for this aggregate as we need to access it using the userId & skillsSetId.
			var id = AggregateIdentityGenerator.CreateDeterministicGuid($"{userId}{skillSetId}");
			// Get the aggregate.
			var aggregate = _repository.GetById<SelfAssessingAggregate>(id);
			_logger.LogDebug($"\n{DateTime.UtcNow.ToUnixTimeMilliseconds()} - {Thread.CurrentThread.ManagedThreadId}: GetUserSkillSetAggregate({userId}, {skillSetId}) - Aggregate.Version: {aggregate.Version}.\n");
			// Check we have the aggregate, otherwise create it.
			if (aggregate.Version == 0) {
				aggregate = (SelfAssessingAggregate)_factory.Build(typeof(SelfAssessingAggregate), id, null);
				aggregate.OnFirstCreated();
				aggregate.Create(userId, skillSetId);
				_repository.Save(aggregate, commitId: Guid.NewGuid(), updateHeaders: null);
			}
			return aggregate;
		}
	}
}