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
using Folium.Api.Models.Messaging;

namespace Folium.Api.Projections.Activity {
	[Projector(5)]
	public class ActivityProjector : ProjectorBase {
		private readonly ConventionBasedCommitProjecter _conventionProjector;
        private readonly IEntryService _entryService;
        private readonly IUserService _userService;
        private readonly IOptions<Configuration> _applicationConfiguration;

        public ActivityProjector(
            IDbService dbService, 
            IPersistStreams persistStreams,
            IEntryService entryService,
            IUserService userService,
            ILogger<ActivityProjector> logger,
            IOptions<Configuration> applicationConfiguration) : base(persistStreams, dbService) {
            var conventionalDispatcher = new ConventionBasedEventDispatcher(c => Checkpoint = c.ToSome(), commit => {
                logger.LogWarning($"Commit contains null events. CommitId:{commit.CommitId}, CommitSequence:{commit.CommitSequence}, StreamId:{commit.StreamId}, StreamRevision:{commit.StreamRevision}, EventCount:{commit.Events.Count}, AggregateId {commit.AggregateId()}");
            })
				.FirstProject<EntryCreated>(OnEntryCreated)
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
				.ThenProject<EntryCommentCreated>(OnEntryCommentCreated)
                .ThenProject<MessageCreated>(OnMessageCreated);

            _conventionProjector = new ConventionBasedCommitProjecter(this, dbService, conventionalDispatcher);
            _entryService = entryService;
            _userService = userService;
            _applicationConfiguration = applicationConfiguration;
        }

        public override void Project(ICommit commit) {
			_conventionProjector.Project(commit);
        }

        private void CreateEmailNotification(IDbTransaction tx, Models.EmailNotification emailNotification) {
            var sqlParams = emailNotification.ToDynamic();

            const string sql = @"
				INSERT INTO [dbo].[ActivityProjector.EmailNotification]
					   ([Id]
                       ,[To]
					   ,[Subject]
					   ,[HtmlBody]
                       ,[ActionLink]
                       ,[ActionTitle]
                       ,[When]
                       ,[UserId])
				 SELECT
                        @Id
					   ,@To
					   ,@Subject
					   ,@HtmlBody
                       ,@ActionLink
                       ,@ActionTitle
                       ,@When
                       ,@UserId
				WHERE NOT EXISTS(SELECT * FROM [dbo].[ActivityProjector.EmailNotification] WHERE [UserId] = @UserId AND [When] = @When AND [To] = @To);";
            var insertedRowCount = tx.Connection.Execute(sql, (object)sqlParams, tx);
                        
            if (insertedRowCount == 1) {
                // Schedule the email to be sent on a background thread.
                BackgroundJob.Enqueue<IEmailService>(service => service.SendEmail(emailNotification.Id));
            }
        }

        private void RecordActivity(IDbTransaction tx, Models.Activity activity) {
            var sqlParams = new {
                activity.When,
                activity.Type,
                activity.UserId,
                activity.Title,
                activity.Link,
                EntryIncrement = activity.Type == (int)ActivityType.EntryCreated ? 1 : (activity.Type == (int)ActivityType.EntryRemoved ? -1 : 0),
                SelfAssessmentIncrement = (activity.Type == (int)ActivityType.SelfAssessmentCreated || activity.Type == (int)ActivityType.SelfAssessmentUpdated) ? 1 : (activity.Type == (int)ActivityType.SelfAssessmentRemoved ? -1 : 0),
                PlacementIncrement = activity.Type == (int)ActivityType.PlacementCreated ? 1 : (activity.Type == (int)ActivityType.PlacementRemoved ? -1 : 0)
            };

			const string sql = @"
                INSERT INTO [dbo].[ActivityProjector.ActivitySummary]
					   ([UserId]
					   ,[TotalEntries]
					   ,[TotalSelfAssessments]
					   ,[TotalPlacements])
				 SELECT
					   @UserId
					   ,0
                       ,0
                       ,0
				WHERE NOT EXISTS(SELECT * FROM [dbo].[ActivityProjector.ActivitySummary] WHERE [UserId] = @UserId);

				UPDATE [dbo].[ActivityProjector.ActivitySummary]
                SET [TotalEntries] = [TotalEntries] + @EntryIncrement
                    ,[TotalSelfAssessments] = [TotalSelfAssessments] + @SelfAssessmentIncrement
                    ,[TotalPlacements] = [TotalPlacements] + @PlacementIncrement
				WHERE [UserId] = @UserId
                AND NOT EXISTS(SELECT * FROM [dbo].[ActivityProjector.Activity] WHERE [UserId] = @UserId AND [Type] = @Type AND [When] = @When AND [Link] = @Link);

				INSERT INTO [dbo].[ActivityProjector.Activity]
					   ([UserId]
					   ,[Type]
					   ,[When]
					   ,[Title]
					   ,[Link])
				 SELECT
					   @UserId
					   ,@Type
					   ,@When
					   ,@Title
					   ,@Link
				WHERE NOT EXISTS(SELECT * FROM [dbo].[ActivityProjector.Activity] WHERE [UserId] = @UserId AND [Type] = @Type AND [When] = @When AND [Link] = @Link);";
			tx.Connection.Execute(sql, (object) sqlParams, tx);
        }

