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
using System.Linq;
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
using Microsoft.Extensions.Logging;

namespace Folium.Api.Projections.Placement {
	[Projector(4)]
	public class PlacementProjector : ProjectorBase {
		private readonly ConventionBasedCommitProjecter _conventionProjector;
		private readonly ILogger<PlacementProjector> _logger;

		public PlacementProjector(IDbService dbService, IPersistStreams persistStreams, ILogger<PlacementProjector> logger) : base(persistStreams, dbService) {
			var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit => {
				logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
			})
				.FirstProject<PlacementCreated>(OnPlacementCreated)
				.ThenProject<PlacementUpdated>(OnPlacementUpdated)
				.ThenProject<PlacementRemoved>(OnPlacementRemoved)
			    .ThenProject<EntryCreated>(OnEntryCreated)
			    .ThenProject<EntryCreatedWithType>(OnEntryCreatedWithType)
				.ThenProject<EntryUpdated>(OnEntryUpdated)
			    .ThenProject<EntryRemoved>(OnEntryRemoved)
			    .ThenProject<EntryShared>(OnEntryShared)
			    .ThenProject<EntryCollaboratorRemoved>(OnEntryCollaboratorRemoved)
				.ThenProject<EntrySignOffRequested>(OnEntrySignOffRequested)
				.ThenProject<EntrySignOffUserRemoved>(OnEntrySignOffUserRemoved)
				.ThenProject<EntrySignedOff>(OnEntrySignedOff);

			_conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
			_logger = logger;
		}

