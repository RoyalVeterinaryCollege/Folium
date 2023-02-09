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
using System.Data;
using Dapper;
using EventSaucing.NEventStore;
using EventSaucing.Projector;
using Folium.Api.Extensions;
using Folium.Api.Models.Entry.Events;
using Folium.Api.Models.Placement.Events;
using NEventStore;
using NEventStore.Persistence;
using Scalesque;
using IDbService = EventSaucing.Storage.IDbService;
using Microsoft.Extensions.Logging;

namespace Folium.Api.Projections.Where {
	[Projector(3)]
	public class WhereProjector: ProjectorBase {
		readonly ConventionBasedCommitProjecter _conventionProjector;
		readonly ILogger<WhereProjector> _logger;

		public WhereProjector(IDbService dbService, IPersistStreams persistStreams, ILogger<WhereProjector> logger) :base(persistStreams, dbService) {
			var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit => {
				logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
			})
			    .FirstProject<EntryCreated>(OnEntryCreated)
			    .ThenProject<EntryUpdated>(OnEntryUpdated)
				.ThenProject<EntryRemoved>(OnEntryRemoved)
				.ThenProject<PlacementCreated>(OnPlacementCreated)
				.ThenProject<PlacementUpdated>(OnPlacementUpdated)
				.ThenProject<PlacementRemoved>(OnPlacementRemoved);

