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
using Folium.Api.Models.SelfAssessing.Events;
using NEventStore;
using NEventStore.Persistence;
using Scalesque;
using IDbService = EventSaucing.Storage.IDbService;
using Folium.Api.Services;
using Hangfire;
using Folium.Api.Dtos;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Folium.Api.Models;
using System.Collections.Generic;

namespace Folium.Api.Projections.Reporting {
	[Projector(6)]
	public class ReportingProjector : ProjectorBase {
		private readonly ConventionBasedCommitProjecter _conventionProjector;
        private readonly IEntryService _entryService;
        private readonly IUserService _userService;
        private readonly ITutorGroupService _tutorGroupService;
        private readonly IOptions<Configuration> _applicationConfiguration;

        public ReportingProjector(
            IDbService dbService, 
            IPersistStreams persistStreams,
            IEntryService entryService,
            IUserService userService,
            ITutorGroupService tutorGroupService,
            ILogger<ReportingProjector> logger,
            IOptions<Configuration> applicationConfiguration) : base(persistStreams, dbService) {
            var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit => {
                logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
            })
                .FirstProject<EntryCreated>(OnEntryCreated)
                .ThenProject<EntryCreatedWithType>(OnEntryCreatedWithType)
                .ThenProject<EntryUpdated>(OnEntryUpdated)
                .ThenProject<EntryRemoved>(OnEntryRemoved)
                .ThenProject<PlacementCreated>(OnPlacementCreated)
                .ThenProject<PlacementUpdated>(OnPlacementUpdated)
                .ThenProject<PlacementRemoved>(OnPlacementRemoved)
                .ThenProject<SkillSelfAssessmentCreated>(OnSelfAssessmentCreated)
                .ThenProject<SkillSelfAssessmentUpdated>(OnSelfAssessmentUpdated)
                .ThenProject<SkillSelfAssessmentRemoved>(OnSelfAssessmentRemoved)
                .ThenProject<EntryShared>(OnEntryShared)
                .ThenProject<EntryCollaboratorRemoved>(OnEntryCollaboratorRemoved)
                .ThenProject<EntryCommentCreated>(OnEntryCommentCreated);

            _conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
            _entryService = entryService;
            _userService = userService;
            _tutorGroupService = tutorGroupService;
            _applicationConfiguration = applicationConfiguration;
        }