		public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
		}

		private void OnPlacementCreated(IDbTransaction tx, ICommit commit, PlacementCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();
			_logger.LogDebug($"Creating with CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, Events:{commit.Events.FoldLeft("", (s, m) => $"{s}{m.Body} ")},  placement with id {commit.AggregateId()} with the values: Title:{@event.Title}, FullyQualifiedTitle:{@event.FullyQualifiedTitle}, Start:{@event.Start}, End:{@event.End}, Reference:{@event.Reference}, LastUpdatedBy:{@event.LastUpdatedBy}, LastUpdatedAt:{@event.LastUpdatedAt}, UserId:{@event.UserId}, CreatedAt:{@event.CreatedAt}, CreatedBy:{@event.CreatedBy}");

			const string sql = @"
				INSERT INTO [dbo].[PlacementProjector.Placement]
					   ([Id]
					   ,[UserId]
					   ,[Title]
					   ,[FullyQualifiedTitle]
					   ,[Start]
					   ,[End]
					   ,[Reference]
					   ,[CreatedBy]
					   ,[CreatedAt]
					   ,[LastUpdatedBy]
					   ,[LastUpdatedAt]
                       ,[Type])
				 SELECT
					   @Id
					   ,@UserId
					   ,@Title
					   ,@FullyQualifiedTitle
					   ,@Start
					   ,@End
					   ,@Reference
					   ,@CreatedBy
					   ,@CreatedAt
					   ,@CreatedBy
					   ,@CreatedAt
                       ,@Type
				WHERE NOT EXISTS(SELECT * FROM [dbo].[PlacementProjector.Placement] WHERE Id = @Id);";
			tx.Connection.Execute(sql, (object) sqlParams, tx);
		}

		private void OnPlacementUpdated(IDbTransaction tx, ICommit commit, PlacementUpdated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();
			
			const string sql = @"
				UPDATE [dbo].[PlacementProjector.Placement]
				SET [Title] = @Title
					,[FullyQualifiedTitle] = @FullyQualifiedTitle
					,[Start] = @Start
					,[End] = @End
					,[Reference] = @Reference
					,[LastUpdatedBy] = @LastUpdatedBy
					,[LastUpdatedAt] = @LastUpdatedAt
                    ,[Type] = @type
				WHERE Id = @Id;

				UPDATE [PlacementProjector.Entry]
				SET [Where] = @FullyQualifiedTitle
				WHERE [PlacementProjector.Entry].PlacementId = @Id;";
			tx.Connection.Execute(sql, (object) sqlParams, tx);
		}

		private void OnPlacementRemoved(IDbTransaction tx, ICommit commit, PlacementRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql1 = @"
				DELETE FROM [dbo].[PlacementProjector.Placement]
				WHERE Id = @Id;";
			tx.Connection.Execute(sql1, (object) sqlParams, tx);

            const string sql2 = @"
				UPDATE [dbo].[PlacementProjector.Entry]
                SET [PlacementId] = NULL
				WHERE [PlacementId] = @Id;";
            tx.Connection.Execute(sql2, (object)sqlParams, tx);
        }
		private void OnEntryCreated(IDbTransaction tx, ICommit commit, EntryCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				DECLARE @PlacementId uniqueidentifier;  
				SELECT TOP 1 @PlacementId = [PlacementProjector_Placement].[Id]
				FROM [dbo].[PlacementProjector.Placement] AS [PlacementProjector_Placement]
				WHERE [FullyQualifiedTitle] = @Where
				AND [UserId] = @UserId

				INSERT INTO [dbo].[PlacementProjector.Entry]
					   ([Id]
					   ,[PlacementId]
					   ,[SkillSetId]
					   ,[Title]
					   ,[Description]
					   ,[UserId]
					   ,[Where]
					   ,[When]
					   ,[Shared])
				SELECT TOP 1
					   @Id
					   ,@PlacementId
					   ,@SkillSetId
					   ,@Title
					   ,@Description
					   ,@UserId
					   ,@Where
					   ,@When
					   ,0 -- Not shared
				WHERE NOT EXISTS(SELECT * FROM [dbo].[PlacementProjector.Entry] WHERE Id = @Id);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntryCreatedWithType(IDbTransaction tx, ICommit commit, EntryCreatedWithType @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
			UPDATE [PlacementProjector.Entry]
			SET	[TypeId] = [EntryType].[Id],
				[TypeName] = [EntryType].[Name],
				[IsSignOffCompatible] = [EntryType].[IsSignOffCompatible]
			FROM (
				SELECT [Id], [Name], CASE WHEN Template LIKE '%""signOff"":%' THEN 1 ELSE 0 END AS [IsSignOffCompatible]
				FROM [dbo].[EntryType]) AS EntryType
			WHERE
				[EntryType].[Id] = @TypeId
				AND [PlacementProjector.Entry].[Id] = @Id";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntryUpdated(IDbTransaction tx, ICommit commit, EntryUpdated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"

				DECLARE @UserId int;  
				SELECT @UserId = [PlacementProjector_Entry].[UserId]
				FROM [dbo].[PlacementProjector.Entry] AS [PlacementProjector_Entry]
				WHERE Id = @Id;

				DECLARE @PlacementId uniqueidentifier;  
				SELECT TOP 1 @PlacementId = [PlacementProjector_Placement].[Id]
				FROM [dbo].[PlacementProjector.Placement] AS [PlacementProjector_Placement]
				WHERE [FullyQualifiedTitle] = @Where
				AND [UserId] = @UserId

				UPDATE [PlacementProjector_Entry]
				SET [Title] = @Title
					,[Description] = @Description
					,[Where] = @Where
					,[When] = @When
					,[PlacementId] = @PlacementId
				FROM [dbo].[PlacementProjector.Entry] AS [PlacementProjector_Entry] 
				WHERE [PlacementProjector_Entry].Id = @Id;

				DELETE FROM [dbo].[PlacementProjector.Entry]
				WHERE [Id] = @Id
				AND [Where] != @Where;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntryRemoved(IDbTransaction tx, ICommit commit, EntryRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				DELETE FROM [dbo].[PlacementProjector.Entry]
				WHERE [Id] = @Id;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntryCollaboratorRemoved(IDbTransaction tx, ICommit commit, EntryCollaboratorRemoved @event) {
			var sqlParams = new {
				EntryId = commit.AggregateId(),
				@event.UserId
			};

			const string sql = @"
				DELETE FROM [dbo].[PlacementProjector.EntrySharedWith]
				WHERE [EntryId] = @EntryId
					  AND [UserId] = @UserId;

				UPDATE [PlacementProjector.Entry]
				SET	[Shared] = CASE WHEN EXISTS (SELECT 1 FROM [dbo].[PlacementProjector.EntrySharedWith] WHERE [EntryId] = @EntryId) THEN 1 ELSE 0 END
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
				INSERT INTO [dbo].[PlacementProjector.EntrySharedWith]
					   ([EntryId]
					   ,[UserId])
				 SELECT
					   @EntryId
					   ,@UserId
				WHERE NOT EXISTS(SELECT * FROM [dbo].[PlacementProjector.EntrySharedWith] WHERE EntryId = @EntryId AND UserId = @UserId);
				
				UPDATE [PlacementProjector.Entry]
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
				UPDATE [PlacementProjector.Entry]
				SET	[SignOffRequested] = CASE WHEN EXISTS (SELECT 1 FROM [dbo].[EntryProjector.SignOffRequest] WHERE [EntryId] = @EntryId) THEN 1 ELSE 0 END
				WHERE [Id] = @EntryId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}

		private void OnEntrySignOffRequested(IDbTransaction tx, ICommit commit, EntrySignOffRequested @event) {
			foreach (var authorisedUserIds in @event.AuthorisedUserIds) {
				var sqlParams = new {
					EntryId = commit.AggregateId()
				};
				const string sql = @"
				UPDATE [PlacementProjector.Entry]
				SET	[SignOffRequested] = 1
				WHERE [Id] = @EntryId;";
				tx.Connection.Execute(sql, (object)sqlParams, tx);
			}
		}

		private void OnEntrySignedOff(IDbTransaction tx, ICommit commit, EntrySignedOff @event) {
			var sqlParams = new {
				EntryId = commit.AggregateId(),
				UserId = @event.AuthorisedUserId,
				@event.When
			};
			const string sql = @"
			UPDATE [PlacementProjector.Entry]
			SET	[SignedOffAt] = @When,
				[SignedOffBy] = @UserId,
				[SignedOff] = 1
			WHERE [Id] = @EntryId;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
	}
}