			_conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
			_logger = logger;
		}
		public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
		}
		private void OnEntryCreated(IDbTransaction tx, ICommit commit, EntryCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				INSERT INTO [dbo].[WhereProjector.Entry]
					   ([EntryId]
					   ,[UserId]
					   ,[Where])
				 SELECT
					   @Id
					   ,@UserId
					   ,@Where
				WHERE NOT EXISTS(SELECT * FROM [dbo].[WhereProjector.Entry] WHERE [EntryId] = @Id);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);

			CreateWhereProjection(tx, @event.UserId, @event.Where);
		}
		private void OnEntryUpdated(IDbTransaction tx, ICommit commit, EntryUpdated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			// Get the original where entry.
			const string sql1 = @"
				SELECT [Where]
					  ,[UserId]
				FROM [dbo].[WhereProjector.Entry]
				WHERE [EntryId] = @Id;";
			var row = tx.Connection.QuerySingleOrDefault<dynamic>(sql1, (object)sqlParams, tx);

            if (row == null) {
                // The entry has been removed, commit out of sequence??
                // Report and do nothing.
                _logger.LogWarning($"Received an event on Entry {sqlParams.Id} which does not exist in the projector table.");
                return;
            }

			var originalWhere = row.Where;
			var userId = row.UserId;

			const string sql2 = @"
				UPDATE [dbo].[WhereProjector.Entry]
				SET [Where] = @Where
				WHERE [EntryId] = @Id;";
			tx.Connection.Execute(sql2, (object)sqlParams, tx);

			sqlParams = new {
				UserId = userId,
				Where = @event.Where,
				OriginalWhere = originalWhere
			};

			const string sql = @"
				UPDATE [dbo].[WhereProjector.Where]
				SET [UsageCount] = [UsageCount] - 1
				WHERE [UserId] = @UserId AND [Name] = @OriginalWhere;

				DELETE FROM [dbo].[WhereProjector.Where]
				WHERE [UserId] = @UserId 
				AND [Name] = @OriginalWhere
				AND [UsageCount] = 0;

				UPDATE [dbo].[WhereProjector.Where]
				SET [UsageCount] = [UsageCount] + 1
				WHERE [UserId] = @UserId AND [Name] = @Where;

				INSERT INTO [dbo].[WhereProjector.Where]
					   ([UserId]
					   ,[Name]
					   ,[UsageCount])
				 SELECT
					   @UserId
					   ,@Where
					   ,1
				WHERE NOT EXISTS(SELECT * FROM [dbo].[WhereProjector.Where] WHERE [UserId] = @UserId AND [Name] = @Where);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		private void OnEntryRemoved(IDbTransaction tx, ICommit commit, EntryRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();
			
			// Get the original where entry.
			const string sql1 = @"
				SELECT [Where]
					  ,[UserId]
				FROM [dbo].[WhereProjector.Entry]
				WHERE [EntryId] = @Id;";
			var row = tx.Connection.QuerySingle<dynamic>(sql1, (object)sqlParams, tx);
			var where = row.Where;
			var userId = row.UserId;

			const string sql2 = @"
				DELETE FROM [dbo].[WhereProjector.Entry]
				WHERE [EntryId] = @Id;";
			tx.Connection.Execute(sql2, (object)sqlParams, tx);

			DeleteWhereProjection(tx, userId, where);
		}
		private void OnPlacementCreated(IDbTransaction tx, ICommit commit, PlacementCreated @event) {
			var sqlParams = new {
				Id = commit.AggregateId(),
				@event.UserId,
				Where = @event.FullyQualifiedTitle
			};

			const string sql = @"
				INSERT INTO [dbo].[WhereProjector.Placement]
					   ([PlacementId]
					   ,[UserId]
					   ,[Where])
				 SELECT
					   @Id
					   ,@UserId
					   ,@Where
				WHERE NOT EXISTS(SELECT * FROM [dbo].[WhereProjector.Placement] WHERE [PlacementId] = @Id);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);

			CreateWhereProjection(tx, @event.UserId, @event.FullyQualifiedTitle);
		}
		private void OnPlacementUpdated(IDbTransaction tx, ICommit commit, PlacementUpdated @event) {
			var placementSqlParams = new {
				Id = commit.AggregateId(),
				Where = @event.FullyQualifiedTitle
			};

			// Get the original where Placement.
			const string sql1 = @"
				SELECT [Where]
				FROM [dbo].[WhereProjector.Placement]
				WHERE [PlacementId] = @Id;";
			var row = tx.Connection.QuerySingle<dynamic>(sql1, (object)placementSqlParams, tx);
			var originalWhere = row.Where;

			const string sql2 = @"
				UPDATE [dbo].[WhereProjector.Placement]
				SET [Where] = @Where
				WHERE [PlacementId] = @Id;";
			tx.Connection.Execute(sql2, (object)placementSqlParams, tx);
			
			var whereSqlParams = new {
				UserId = @event.UserId,
				Where = @event.FullyQualifiedTitle,
				OriginalWhere = originalWhere
			};

			const string sql = @"
				UPDATE [dbo].[WhereProjector.Entry]
				SET [Where] = @Where
				WHERE [UserId] = @UserId
                AND [Where] = @OriginalWhere;

				DECLARE @where_count int;  
				SELECT @where_count = [UsageCount]
				FROM [dbo].[WhereProjector.Where]
				WHERE [UserId] = @UserId 
				AND [Name] = @OriginalWhere;

				DELETE FROM [dbo].[WhereProjector.Where]
				WHERE [UserId] = @UserId 
				AND [Name] = @OriginalWhere;

				UPDATE [dbo].[WhereProjector.Where]
				SET [UsageCount] = [UsageCount] + ISNULL(@where_count, 0)
				WHERE [UserId] = @UserId 
				AND [Name] = @Where;

				INSERT INTO [dbo].[WhereProjector.Where]
					   ([UserId]
					   ,[Name]
					   ,[UsageCount])
				 SELECT
					   @UserId
					   ,@Where
					   ,ISNULL(@where_count, 0)
				WHERE NOT EXISTS(SELECT * FROM [dbo].[WhereProjector.Where] WHERE [UserId] = @UserId AND [Name] = @Where);";
			tx.Connection.Execute(sql, (object)whereSqlParams, tx);
		}
		private void OnPlacementRemoved(IDbTransaction tx, ICommit commit, PlacementRemoved @event) {
			var sqlParams = new {
				Id = commit.AggregateId()
			};

			const string sql = @"
				DELETE FROM [dbo].[WhereProjector.Placement]
				WHERE [PlacementId] = @Id;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);

			DeleteWhereProjection(tx, @event.UserId, @event.FullyQualifiedTitle);
		}
		private void CreateWhereProjection(IDbTransaction tx, int userId, string where) {
			var sqlParams = new {
				UserId = userId,
				Where = where
			};

			const string sql = @"
				UPDATE [dbo].[WhereProjector.Where]
				SET [UsageCount] = [UsageCount] + 1
				WHERE [UserId] = @UserId AND [Name] = @Where;

				INSERT INTO [dbo].[WhereProjector.Where]
					   ([UserId]
					   ,[Name]
					   ,[UsageCount])
				 SELECT
					   @UserId
					   ,@Where
					   ,1
				WHERE NOT EXISTS(SELECT * FROM [dbo].[WhereProjector.Where] WHERE [UserId] = @UserId AND [Name] = @Where);";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
		
		private void DeleteWhereProjection(IDbTransaction tx, int userId, string where) {
			var sqlParams = new {
				UserId = userId,
				Where = where
			};

			const string sql = @"
				UPDATE [dbo].[WhereProjector.Where]
				SET [UsageCount] = [UsageCount] - 1
				WHERE [UserId] = @UserId AND [Name] = @Where;

				DELETE FROM [dbo].[WhereProjector.Where]
				WHERE [UserId] = @UserId 
				AND [Name] = @Where
				AND [UsageCount] = 0;";
			tx.Connection.Execute(sql, (object)sqlParams, tx);
		}
	}
}
