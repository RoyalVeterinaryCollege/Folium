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
using System.Collections.Generic;
using System.IO;
using Folium.Api.Models;
using Dapper;
using System.Linq;
using CsvHelper;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonDomain.Persistence;
using Folium.Api.Dtos.Reporting;
using Folium.Api.Dtos;

namespace Folium.Api.Services {
    public interface IReportService {
        Task<IEnumerable<ReportOnOptionDto>> GetReportOnOptionsAsync(User currentUser);
        Task<SelfAssessmentEngagementResultSetDto> GetSelfAssessmentEngagementReportAsync(User currentUser, SelfAssessmentEngagementCriteriaDto criteria);
        Task<DateTime> GetSelfAssessmentEngagementMinDateAsync();
        Task<EntryEngagementResultSetDto> GetEntryEngagementReportAsync(User currentUser, EntryEngagementCriteriaDto criteria);
        Task<DateTime> GetEntryEngagementMinDateAsync();
        Task<PlacementEngagementResultSetDto> GetPlacementEngagementReportAsync(User currentUser, PlacementEngagementCriteriaDto criteria);
        Task<DateTime> GetPlacementEngagementMinDateAsync();
        Task<List<string>> GetPlacementTypesAsync();
    }
    public class ReportService : IReportService {
        private readonly IDbService _dbService;
        private readonly IUserService _userService;
        private readonly ICourseService _courseService;
        private readonly ITutorGroupService _tutorGroupService;

        private DateTime? _selfAssessmentEngagementMinDate;
        private DateTime? _entryEngagementMinDate;
        private DateTime? _placementEngagementMinDate;
        private List<string> _placementTypes;

        public ReportService(
            IDbService dbService,
            IUserService userService,
            ICourseService courseService,
            ITutorGroupService tutorGroupService) {
            _dbService = dbService;
            _userService = userService;
            _courseService = courseService;
            _tutorGroupService = tutorGroupService;
        }

