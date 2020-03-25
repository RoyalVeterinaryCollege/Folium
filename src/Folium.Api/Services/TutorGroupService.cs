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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using Folium.Api.Dtos;
using System.Data;

namespace Folium.Api.Services {
    public interface ITutorGroupService {
	    Task<IEnumerable<User>> GetTutorsAsync(User user, int courseId);
        IEnumerable<User> GetAllTutors(User user, IDbTransaction transaction = null);
        Task<IEnumerable<TuteeGroupDto>> GetTuteesAsync(int userId);
        Task<bool> IsUsersTutorAsync(int tutorId, int userId);
        Task<TuteeGroupDto> GetTuteeGroupAsync(int tuteeGroupId);
    }
    public class TutorGroupService : ITutorGroupService {
        private readonly IDbService _dbService;
        private readonly ILogger<UserService> _logger;
        public TutorGroupService (
            ILogger<UserService> logger,
            IDbService dbService) {
            _logger = logger;
            _dbService = dbService;
        }
                
        public async Task<IEnumerable<TuteeGroupDto>> GetTuteesAsync(int userId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var tutorGroups = new List<TuteeGroupDto>();
                await connection.QueryAsync<TuteeGroup, User, ActivitySummary, TuteeGroupDto>(@"
                    SELECT [TuteeGroup].*, [User].*, [ActivitySummary].[UserId] AS [Id], [ActivitySummary].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [CourseEnrolment].[UserId] = [User].[Id]
                    LEFT JOIN [dbo].[ActivityProjector.ActivitySummary] [ActivitySummary]
                            ON [User].[Id] = [ActivitySummary].[UserId]
					WHERE [CourseEnrolment].[Active] = 1
					    AND [Tutee].[Removed] = 0
                        AND [TuteeGroup].[Removed] = 0
                        AND [TuteeGroup].[TutorId] = @UserId;",
                    (tuteeGroup, user, activitySummary) => {
                        var group = tutorGroups.FirstOrDefault(t => t.Id == tuteeGroup.Id);
                        user.ActivitySummary = activitySummary;
                        if (group == null) {
                            group = new TuteeGroupDto {
                                Id = tuteeGroup.Id,
                                Title = tuteeGroup.Title,
                                CourseId = tuteeGroup.CourseId,
                                TutorId = tuteeGroup.TutorId,
                                Tutees = new List<UserDto> { new UserDto(user) }
                            };
                            tutorGroups.Add(group);
                        }
                        else {
                            group.Tutees.Add(new UserDto(user));
                        }
                        return group;
                    },
                    new {
                        UserId = userId
                    });
                return tutorGroups;
            }
        }

        public async Task<TuteeGroupDto> GetTuteeGroupAsync(int tuteeGroupId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var tutorGroups = new List<TuteeGroupDto>();
                await connection.QueryAsync<TuteeGroup, User, ActivitySummary, TuteeGroupDto>(@"
                    SELECT [TuteeGroup].*, [User].*, [ActivitySummary].[UserId] AS [Id], [ActivitySummary].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [CourseEnrolment].[UserId] = [User].[Id]
                    LEFT JOIN [dbo].[ActivityProjector.ActivitySummary] [ActivitySummary]
                            ON [User].[Id] = [ActivitySummary].[UserId]
					WHERE [CourseEnrolment].[Active] = 1
					    AND [Tutee].[Removed] = 0
                        AND [TuteeGroup].[Id] = @TuteeGroupId;",
                    (tuteeGroup, user, activitySummary) => {
                        var group = tutorGroups.FirstOrDefault(t => t.Id == tuteeGroup.Id);
                        user.ActivitySummary = activitySummary;
                        if (group == null) {
                            group = new TuteeGroupDto {
                                Id = tuteeGroup.Id,
                                Title = tuteeGroup.Title,
                                CourseId = tuteeGroup.CourseId,
                                TutorId = tuteeGroup.TutorId,
                                Tutees = new List<UserDto> { new UserDto(user) }
                            };
                            tutorGroups.Add(group);
                        }
                        else {
                            group.Tutees.Add(new UserDto(user));
                        }
                        return group;
                    },
                    new {
                        TuteeGroupId = tuteeGroupId
                    });
                return tutorGroups.FirstOrDefault();
            }
        }

        public async Task<IEnumerable<User>> GetTutorsAsync(User user, int courseId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var tutors = await connection.QueryAsync<User>(@"
                    SELECT [User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [TuteeGroup].[TutorId] = [User].[Id]
					WHERE [CourseEnrolment].[UserId] = @userId
					AND [CourseEnrolment].[CourseId] = @courseId 
					AND [CourseEnrolment].[Active] = 1
					AND [Tutee].[Removed] = 0
					AND [TuteeGroup].[Removed] = 0;",
					new {
						userId = user.Id,
						courseId
					});
				return tutors;
			}
        }

        public IEnumerable<User> GetAllTutors(User user, IDbTransaction transaction = null) {
            IDbConnection connection = null;
            if (transaction == null) {
                connection = _dbService.GetConnection();
            }
            else {
                connection = transaction.Connection;
            }
            var closedConnection = connection.State == ConnectionState.Closed;
            if (closedConnection) {
                connection.Open();
            }
            try {
                var tutors = connection.Query<User>(@"
                    SELECT [User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [TuteeGroup].[TutorId] = [User].[Id]
					WHERE [CourseEnrolment].[UserId] = @userId
					AND [CourseEnrolment].[Active] = 1
					AND [Tutee].[Removed] = 0
					AND [TuteeGroup].[Removed] = 0;",
                    new {
                        userId = user.Id
                    }, transaction);
                return tutors;
            }
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }

        public async Task<bool> IsUsersTutorAsync(int tutorId, int userId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var tutorMatchCount = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					WHERE  [TuteeGroup].[TutorId] = @tutorId
                    AND [CourseEnrolment].[UserId] = @userId
                    AND [CourseEnrolment].[Active] = 1
					AND [Tutee].[Removed] = 0;",
                    new
                    {
                        userId = userId,
                        tutorId = tutorId
                    });
                return tutorMatchCount > 0;
            }
        }
    }    
}