        private void OnEntryCreated(IDbTransaction tx, ICommit commit, EntryCreated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			// Record who created the entry so it can be used when it is updated.
			const string sql = @"
				INSERT INTO [dbo].[ActivityProjector.Entry]
					   ([EntryId]
					   ,[UserId])
				 SELECT
					   @Id
					   ,@UserId
				WHERE NOT EXISTS(SELECT * FROM [dbo].[ActivityProjector.Entry] WHERE [EntryId] = @Id);";
			tx.Connection.Execute(sql, (object) sqlParams, tx);

			RecordActivity(tx, new Models.Activity {
				UserId = @event.UserId,
				Type = (int) ActivityType.EntryCreated,
				When = @event.CreatedAt,
				Link = commit.AggregateId().ToString()
			});
        }

		private void OnEntryUpdated(IDbTransaction tx, ICommit commit, EntryUpdated @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			// Get the original where entry.
			const string sql = @"
				SELECT [UserId]
				FROM [dbo].[ActivityProjector.Entry]
				WHERE [EntryId] = @Id;";
			var userId = tx.Connection.QuerySingle<int>(sql, (object) sqlParams, tx);

			RecordActivity(tx, new Models.Activity {
				UserId = userId,
				Type = (int) ActivityType.EntryUpdated,
				When = @event.LastUpdatedAt,
				Link = commit.AggregateId().ToString()
			});
		}

		private void OnEntryRemoved(IDbTransaction tx, ICommit commit, EntryRemoved @event) {
			var sqlParams = @event.ToDynamic();
			sqlParams.Id = commit.AggregateId();

			// Get the original where entry.
			const string sql1 = @"
				SELECT [UserId]
				FROM [dbo].[ActivityProjector.Entry]
				WHERE [EntryId] = @Id;";
			var userId = tx.Connection.QuerySingle<int>(sql1, (object) sqlParams, tx);

			const string sql2 = @"
				DELETE FROM [dbo].[ActivityProjector.Entry]
				WHERE [EntryId] = @Id;";
			tx.Connection.Execute(sql2, (object) sqlParams, tx);

			RecordActivity(tx, new Models.Activity {
				UserId = userId,
				Type = (int) ActivityType.EntryRemoved,
				When = commit.CommitStamp,
				Link = commit.AggregateId().ToString()
			});
        }

