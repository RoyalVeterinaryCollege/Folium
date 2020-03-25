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
using EventSaucing.Projector;
using EventSaucing.Storage;
using EventSaucing.NEventStore;
using Folium.Api.Extensions;
using NEventStore;
using NEventStore.Persistence;
using Scalesque;
using Dapper;
using Folium.Api.Models.Entry.Events;
using Folium.Api.Models.Placement.Events;
using Folium.Api.Models.SelfAssessing.Events;
using Microsoft.Extensions.Logging;
using System;
using Folium.Api.Models.File.Events;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Folium.Api.Projections.Entry {
	[Projector(1)]
	public class EntryProjector: ProjectorBase {
		readonly ConventionBasedCommitProjecter _conventionProjector;

		public EntryProjector(IDbService dbService, IPersistStreams persistStreams, ILogger<EntryProjector> logger) :base(persistStreams, dbService) {
			var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit => {
				logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
			})
			   .FirstProject<EntryCreated>(OnEntryCreated)
			   .ThenProject<EntryCreatedWithType>(OnEntryCreatedWithType)
			   .ThenProject<EntryUpdated>(OnEntryUpdated)
			   .ThenProject<EntrySkillGroupingChanged>(OnEntrySkillGroupingChanged)
			   .ThenProject<EntryRemoved>(OnEntryRemoved)
			   .ThenProject<EntrySelfAssessmentCreated>(OnEntrySelfAssessmentCreated)
			   .ThenProject<EntrySelfAssessmentAdded>(OnEntrySelfAssessmentAdded)
			   .ThenProject<EntrySelfAssessmentUpdated>(OnEntrySelfAssessmentUpdated)
			   .ThenProject<EntrySelfAssessmentRemoved>(OnEntrySelfAssessmentRemoved)
			   .ThenProject<PlacementNameUpdated>(OnPlacementNameUpdated)
			   .ThenProject<EntryShared>(OnEntryShared)
			   .ThenProject<EntryCollaboratorRemoved>(OnEntryCollaboratorRemoved)
			   .ThenProject<EntrySignOffRequested>(OnEntrySignOffRequested)
			   .ThenProject<EntrySignOffUserRemoved>(OnEntrySignOffUserRemoved)
			   .ThenProject<EntrySignedOff>(OnEntrySignedOff)
			   .ThenProject<EntryCommentCreated>(OnEntryCommentCreated)
			   .ThenProject<EntryCommentCreatedWithFiles>(OnEntryCommentCreatedWithFiles)
			   .ThenProject<EntryFileCreated>(OnFileCreated)
			   .ThenProject<EntryFileRemoved>(OnFileRemoved)
			   .ThenProject<EntryAudioVideoFileEncoded>(OnAudioVideoFileEncoded);

			_conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
		}

        public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
		}

		private void OnEntryCreated(IDbTransaction tx, ICommit commit, EntryCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				INSERT INTO [dbo].[EntryProjector.Entry]
					   ([Id]
					   ,[SkillSetId]
					   ,[Title]
					   ,[Description]
					   ,[UserId]
					   ,[Where]
					   ,[When]
					   ,[CreatedAt]
					   ,[LastUpdatedAt]
					   ,[Shared]
                       ,[SkillGroupingId])
				 SELECT
					   @Id
					   ,@SkillSetId
					   ,@Title
					   ,@Description
					   ,@UserId
					   ,@Where
					   ,@When
					   ,@CreatedAt
					   ,@LastUpdatedAt
					   ,0 -- Not shared
                       ,@SkillGroupingId
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.Entry] WHERE Id = @Id);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntryCreatedWithType(IDbTransaction tx, ICommit commit, EntryCreatedWithType @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
			UPDATE [EntryProjector.Entry]
			SET	[TypeId] = [EntryType].[Id],
				[TypeName] = [EntryType].[Name],
				[IsSignOffCompatible] = [EntryType].[IsSignOffCompatible]
			FROM (
				SELECT [Id], [Name], CASE WHEN Template LIKE '%""signOff"":%' THEN 1 ELSE 0 END AS [IsSignOffCompatible]
				FROM [dbo].[EntryType]) AS EntryType
			WHERE
				[EntryType].[Id] = @TypeId
				AND [EntryProjector.Entry].[Id] = @Id";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntryUpdated(IDbTransaction tx, ICommit commit, EntryUpdated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				UPDATE [dbo].[EntryProjector.Entry]
				SET [Title] = @Title
					,[Description] = @Description
					,[Where] = @Where
					,[When] = @When
					,[LastUpdatedAt] = @LastUpdatedAt
				WHERE Id = @Id;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
        }
        private void OnEntrySkillGroupingChanged(IDbTransaction tx, ICommit commit, EntrySkillGroupingChanged @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            const string sql = @"
				UPDATE [dbo].[EntryProjector.Entry]
				SET [SkillGroupingId] = @SkillGroupingId
				WHERE Id = @Id;";
            tx.Connection.Execute(sql, (object)sqlParams, tx);
        }
        private void OnEntryRemoved(IDbTransaction tx, ICommit commit, EntryRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				DELETE FROM [dbo].[EntryProjector.Entry]
				WHERE Id = @Id;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntrySelfAssessmentCreated(IDbTransaction tx, ICommit commit, EntrySelfAssessmentCreated @event) {
			InsertEntrySelfAssessment(tx, @event.SelfAssessment);
		}
		private void OnEntrySelfAssessmentAdded(IDbTransaction tx, ICommit commit, EntrySelfAssessmentAdded @event) {
			InsertEntrySelfAssessment(tx, @event.SelfAssessment);
		}
		private void InsertEntrySelfAssessment(IDbTransaction tx, Models.SelfAssessment selfAssessment) {
			var sqlParams =
				new {
					selfAssessment.EntryId,
					selfAssessment.SkillId,
					selfAssessment.SelfAssessmentLevelId,
					selfAssessment.Score
				};

			const string sql = @"
				INSERT INTO [dbo].[EntryProjector.SelfAssessment]
					   ([EntryId]
					   ,[SkillId]
					   ,[SelfAssessmentLevelId]
					   ,[Score])
				 SELECT
					   @EntryId
					   ,@SkillId
					   ,@SelfAssessmentLevelId
					   ,@Score
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.SelfAssessment] WHERE EntryId = @EntryId AND SkillId = @SkillId);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntrySelfAssessmentUpdated(IDbTransaction tx, ICommit commit, EntrySelfAssessmentUpdated @event) {
			var sqlParams =
				new {
					@event.SelfAssessment.EntryId,
					@event.SelfAssessment.SkillId,
					@event.SelfAssessment.SelfAssessmentLevelId,
					@event.SelfAssessment.Score
				};

			const string sql = @"
				UPDATE [dbo].[EntryProjector.SelfAssessment]
				SET [SelfAssessmentLevelId] = @SelfAssessmentLevelId
					,[Score] = @Score
				WHERE [EntryId] = @EntryId
					  AND [SkillId] = @SkillId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntrySelfAssessmentRemoved(IDbTransaction tx, ICommit commit, EntrySelfAssessmentRemoved @event) {
			var sqlParams =
				new {
					@event.SelfAssessment.EntryId,
					@event.SelfAssessment.SkillId
				};

			const string sql = @"
				DELETE FROM [dbo].[EntryProjector.SelfAssessment]
				WHERE [EntryId] = @EntryId
					  AND [SkillId] = @SkillId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnPlacementNameUpdated(IDbTransaction tx, ICommit commit, PlacementNameUpdated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				UPDATE [EntryProjector.Entry]
				SET [Where] = @FullyQualifiedTitle
				WHERE [UserId] = @UserId
				AND [Where] = @OriginalFullyQualifiedTitle;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntryCommentCreated(IDbTransaction tx, ICommit commit, EntryCommentCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.EntryId = commit.AggregateId();

			const string sql = @"
				INSERT INTO [dbo].[EntryProjector.EntryComment]
					   ([Id]
					  ,[EntryId]
					  ,[Comment]
					  ,[CreatedBy]
					  ,[CreatedAt])
				 SELECT
					   @Id
					  ,@EntryId
					  ,@Comment
					  ,@CreatedBy
					  ,@CreatedAt
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.EntryComment] WHERE [EntryId] = @EntryId AND [Id] = @Id);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntryCollaboratorRemoved(IDbTransaction tx, ICommit commit, EntryCollaboratorRemoved @event) {
			var sqlParams = new {
				EntryId = commit.AggregateId(),
				@event.UserId
			};

			const string sql = @"
				DELETE FROM [dbo].[EntryProjector.SharedWith]
				WHERE [EntryId] = @EntryId
					  AND [UserId] = @UserId;

				UPDATE [EntryProjector.Entry]
				SET	[Shared] = CASE WHEN EXISTS (SELECT 1 FROM [dbo].[EntryProjector.SharedWith] WHERE [EntryId] = @EntryId) THEN 1 ELSE 0 END
				WHERE [Id] = @EntryId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntryShared(IDbTransaction tx, ICommit commit, EntryShared @event) {
			foreach (var collaboratorId in @event.CollaboratorIds) {
				var sqlParams = new {
					EntryId = commit.AggregateId(),
					UserId = collaboratorId
				};
				const string sql = @"
				INSERT INTO [dbo].[EntryProjector.SharedWith]
					   ([EntryId]
					   ,[UserId])
				 SELECT
					   @EntryId
					   ,@UserId
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.SharedWith] WHERE EntryId = @EntryId AND UserId = @UserId);
				
				UPDATE [EntryProjector.Entry]
				SET	[Shared] = 1
				WHERE [Id] = @EntryId;";
				tx.Connection.Execute(sql, (object)sqlParams, tx);
			}
		}

		private void OnEntrySignOffUserRemoved(IDbTransaction tx, ICommit commit, EntrySignOffUserRemoved @event) {
			var sqlParams = new {
				EntryId = commit.AggregateId(),
				UserId = @event.RemoveUserId
			};

			const string sql = @"
				DELETE FROM [dbo].[EntryProjector.SignOffRequest]
				WHERE [EntryId] = @EntryId
					  AND [UserId] = @UserId;

				UPDATE [EntryProjector.Entry]
				SET	[SignOffRequested] = CASE WHEN EXISTS (SELECT 1 FROM [dbo].[EntryProjector.SignOffRequest] WHERE [EntryId] = @EntryId) THEN 1 ELSE 0 END
				WHERE [Id] = @EntryId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntrySignOffRequested(IDbTransaction tx, ICommit commit, EntrySignOffRequested @event) {
			foreach (var authorisedUserIds in @event.AuthorisedUserIds) {
				var sqlParams = new {
					EntryId = commit.AggregateId(),
					UserId = authorisedUserIds,
					@event.When
				};
				const string sql = @"
				INSERT INTO [dbo].[EntryProjector.SignOffRequest]
					   ([EntryId]
					   ,[UserId]
					   ,[When])
				 SELECT
					   @EntryId
					   ,@UserId
					   ,@When
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.SignOffRequest] WHERE EntryId = @EntryId AND UserId = @UserId);
				
				UPDATE [EntryProjector.Entry]
				SET	[SignOffRequested] = 1
				WHERE [Id] = @EntryId;";
				tx.Connection.Execute(sql, (object)sqlParams, tx);
			}
		}

		private void OnEntrySignedOff(IDbTransaction tx, ICommit commit, EntrySignedOff @event) {
			var sqlParams = new {
				EntryId = commit.AggregateId(),
				UserId = @event.AuthorisedUserId,
				@event.When,
				@event.CommentId
			};
			const string sql = @"
			UPDATE [EntryProjector.Entry]
			SET	[SignedOffAt] = @When,
				[SignedOffBy] = @UserId,
				[SignedOff] = 1
			WHERE [Id] = @EntryId;

			UPDATE [EntryProjector.EntryComment]
			SET	[ForSignOff] = 1
			WHERE [Id] = @CommentId
			AND [EntryId] = @EntryId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntryCommentCreatedWithFiles(IDbTransaction tx, ICommit commit, EntryCommentCreatedWithFiles @event) {
			foreach (var fileId in @event.FileIds) {
				var sqlParams = new {
					@event.CommentId,
					EntryId = commit.AggregateId(),
					FileId = fileId
				};
				const string sql = @"
				INSERT INTO [dbo].[EntryProjector.EntryCommentFile]
					   ([CommentId]
					   ,[EntryId]
					   ,[FileId])
				 SELECT
					   @CommentId
					   ,@EntryId
					   ,@FileId
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.EntryCommentFile] WHERE CommentId = @CommentId AND EntryId = @EntryId AND FileId = @FileId);";
				tx.Connection.Execute(sql, (object)sqlParams, tx);
			}
		}

		private void OnFileCreated(IDbTransaction tx, ICommit commit, EntryFileCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.EntryId = commit.AggregateId();

			const string sql = @"
				INSERT INTO [dbo].[EntryProjector.EntryFile]
					   ([EntryId]
					   ,[FileId]
					   ,[OnComment]
					   ,[CreatedBy]
					   ,[CreatedAt]
					   ,[FileName]
					   ,[FilePath]
                       ,[Type]
					   ,[Size]
				       ,[IsAudioVideoEncoded])
				 SELECT
					   @EntryId
					   ,@FileId
					   ,@OnComment
					   ,@CreatedBy
					   ,@CreatedAt
					   ,@FileName
					   ,@FilePath
                       ,@Type
					   ,@Size
					   ,0
				WHERE NOT EXISTS(SELECT * FROM [dbo].[EntryProjector.EntryFile] WHERE EntryId = @EntryId AND FileId = @FileId);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnFileRemoved(IDbTransaction tx, ICommit commit, EntryFileRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.EntryId = commit.AggregateId();

			const string sql = @"
				DELETE FROM [dbo].[EntryProjector.EntryFile]
				WHERE EntryId = @EntryId AND FileId = @FileId;

				DELETE FROM [dbo].[EntryProjector.EntryCommentFile]
				WHERE EntryId = @EntryId AND FileId = @FileId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnAudioVideoFileEncoded(IDbTransaction tx, ICommit commit, EntryAudioVideoFileEncoded @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.EntryId = commit.AggregateId();

			const string sql = @"
				UPDATE [dbo].[EntryProjector.EntryFile]
				SET [IsAudioVideoEncoded] = 1,
					[EncodedAudioVideoDirectoryPath] = @EncodedAudioVideoDirectoryPath
				WHERE EntryId = @EntryId AND FileId = @FileId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
	}
}
