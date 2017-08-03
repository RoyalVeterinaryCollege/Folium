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
using System.Data;
using Dapper;
using EventSaucing.Projector;
using EventSaucing.Storage;
using Folium.Api.Models.SelfAssessing.Events;
using NEventStore;
using NEventStore.Persistence;
using Scalesque;
using Microsoft.Extensions.Logging;
using EventSaucing.NEventStore;

namespace Folium.Api.Projections.SelfAssessment {
	[Projector(2)]
	public class SelfAssessmentProjector : ProjectorBase {
		readonly ConventionBasedCommitProjecter _conventionProjector;

		public SelfAssessmentProjector(IDbService dbService, IPersistStreams persistStreams, ILogger<SelfAssessmentProjector> logger) :base(persistStreams, dbService) {
			var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit => {
				logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
			})
			   .FirstProject<SkillSelfAssessmentCreated>(CreateSelfAssessment)
			   .ThenProject<SkillSelfAssessmentUpdated>(UpdateSelfAssessment)
			   .ThenProject<SkillSelfAssessmentRemoved>(RemoveSelfAssessment);

			_conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
		}

		public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
		}

		private void CreateSelfAssessment(IDbTransaction tx, ICommit commit, SkillSelfAssessmentCreated @event) {
			// A self assessment has been added. We need to insert it.
			var sqlParameters =
				new {
					@event.SelfAssessment.UserId,
					@event.SelfAssessment.SkillId,
					@event.SkillSetId,
					@event.SelfAssessment.SelfAssessmentLevelId,
					@event.SelfAssessment.Score,
					@event.SelfAssessment.CreatedAt
				};

			const string sql = @"
				INSERT INTO [dbo].[SelfAssessmentProjector.SelfAssessment]
					([UserId]
					,[SkillId]
					,[SkillSetId]
					,[SelfAssessmentLevelId]
					,[Score]
					,[CreatedAt])
				SELECT  
					@UserId
					,@SkillId
					,@SkillSetId
					,@SelfAssessmentLevelId
					,@Score
					,@CreatedAt
				WHERE NOT EXISTS(SELECT * FROM [dbo].[SelfAssessmentProjector.SelfAssessment] WHERE [UserId] = @UserId AND [SkillId] = @SkillId);";
			tx.Connection.Execute(sql, (object)sqlParameters, tx);
		}
		private void UpdateSelfAssessment(IDbTransaction tx, ICommit commit, SkillSelfAssessmentUpdated @event) {
			// A self assessment has been updated. We need to update it.
			var sqlParameters =
				new {
					@event.SelfAssessment.UserId,
					@event.SelfAssessment.SkillId,
					@event.SelfAssessment.SelfAssessmentLevelId,
					@event.SelfAssessment.Score,
					@event.SelfAssessment.CreatedAt
				};

			const string sql = @"
				UPDATE [dbo].[SelfAssessmentProjector.SelfAssessment]
				SET SelfAssessmentLevelId = @SelfAssessmentLevelId,
					Score = @Score,
					CreatedAt = @CreatedAt
				WHERE UserId = @UserId 
					AND SkillId = @SkillId;";
			tx.Connection.Execute(sql, (object)sqlParameters, tx);
		}
		private void RemoveSelfAssessment(IDbTransaction tx, ICommit commit, SkillSelfAssessmentRemoved @event) {
			// A self assessment has been removed.
			// If the removed self assessment has a previous assessment then update the table, otherwise remove it where the dates match.

			if (@event.PreviousSelfAssessment != null) {
				// Update the table.
				var sqlParameters =
					new {
						@event.SelfAssessment.UserId,
						@event.SelfAssessment.SkillId,
						@event.PreviousSelfAssessment.SelfAssessmentLevelId,
						@event.PreviousSelfAssessment.Score,
						@event.PreviousSelfAssessment.CreatedAt
					};
				const string sql = @"
				UPDATE [dbo].[SelfAssessmentProjector.SelfAssessment]
				SET SelfAssessmentLevelId = @SelfAssessmentLevelId,
					Score = @Score,
					CreatedAt = @CreatedAt
				WHERE UserId = @UserId 
					AND SkillId = @SkillId;";
				tx.Connection.Execute(sql, (object) sqlParameters, tx);
			} else {
				// Remove from the table.
				var sqlParameters =
					new {
						@event.SelfAssessment.UserId,
						@event.SelfAssessment.SkillId,
						@event.SelfAssessment.CreatedAt
					};
				const string sql = @"
				DELETE FROM [dbo].[SelfAssessmentProjector.SelfAssessment]
				WHERE UserId = @UserId 
					AND SkillId = @SkillId
					AND CreatedAt = @CreatedAt;";
				tx.Connection.Execute(sql, (object)sqlParameters, tx);
			}
		}
	}
}