		private void OnPlacementCreated(IDbTransaction tx, ICommit commit, PlacementCreated @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.UserId,
				Type = (int) ActivityType.PlacementCreated,
				When = @event.CreatedAt,
				Link = commit.AggregateId().ToString()
			});
        }

		private void OnPlacementUpdated(IDbTransaction tx, ICommit commit, PlacementUpdated @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.UserId,
				Type = (int) ActivityType.PlacementUpdated,
				When = @event.LastUpdatedAt,
				Link = commit.AggregateId().ToString()
			});
		}

		private void OnPlacementRemoved(IDbTransaction tx, ICommit commit, PlacementRemoved @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.UserId,
				Type = (int) ActivityType.PlacementRemoved,
				When = commit.CommitStamp,
				Link = commit.AggregateId().ToString()
			});
        }
		private void OnSelfAssessmentCreated(IDbTransaction tx, ICommit commit, SkillSelfAssessmentCreated @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.SelfAssessment.UserId,
				Type = (int)ActivityType.SelfAssessmentCreated,
				When = @event.SelfAssessment.CreatedAt,
                Link = @event.SelfAssessment.SkillId.ToString()
            });
        }

		private void OnSelfAssessmentUpdated(IDbTransaction tx, ICommit commit, SkillSelfAssessmentUpdated @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.SelfAssessment.UserId,
				Type = (int)ActivityType.SelfAssessmentUpdated,
				When = @event.SelfAssessment.CreatedAt,
				Link = @event.SelfAssessment.SkillId.ToString()
			});
        }

		private void OnSelfAssessmentRemoved(IDbTransaction tx, ICommit commit, SkillSelfAssessmentRemoved @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.SelfAssessment.UserId,
				Type = (int)ActivityType.SelfAssessmentRemoved,
				When = commit.CommitStamp,
                Link = @event.SelfAssessment.SkillId.ToString()
            });
        }

		private void OnEntryCommentCreated(IDbTransaction tx, ICommit commit, EntryCommentCreated @event) {
            RecordActivity(tx, new Models.Activity {
                UserId = @event.CreatedBy,
                Type = (int)ActivityType.EntryCommentCreated,
                When = @event.CreatedAt,
                Link = $"{commit.AggregateId()},{@event.Id}"
            });
            var entryAuthor = _entryService.GetEntryAuthor(commit.AggregateId(), tx);
            var author = new UserDto(_userService.GetUser(@event.CreatedBy, tx));
            var collaborators = 
                (_entryService.GetCollaborators(commit.AggregateId())).ToArray()
                .Union(new[] { entryAuthor })
                .Where(c => c.Id != @event.CreatedBy);

            foreach(var collaborator in collaborators) {
                var notification = new Models.EmailNotification {
                    Id = Guid.NewGuid(),
                    UserId = @event.CreatedBy,
                    To = collaborator.Email,
                    When = @event.CreatedAt,
                    Subject = $"{author.FirstName} {author.LastName} has made a comment",
                    HtmlBody = $"<p>{collaborator.FirstName} {collaborator.LastName},</p> <p>Just to let you know {author.FirstName} {author.LastName} has made a new comment on an entry in Folium.</p>",
                    ActionLink = $"{_applicationConfiguration.Value.BaseUrl}/entries/{commit.AggregateId()}?comment-id={@event.Id}",
                    ActionTitle = "View Entry"
                };
                CreateEmailNotification(tx, notification);
            }
        }

		private void OnEntryCollaboratorRemoved(IDbTransaction tx, ICommit commit, EntryCollaboratorRemoved @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.UserId,
				Type = (int)ActivityType.EntryCollaboratorRemoved,
				When = commit.CommitStamp,
				Link = commit.AggregateId().ToString()
			});
		}

		private void OnEntryShared(IDbTransaction tx, ICommit commit, EntryShared @event) {
			RecordActivity(tx, new Models.Activity {
				UserId = @event.UserId,
				Type = (int)ActivityType.EntryShared,
				When = commit.CommitStamp,
				Link = commit.AggregateId().ToString()
			});

            var author = _userService.GetUser(@event.UserId, tx);
            var collaborators = @event.CollaboratorIds.Select(collaboratorId => _userService.GetUser(collaboratorId, tx));

            foreach(var collaborator in collaborators) {
                var customText = string.IsNullOrWhiteSpace(@event.Message) ? "" : $"<p>{WebUtility.HtmlEncode(@event.Message)}</p>";
                var notification = new Models.EmailNotification {
                    Id = Guid.NewGuid(),
                    UserId = @event.UserId,
                    To = collaborator.Email,
                    When = commit.CommitStamp,
                    Subject = $"{author.FirstName} {author.LastName} has shared an entry with you",
                    HtmlBody = $"<p>{collaborator.FirstName} {collaborator.LastName},</p> <p>Just to let you know {author.FirstName} {author.LastName} has shared an entry in Folium with you.</p>{customText}<p style='line-height:2.5;'>Thanks<br>The Folium Team</p>",
                    ActionLink = $"{_applicationConfiguration.Value.BaseUrl}/entries/{commit.AggregateId()}",
                    ActionTitle = "View Entry"
                };
                CreateEmailNotification(tx, notification);
            }
        }
        
        private void OnMessageCreated(IDbTransaction tx, ICommit comment, MessageCreated @event) {
            // Get the users.
            var fromUser = _userService.GetUser(@event.FromUserId, tx);
            var toUser = _userService.GetUser(@event.ToUserId, tx);

            var notification = new Models.EmailNotification {
                Id = Guid.NewGuid(),
                UserId = @event.FromUserId,
                To = toUser.Email,
                When = @event.CreatedAt,
                Subject = $"{fromUser.FirstName} {fromUser.LastName} has sent you a message",
                HtmlBody = $"<p>{toUser.FirstName} {toUser.LastName},</p>{@event.Body}<p><p style='line-height:2.5;'>{fromUser.FirstName} {fromUser.LastName}<br><a href='mailto:{fromUser.Email}'>{fromUser.Email}</a></p>"
            };
            CreateEmailNotification(tx, notification);
        }

        public enum ActivityType {
			EntryCreated = 1,
			EntryUpdated = 2,
			EntryRemoved = 3,
			PlacementCreated = 4,
			PlacementUpdated = 5,
			PlacementRemoved = 6,
			SelfAssessmentCreated = 7,
			SelfAssessmentUpdated = 8,
			SelfAssessmentRemoved = 9,
			EntryCommentCreated = 10,
			EntryCollaboratorRemoved = 11,
			EntryShared = 12
		}
	}
}