        /// <summary>
        /// Get the users and groups for reporting on.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ReportOnOptionDto>> GetReportOnOptionsAsync(User currentUser) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                return await connection.QueryAsync<ReportOnOptionDto>(@"
                    SELECT [Id]
							,[Name]
							,1 AS [IsUser]
							,0 AS [IsTuteeGroup]
							,0 AS [IsCourse]
							,0 AS [EnrolmentYear]
							,0 AS [CourseYear]
					FROM (
						SELECT [User].[Id]
								,ISNULL([User].[FirstName], '') + ' ' + ISNULL([User].[LastName], '') + ' &lt;' + [User].[Email] + '&gt;' AS [Name]
						FROM [dbo].[User]
						WHERE [Id] = @UserId
						UNION
						SELECT [User].[Id]
								,ISNULL([User].[FirstName], '') + ' ' + ISNULL([User].[LastName], '') + ' &lt;' + [User].[Email] + '&gt;' AS [Name]
						FROM [dbo].[CourseEnrolment]
						INNER JOIN [dbo].[User]
							ON [CourseEnrolment].[UserId] = [User].[Id]
						LEFT JOIN [dbo].[Tutee]
							ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
						LEFT JOIN [dbo].[TuteeGroup]
							ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
						LEFT JOIN [dbo].[CourseAdministrator]
							ON [CourseAdministrator].[UserId] = @UserId
							AND [CourseAdministrator].[CourseId] = [CourseEnrolment].[CourseId]
						WHERE ([TuteeGroup].[TutorId] = @UserId OR [CourseAdministrator].[UserId] IS NOT NULL)
					) AS [Users]
					GROUP BY [Id], [Name]
					UNION
					SELECT	[TuteeGroup].[Id]
							,ISNULL([User].[FirstName], '') + ' ' + ISNULL([User].[LastName], '') + ' ' + [Title] + ' Tutor Group' AS [Name]
							,0 AS [IsUser]
							,1 AS [IsTuteeGroup]
							,0 AS [IsCourse]
							,0 AS [EnrolmentYear]
							,0 AS [CourseYear]
					FROM [dbo].[TuteeGroup]
					INNER JOIN [dbo].[User]
					ON [TuteeGroup].TutorId = [User].Id				
                    LEFT JOIN [dbo].[CourseAdministrator]
                        ON [CourseAdministrator].[UserId] = @UserId
                        AND [CourseAdministrator].[CourseId] = [TuteeGroup].[CourseId]
					WHERE [TuteeGroup].[Removed] = 0
						AND ([TuteeGroup].TutorId = @UserId OR [CourseAdministrator].[UserId] IS NOT NULL)
					UNION
					SELECT	[Course].[Id]
							,[Course].[Title] + ' Year ' + CAST([CourseEnrolment].[CourseYear] AS NVARCHAR(2)) + ' (' + CAST([CourseEnrolment].[EnrolmentYear] AS NVARCHAR(4)) + ')' AS [Name]
							,0 AS [IsUser]
							,0 AS [IsTuteeGroup]
							,1 AS [IsCourse]
							,[CourseEnrolment].[EnrolmentYear]
							,[CourseEnrolment].[CourseYear]
					FROM [dbo].[CourseEnrolment]		
                    INNER JOIN [dbo].[Course]
					ON [Course].[Id] = [CourseEnrolment].[CourseId]
                    INNER JOIN [dbo].[CourseAdministrator]
                        ON [CourseAdministrator].[UserId] = @UserId
                        AND [CourseAdministrator].[CourseId] = [CourseEnrolment].[CourseId]
					GROUP BY [Course].[Id], [Course].[Title], [CourseEnrolment].[CourseYear], [CourseEnrolment].[EnrolmentYear];",
                        new {
                            UserId = currentUser.Id
                        });
            }
        }

        /// <summary>
        /// Get the SelfAssessmentEngagement min date.
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime> GetSelfAssessmentEngagementMinDateAsync() {
            if (_selfAssessmentEngagementMinDate == null) {
                using (var connection = _dbService.GetConnection()) {
                    await connection.OpenAsync();
                    var result = await connection.ExecuteScalarAsync<DateTime?>(@"
                        SELECT MIN([Date])
                        FROM [dbo].[ReportingProjector.SelfAssessmentEngagement]");
                    _selfAssessmentEngagementMinDate = result.HasValue ? result.Value : DateTime.Now;
                }
            }

            return _selfAssessmentEngagementMinDate.Value;
        }

        /// <summary>
        /// Get the SelfAssessmentEngagement Report results.
        /// </summary>
        /// <returns></returns>
        public async Task<SelfAssessmentEngagementResultSetDto> GetSelfAssessmentEngagementReportAsync(User currentUser, SelfAssessmentEngagementCriteriaDto criteria) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();

                // Get the user list.
                var userIds = await GetUsersToReportOn(currentUser, criteria);

                // We could exceed the 2.1k param limit, so chunck this.
                var resultSet = new List<SelfAssessmentEngagementResultDto>();
                var users = new List<User>();
                var userTutors = new Dictionary<int, List<string>>();
                var chunkSize = 2000;

                for (int i = 0; i < userIds.Count; i = i + chunkSize) {
                    var data = await connection.QueryMultipleAsync(@"
                    SELECT [UserId]
                          ,[SkillId]
                          ,[Score]
                          ,[Date]
                      FROM [dbo].[ReportingProjector.SelfAssessmentEngagement]
                      WHERE [ReportingProjector.SelfAssessmentEngagement].[SkillSetId] = @SkillSetId
                        AND [UserId] IN @UserIds
                        AND ([Date] >= @From OR @From IS NULL)
                        AND ([Date] <= @To OR @To IS NULL);

                    SELECT *
                        FROM [dbo].[User]
                        WHERE [User].[Id] IN @UserIds;

                    SELECT [CourseEnrolment].[UserId], [User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [TuteeGroup].[TutorId] = [User].[Id]
					WHERE [CourseEnrolment].[UserId] IN @UserIds
					AND [CourseEnrolment].[Active] = 1
					AND [Tutee].[Removed] = 0
                    AND [TuteeGroup].[Removed] = 0;",
                            new {
                                criteria.SkillSetId,
                                UserIds = userIds.GetRange(i, userIds.Count < (i + chunkSize) ? (userIds.Count - i) : chunkSize),
                                criteria.From,
                                criteria.To
                            });

                    resultSet.AddRange(await data.ReadAsync<SelfAssessmentEngagementResultDto>());
                    users.AddRange(await data.ReadAsync<User>());
                    var tutors = await data.ReadAsync<dynamic>();
                    foreach(var tutor in tutors) {
                        if(userTutors.ContainsKey((int)tutor.UserId)) {
                            userTutors[(int)tutor.UserId].Add(tutor.FirstName + " " + tutor.LastName);
                        } else {
                            userTutors.Add((int)tutor.UserId, new List<string>{ tutor.FirstName + " " + tutor.LastName });
                        }
                    }
                }
                return new SelfAssessmentEngagementResultSetDto {
                    DataSet = resultSet,
                    Criteria = criteria,
                    Users = users.Select(u => new ReportUserDto(u) { Tutors = userTutors.ContainsKey(u.Id) ? userTutors[u.Id] : new List<string>() }).ToList()
                };
            }
        }

        /// <summary>
        /// Get the EntryEngagement Report results.
        /// </summary>
        /// <returns></returns>
        public async Task<EntryEngagementResultSetDto> GetEntryEngagementReportAsync(User currentUser, EntryEngagementCriteriaDto criteria) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();

                // Get the user list.
                var userIds = await GetUsersToReportOn(currentUser, criteria);

                // We could exceed the 2.1k param limit, so chunck this.
                var resultSet = new List<EntryEngagementResultDto>();
                var users = new List<User>();
                var userTutors = new Dictionary<int, List<string>>();
                var chunkSize = 2000;

                for (int i = 0; i < userIds.Count; i = i + chunkSize) {
                    var data = await connection.QueryMultipleAsync(@"
                      SELECT
                           [UserId]
                          ,[EntryTypeId]
                          ,[When]
                          ,[SharedCount]
                          ,[SharedWithTutorCount]
                          ,[CommentCount]
                          ,[IsSignOffCompatible]
                          ,[SignOffRequestCount]
                          ,[SignedOff]
                      FROM [dbo].[ReportingProjector.EntryEngagement]
                      WHERE [UserId] IN @UserIds "
                      + (criteria.EntryTypeIds != null && criteria.EntryTypeIds.Count > 0
                        ? criteria.BasicEntryType ? "AND (([EntryTypeId] IN @EntryTypeIds) OR EntryTypeId IS NULL)"
                                                  : "AND ([EntryTypeId] IN @EntryTypeIds)"
                        : criteria.BasicEntryType ? "AND EntryTypeId IS NULL"
                                                  : "")
                      + @"
                        AND ([When] >= @From OR @From IS NULL)
                        AND ([When] <= @To OR @To IS NULL);

                    SELECT *
                        FROM [dbo].[User]
                        WHERE [User].[Id] IN @UserIds;

                    SELECT [CourseEnrolment].[UserId], [User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [TuteeGroup].[TutorId] = [User].[Id]
					WHERE [CourseEnrolment].[UserId] IN @UserIds
					AND [CourseEnrolment].[Active] = 1
					AND [Tutee].[Removed] = 0
                    AND [TuteeGroup].[Removed] = 0;",
                            new {
                                criteria.EntryTypeIds,
                                UserIds = userIds.GetRange(i, userIds.Count < (i + chunkSize) ? (userIds.Count - i) : chunkSize),
                                criteria.From,
                                criteria.To
                            });
                    
                    resultSet.AddRange(await data.ReadAsync<EntryEngagementResultDto>());
                    users.AddRange(await data.ReadAsync<User>());
                    var tutors = await data.ReadAsync<dynamic>();
                    foreach (var tutor in tutors) {
                        if (userTutors.ContainsKey((int)tutor.UserId)) {
                            userTutors[(int)tutor.UserId].Add(tutor.FirstName + " " + tutor.LastName);
                        }
                        else {
                            userTutors.Add((int)tutor.UserId, new List<string> { tutor.FirstName + " " + tutor.LastName });
                        }
                    }
                }
                return new EntryEngagementResultSetDto {
                    DataSet = resultSet,
                    Criteria = criteria,
                    Users = users.Select(u => new ReportUserDto(u) { Tutors = userTutors.ContainsKey(u.Id) ? userTutors[u.Id] : new List<string>() }).ToList()
                };
            }
        }

        /// <summary>
        /// Get the EntryEngagement min date.
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime> GetEntryEngagementMinDateAsync() {
            if (_entryEngagementMinDate == null) {
                using (var connection = _dbService.GetConnection()) {
                    await connection.OpenAsync();
                    var result = await connection.ExecuteScalarAsync<DateTime?>(@"
                        SELECT MIN([When])
                        FROM [dbo].[ReportingProjector.EntryEngagement]");
                    _entryEngagementMinDate = result.HasValue ? result.Value : DateTime.Now;
                }
            }

            return _entryEngagementMinDate.Value;
        }

        /// <summary>
        /// Get the PlacementEngagement Report results.
        /// </summary>
        /// <returns></returns>
        public async Task<PlacementEngagementResultSetDto> GetPlacementEngagementReportAsync(User currentUser, PlacementEngagementCriteriaDto criteria) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();

                // Get the user list.
                var userIds = await GetUsersToReportOn(currentUser, criteria);

                // We could exceed the 2.1k param limit, so chunck this.
                var resultSet = new List<PlacementEngagementResultDto>();
                var users = new List<User>();
                var userTutors = new Dictionary<int, List<string>>();
                var chunkSize = 2000;

                for (int i = 0; i < userIds.Count; i = i + chunkSize) {
                    var data = await connection.QueryMultipleAsync(@"
                    SELECT
                           [UserId]
                          ,[EntryCount]
                          ,[SharedEntryCount]
                          ,[SharedEntryWithTutorCount]
                          ,[EntrySignOffCompatibleCount]
                          ,[EntrySignOffRequestCount]
                          ,[EntrySignedOffCount]
                      FROM [dbo].[ReportingProjector.PlacementEngagement]
                      WHERE [UserId] IN @UserIds
                        AND ([Start] >= @From OR @From IS NULL)
                        AND ([End] <= @To OR @To IS NULL)
                        AND ([Type] = @Type OR @Type IS NULL);

                    SELECT *
                        FROM [dbo].[User]
                        WHERE [User].[Id] IN @UserIds;

                    SELECT [CourseEnrolment].[UserId], [User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
						ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
						ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
						ON [TuteeGroup].[TutorId] = [User].[Id]
					WHERE [CourseEnrolment].[UserId] IN @UserIds
					AND [CourseEnrolment].[Active] = 1
					AND [Tutee].[Removed] = 0
                    AND [TuteeGroup].[Removed] = 0;",
                            new {
                                UserIds = userIds.GetRange(i, userIds.Count < (i + chunkSize) ? (userIds.Count - i) : chunkSize),
                                criteria.From,
                                criteria.To,
                                criteria.Type
                            });

                    resultSet.AddRange(await data.ReadAsync<PlacementEngagementResultDto>());
                    users.AddRange(await data.ReadAsync<User>());
                    var tutors = await data.ReadAsync<dynamic>();
                    foreach (var tutor in tutors) {
                        if (userTutors.ContainsKey((int)tutor.UserId)) {
                            userTutors[(int)tutor.UserId].Add(tutor.FirstName + " " + tutor.LastName);
                        }
                        else {
                            userTutors.Add((int)tutor.UserId, new List<string> { tutor.FirstName + " " + tutor.LastName });
                        }
                    }
                }
                return new PlacementEngagementResultSetDto {
                    DataSet = resultSet,
                    Criteria = criteria,
                    Users = users.Select(u => new ReportUserDto(u) { Tutors = userTutors.ContainsKey(u.Id) ? userTutors[u.Id] : new List<string>() }).ToList()
                };
            }
        }

        /// <summary>
        /// Get the PlacementEngagement min date.
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime> GetPlacementEngagementMinDateAsync() {
            if (_placementEngagementMinDate == null) {
                using (var connection = _dbService.GetConnection()) {
                    await connection.OpenAsync();
                    var result = await connection.ExecuteScalarAsync<DateTime?>(@"
                        SELECT MIN([Start])
                        FROM [dbo].[ReportingProjector.PlacementEngagement]
                        WHERE [Start] IS NOT NULL");
                    _placementEngagementMinDate = result.HasValue ? result.Value : DateTime.Now;
                }
            }

            return _placementEngagementMinDate.Value;
        }

        /// <summary>
        /// Get the Placement Types.
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetPlacementTypesAsync() {
            if (_placementTypes == null) {
                using (var connection = _dbService.GetConnection()) {
                    await connection.OpenAsync();
                    var result = await connection.QueryAsync<string>(@"
                        SELECT DISTINCT [Type]
                        FROM [dbo].[ReportingProjector.PlacementEngagement]");
                    _placementTypes = result.ToList();
                }
            }

            return _placementTypes;
        }

        private async Task<List<int>> GetUsersToReportOn(User currentUser, CriteriaDto criteria) {
            // Get the courses administered by the current user.
            var courses = await _courseService.GetCoursesAdministratedByUserAsync(currentUser);

            // Get the user list.
            var userIds = new List<int>();
            foreach (var who in criteria.Who) {
                if (who.IsUser && await _userService.CanViewUserDataAsync(currentUser, _userService.GetUser(who.Id))) {
                    userIds.Add(who.Id);
                }
                if (who.IsTuteeGroup) {
                    var tuteeGroup = await _tutorGroupService.GetTuteeGroupAsync(who.Id);
                    if (tuteeGroup.TutorId == currentUser.Id || courses.Any(c => c.Id == tuteeGroup.CourseId)) {
                        userIds.AddRange(tuteeGroup.Tutees.Select(t => t.Id));
                    }
                }
                if (who.IsCourse && courses.Any(c => c.Id == who.Id)) {
                    var courseEnrolments = _courseService.GetCourseEnrolments(who.Id, who.EnrolmentYear, who.CourseYear);
                        // Removed the below - not sure it is needed?
                        //.Where(e => (!criteria.To.HasValue || criteria.To.Value.ToUniversalTime().Date == DateTime.UtcNow.Date) ? e.Active : e.LastUpdatedAt.Date >= criteria.To.Value.ToUniversalTime().Date); // If a to date has been specified make sure it is less than the last update date of the enrolment, to make sure it is valid.
                    userIds.AddRange(courseEnrolments.Select(e => e.UserId));
                }
            }

            return userIds.Distinct().ToList(); // Remove any dups.
        }
    }
}