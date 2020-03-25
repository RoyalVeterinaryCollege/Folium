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
using Microsoft.Extensions.Logging;
using Folium.Api.Models.File.Events;

namespace Folium.Api.Projections.File {
	[Projector(7)]
	public class FileProjector : ProjectorBase {
		private readonly ConventionBasedCommitProjecter _conventionProjector;
		private readonly ILogger<FileProjector> _logger;

		public FileProjector(IDbService dbService, IPersistStreams persistStreams, ILogger<FileProjector> logger) : base(persistStreams, dbService) {
			var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit =>
			{
				logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
			})
				.FirstProject<FileCreated>(OnFileCreated)
				.ThenProject<FileRemoved>(OnFileRemoved);

			_conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
			_logger = logger;
		}

		public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
		}

		private void OnFileCreated(IDbTransaction tx, ICommit commit, FileCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();
			_logger.LogDebug($"Creating with CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, Events:{commit.Events.FoldLeft("", (s, m) => $"{s}{m.Body} ")},  file with id {commit.AggregateId()} with the values: CreatedAt:{@event.CreatedAt}, CreatedBy:{@event.CreatedBy}, Filename:{@event.Filename}, FilePath:{@event.FilePath}, Type:{@event.Type}");

			const string sql = @"
				INSERT INTO [dbo].[FileProjector.File]
					   ([Id]
					   ,[CreatedBy]
					   ,[CreatedAt]
					   ,[FileName]
					   ,[FilePath]
                       ,[Type]
					   ,[Size])
				 SELECT
					   @Id
					   ,@CreatedBy
					   ,@CreatedAt
					   ,@FileName
					   ,@FilePath
                       ,@Type
					   ,@Size
				WHERE NOT EXISTS(SELECT * FROM [dbo].[FileProjector.File] WHERE Id = @Id);";
			tx.Connection.Execute(sql, (object) sqlParams, tx);
		}

		private void OnFileRemoved(IDbTransaction tx, ICommit commit, FileRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			const string sql = @"
				DELETE FROM [dbo].[FileProjector.File]
				WHERE Id = @Id;";
			tx.Connection.Execute(sql, (object) sqlParams, tx);
        }
	}
}