		public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
        }

        private void OnEntryCreated(IDbTransaction tx, ICommit commit, EntryCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

            tx.Connection.Execute(@"
                INSERT INTO [dbo].[ReportingProjector.EntryEngagement]
					   ([UserId]
                       ,[EntryId]
                       ,[Where]
					   ,[EntryTypeId]
					   ,[When]
					   ,[SharedCount]
					   ,[SharedWithTutorCount]
					   ,[CommentCount])
				 SELECT
					   @UserId
					   ,@Id
                       ,@Where
					   ,NULL
                       ,@CreatedAt
                       ,0
                       ,0
                       ,0
				WHERE NOT EXISTS(SELECT * FROM [dbo].[ReportingProjector.EntryEngagement] WHERE [EntryId] = @Id);",
                (object)sqlParams, tx);

            // Update the entry count if this entry is related to a placement.
            tx.Connection.Execute(@"
                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				SET [EntryCount] = [EntryCount] + 1
				WHERE [FullyQualifiedTitle] = @Where 
                AND [UserId] = @UserId;",
                (object)sqlParams, tx);
        }

        private void OnEntryCreatedWithType(IDbTransaction tx, ICommit commit, EntryCreatedWithType @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            tx.Connection.Execute(@"
                UPDATE [dbo].[ReportingProjector.EntryEngagement]
		        SET [EntryTypeId] = @TypeId
				WHERE [EntryId] = @Id;",
                (object)sqlParams, tx);
        }

        private void OnEntryUpdated(IDbTransaction tx, ICommit commit, EntryUpdated @event) {            
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            // Get the userid and old 'where' value.
            var result = tx.Connection.QueryFirstOrDefault(@"
                SELECT [UserId], [Where]
                FROM [dbo].[ReportingProjector.EntryEngagement]
				WHERE [EntryId] = @Id;",
                (object)sqlParams, tx);

            sqlParams.UserId = result.UserId;
            sqlParams.OldWhere = result.Where;

            // Update the where and the entry count on the placement report.
            tx.Connection.Execute(@"
                UPDATE [dbo].[ReportingProjector.EntryEngagement]
                SET [Where] = @Where
				WHERE [EntryId] = @Id;

                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				SET [EntryCount] = [EntryCount] - 1
				WHERE [FullyQualifiedTitle] = @OldWhere 
                AND [UserId] = @UserId;
                
                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				SET [EntryCount] = [EntryCount] + 1
				WHERE [FullyQualifiedTitle] = @Where 
                AND [UserId] = @UserId;",
                (object)sqlParams, tx);
        }

        private void OnEntryRemoved(IDbTransaction tx, ICommit commit, EntryRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

            // Get the userid and old 'where' value.
            var result = tx.Connection.QueryFirstOrDefault(@"
                SELECT [UserId], [Where]
                FROM [dbo].[ReportingProjector.EntryEngagement]
				WHERE [EntryId] = @Id;",
                (object)sqlParams, tx);

            sqlParams.UserId = result.UserId;
            sqlParams.OldWhere = result.Where;

            // Remove the entry and update the entry count on the placement report.
            tx.Connection.Execute(@"
                DELETE FROM [dbo].[ReportingProjector.EntryEngagement]
				WHERE [EntryId] = @Id;

                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				SET [EntryCount] = [EntryCount] - 1
				WHERE [FullyQualifiedTitle] = @OldWhere 
                AND [UserId] = @UserId;",
                (object)sqlParams, tx);
        }
        
        private void OnEntryCommentCreated(IDbTransaction tx, ICommit commit, EntryCommentCreated @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            tx.Connection.Execute(@"
                UPDATE [dbo].[ReportingProjector.EntryEngagement]
		        SET [CommentCount] = [CommentCount] + 1
				WHERE [EntryId] = @Id;",
                (object)sqlParams, tx);
        }
        
        private void OnEntryShared(IDbTransaction tx, ICommit commit, EntryShared @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();
            sqlParams.NewShares = @event.CollaboratorIds.Count;

            // Get the entry 'where' value for use on updating the placement data.
            sqlParams.Where = tx.Connection.ExecuteScalar<string>(@"
                SELECT [Where]
                FROM [dbo].[ReportingProjector.EntryEngagement]
				WHERE [EntryId] = @Id;",
                (object)sqlParams, tx);

            tx.Connection.Execute(
                @"
                UPDATE [dbo].[ReportingProjector.EntryEngagement]
		        SET [SharedCount] = [SharedCount] + @NewShares
				WHERE [EntryId] = @Id;

                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				SET [SharedEntryCount] = [SharedEntryCount] + @NewShares
				WHERE [FullyQualifiedTitle] = @Where 
                AND [UserId] = @UserId;", 
                (object)sqlParams, tx);

            var author = _userService.GetUser(@event.UserId, tx);
            
            // If the author has a tutor, check if they have shared with any of them.
            if(author.HasTutor) {
                var tutors = _tutorGroupService.GetAllTutors(author, tx);
                var sharedWithTutorCount = tutors.Count(tutor => @event.CollaboratorIds.Contains(tutor.Id));
                if(sharedWithTutorCount > 0) {

                    sqlParams.SharedWithTutorCount = sharedWithTutorCount;
                    tx.Connection.Execute(
                        @"
                        UPDATE [dbo].[ReportingProjector.EntryEngagement]
		                SET [SharedWithTutorCount] = [SharedWithTutorCount] + @SharedWithTutorCount
				        WHERE [EntryId] = @Id;

                        UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				        SET [SharedEntryWithTutorCount] = [SharedEntryWithTutorCount] + @SharedWithTutorCount
				        WHERE [FullyQualifiedTitle] = @Where 
                        AND [UserId] = @UserId;",
                        (object)sqlParams, tx);
                }
            }
        }

        private void OnEntryCollaboratorRemoved(IDbTransaction tx, ICommit commit, EntryCollaboratorRemoved @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            // Get the entry 'where' value for use on updating the placement data.
            sqlParams.Where = tx.Connection.ExecuteScalar<string>(@"
                SELECT [Where]
                FROM [dbo].[ReportingProjector.EntryEngagement]
				WHERE [EntryId] = @Id;",
                (object)sqlParams, tx);

            tx.Connection.Execute(
                @"
                UPDATE [dbo].[ReportingProjector.EntryEngagement]
		        SET [SharedCount] = [SharedCount] - 1
				WHERE [EntryId] = @Id;

                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				SET [SharedEntryCount] = [SharedEntryCount] - 1
				WHERE [FullyQualifiedTitle] = @Where 
                AND [UserId] = @UserId;",
                (object)sqlParams, tx);

            var author = _userService.GetUser(@event.UserId, tx);

            // If the author has a tutor, check if they have removed them.
            if (author.HasTutor) {
                var tutors = _tutorGroupService.GetAllTutors(author, tx);
                var tutorRemoved = tutors.Any(tutor => tutor.Id == @event.CollaboratorId);
                if (tutorRemoved) {
                    tx.Connection.Execute(
                        @"
                        UPDATE [dbo].[ReportingProjector.EntryEngagement]
		                SET [SharedWithTutorCount] = [SharedWithTutorCount] - 1
				        WHERE [EntryId] = @Id;

                        UPDATE [dbo].[ReportingProjector.PlacementEngagement]
				        SET [SharedEntryWithTutorCount] = [SharedEntryWithTutorCount] - 1
				        WHERE [FullyQualifiedTitle] = @Where 
                        AND [UserId] = @UserId;",
                        (object)sqlParams, tx);
                }
            }
        }

        private void OnPlacementCreated(IDbTransaction tx, ICommit commit, PlacementCreated @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            tx.Connection.Execute(@"
                INSERT INTO [dbo].[ReportingProjector.PlacementEngagement]
					   ([UserId]
                       ,[PlacementId]
					   ,[Start]
					   ,[End]
                       ,[FullyQualifiedTitle]
                       ,[Type]
					   ,[EntryCount]
					   ,[SharedEntryCount]
					   ,[SharedEntryWithTutorCount])
				 SELECT
					   @UserId
					   ,@Id
					   ,@Start
                       ,@End
                       ,@FullyQualifiedTitle
                       ,@Type
                       ,0
                       ,0
                       ,0
				WHERE NOT EXISTS(SELECT * FROM [dbo].[ReportingProjector.PlacementEngagement] WHERE [PlacementId] = @Id);", (object)sqlParams, tx);
        }

		private void OnPlacementUpdated(IDbTransaction tx, ICommit commit, PlacementUpdated @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            tx.Connection.Execute(@"
                UPDATE [dbo].[ReportingProjector.PlacementEngagement]
                SET [Start] = @Start,
                    [End] = @End,
                    [FullyQualifiedTitle] = @FullyQualifiedTitle,
                    [Type] = @Type
				WHERE [PlacementId] = @Id;", (object)sqlParams, tx);
        }

		private void OnPlacementRemoved(IDbTransaction tx, ICommit commit, PlacementRemoved @event) {
            var sqlParams = @event.ToDynamic();
            sqlParams.Id = commit.AggregateId();

            tx.Connection.Execute(@"
                DELETE FROM [dbo].[ReportingProjector.PlacementEngagement]
				WHERE [PlacementId] = @Id;", (object)sqlParams, tx);
        }

		private void OnSelfAssessmentCreated(IDbTransaction tx, ICommit commit, SkillSelfAssessmentCreated @event) {
            SelfAssessmentCreated(tx, @event.SkillSetId, @event.SelfAssessment);
        }

        private void SelfAssessmentCreated(IDbTransaction tx, int skillSetId, Models.SelfAssessment selfAssessment) {
            var sqlParams = selfAssessment.ToDynamic();
            sqlParams.Date = selfAssessment.CreatedAt.Date;
            sqlParams.SkillSetId = skillSetId;

            tx.Connection.Execute(@"
                INSERT INTO [dbo].[ReportingProjector.SelfAssessmentEngagement]
					   ([UserId]
                       ,[SkillId]
					   ,[SkillSetId]
					   ,[Score]
                       ,[Date]
                       ,[When])
				 SELECT
					   @UserId
					   ,@SkillId
					   ,@SkillSetId
                       ,@Score
                       ,@Date
                       ,@CreatedAt
				WHERE NOT EXISTS(
                        SELECT * 
                        FROM [dbo].[ReportingProjector.SelfAssessmentEngagement] 
                        WHERE [UserId] = @UserId
                        AND [SkillId] = @SkillId
                        AND  [Date] = @Date);

                UPDATE [dbo].[ReportingProjector.SelfAssessmentEngagement]
                SET [Score] = @Score,
                    [When] = @CreatedAt
                WHERE [UserId] = @UserId
                AND [SkillId] = @SkillId
                AND [Date] = @Date
                AND [When] < @CreatedAt;", (object)sqlParams, tx);
        }

        private void OnSelfAssessmentUpdated(IDbTransaction tx, ICommit commit, SkillSelfAssessmentUpdated @event) {
            var sqlParams = @event.SelfAssessment.ToDynamic();
            sqlParams.Id = commit.AggregateId();
            sqlParams.Date = @event.SelfAssessment.CreatedAt.Date;

            tx.Connection.Execute(@"
                UPDATE [dbo].[ReportingProjector.SelfAssessmentEngagement]
                SET [Score] = @Score,
                    [When] = @CreatedAt
                WHERE [UserId] = @UserId
                AND [SkillId] = @SkillId
                AND [Date] = @Date
                AND [When] < @CreatedAt;", 
                (object)sqlParams, tx);
        }

		private void OnSelfAssessmentRemoved(IDbTransaction tx, ICommit commit, SkillSelfAssessmentRemoved @event) {
            var sqlParams = @event.SelfAssessment.ToDynamic();
            sqlParams.Date = @event.SelfAssessment.CreatedAt.Date;

            tx.Connection.Execute(@"
                DELETE FROM [dbo].[ReportingProjector.SelfAssessmentEngagement]
                WHERE [UserId] = @UserId
                AND [SkillId] = @SkillId
                AND [Date] = @Date;",
                (object)sqlParams, tx);

            // If we have a previous assessment, use that.
            if(@event.PreviousSelfAssessment != null) {
                SelfAssessmentCreated(tx, @event.SkillSetId, @event.PreviousSelfAssessment);
            }
        }
	}
}
