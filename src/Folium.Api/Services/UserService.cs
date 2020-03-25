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
using System.IO;
using System.Linq;
using System.Security.Claims;
using Folium.Api.Dtos;
using Folium.Api.Extensions;
using Microsoft.AspNetCore.Hosting;
using System.Data;
using ImageMagick;

namespace Folium.Api.Services {
    public interface IUserService {
        Task<User> GetUserAsync(ClaimsPrincipal claimsPrincipal);
        User GetUser(int userId, IDbTransaction transaction = null);

        Task<User> CreateUserAsync(User user);
	    Task<User> GetOrCreateSystemUserAsync();

		Task<User> RegisterUserSignInAsync(UserDto user);
        Task UpdateUserAsync(User user);
        Task UpdateUserAsync(User user, Stream imageStream);
        Task RemoveUserImage(User user);

        Task RefreshCollaboratorOptionsAsync();
	    IEnumerable<CollaboratorOptionDto> GetCollaboratorOptions(User user, string matchMe);
        Task<bool> CanViewUserDataAsync(User currentUser, User userToView);
    }
    public class UserService : IUserService {
        private readonly IDbService _dbService;
        private readonly ICourseService _courseService;
        private readonly ITutorGroupService _tutorGroupServices;
        private readonly string _profilePicDirectory;		
        private readonly ILogger<UserService> _logger;
	    private readonly List<CollaboratorOptionDto> _collaboratorOptions; // local cache of all possible collaborators. 
        public UserService(
            ILogger<UserService> logger,
            IHostingEnvironment hostingEnvironment,
            IDbService dbService,
            ICourseService courseService,
            ITutorGroupService tutorGroupService) {
            _logger = logger;
            _dbService = dbService;
            _courseService = courseService;
            _tutorGroupServices = tutorGroupService;
            _profilePicDirectory = $"{hostingEnvironment.WebRootPath}{Path.DirectorySeparatorChar}images{Path.DirectorySeparatorChar}profiles{Path.DirectorySeparatorChar}150x150{Path.DirectorySeparatorChar}";
			_collaboratorOptions = new List<CollaboratorOptionDto>();
        }

        public async Task<User> GetUserAsync(ClaimsPrincipal claimsPrincipal) {
	        if (claimsPrincipal.HasClaim("is_system_user", "1")) {
		        return await GetOrCreateSystemUserAsync();
	        }
            using (var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var user = (await connection.QueryAsync<User, ActivitySummary, User>(@"
                    SELECT  [User].*,
                        CASE WHEN EXISTS(
                            SELECT *
					        FROM [dbo].[CourseEnrolment]
					        INNER JOIN [dbo].[Tutee]
						        ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					        INNER JOIN [dbo].[User]
						        ON [CourseEnrolment].[UserId] = [User].[Id]
					        WHERE [User].[Email] = @Email
					        AND [CourseEnrolment].[Active] = 1
					        AND [Tutee].[Removed] = 0) THEN 1 ELSE 0
                        END AS HasTutor,
                        CASE WHEN EXISTS(
                            SELECT *
                            FROM [dbo].[TuteeGroup]
					        INNER JOIN [dbo].[User]
						        ON [TuteeGroup].[TutorId] = [User].[Id]
                            WHERE [User].[Email] = @Email
                            AND [Removed] = 0) THEN 1 ELSE 0
                        END AS HasTutees,
                        [ActivitySummary].*
                    FROM [dbo].[User]
                    LEFT JOIN [dbo].[ActivityProjector.ActivitySummary] [ActivitySummary]
                            ON [User].[Id] = [ActivitySummary].[UserId]
                    WHERE [Email] = @Email",
                    (u, a) => { u.ActivitySummary = a; return u; },
                    new {
                        Email = claimsPrincipal.Email()
                    }, splitOn: "UserId")).FirstOrDefault();
				if(user != null)
                {
                    user.Courses = _courseService.GetActiveCourseEnrolments(user).ToList();
                }
                return user;
            }
        }

        public User GetUser(int userId, IDbTransaction transaction = null) {
            IDbConnection connection = null;
            if (transaction == null) {
                connection = _dbService.GetConnection();
            } else {
                connection = transaction.Connection;
            }
            var closedConnection = connection.State == ConnectionState.Closed;
            if (closedConnection) { 
                connection.Open();
            }
            try { 
				var user = connection.Query<User, ActivitySummary, User>(@"
                    SELECT  [User].*,
                        CASE WHEN EXISTS(
                            SELECT *
					        FROM [dbo].[CourseEnrolment]
					        INNER JOIN [dbo].[Tutee]
						        ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					        INNER JOIN [dbo].[User]
						        ON [CourseEnrolment].[UserId] = [User].[Id]
					        WHERE [User].[Id] = @UserId
					        AND [CourseEnrolment].[Active] = 1
					        AND [Tutee].[Removed] = 0) THEN 1 ELSE 0
                        END AS HasTutor,
                        CASE WHEN EXISTS(
                            SELECT *
                            FROM [dbo].[TuteeGroup]
                            WHERE [TuteeGroup].[TutorId] = @UserId
                            AND [Removed] = 0) THEN 1 ELSE 0
                        END AS HasTutees,
                        [ActivitySummary].*
                    FROM [dbo].[User]
                    LEFT JOIN [dbo].[ActivityProjector.ActivitySummary] [ActivitySummary]
                            ON [User].[Id] = [ActivitySummary].[UserId]
                    WHERE [Id] = @UserId",
                    (u, a) => { u.ActivitySummary = a; return u; },
                    new {
						UserId = userId
					}, transaction, splitOn: "UserId").FirstOrDefault();
				if (user != null) {
                    user.Courses = _courseService.GetActiveCourseEnrolments(user, transaction).ToList();
				}
				return user;
			}
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }

        public async Task<User> CreateUserAsync(User user) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var userId = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO [dbo].[User] 
                    ([Email]
                    ,[HasProfilePic]
                    ,[ProfilePicVersion]
                    ,[FirstName]
                    ,[LastName]
                    ,[LastSignIn]) 
                VALUES 
                    (@Email
                    ,0
                    ,0
                    ,@FirstName
                    ,@LastName
                    ,@When);
                SELECT CAST(SCOPE_IDENTITY() as INT);",
				new {
					user.Email,
					user.FirstName,
					user.LastName,
					When = DateTime.UtcNow
				});
                user.Id = userId;
            }
			await RefreshCollaboratorOptionsAsync();
			return user;
		}

		public async Task<User> GetOrCreateSystemUserAsync() {
			using (var connection = _dbService.GetConnection()) {
				var user = await connection.QueryFirstAsync<User>(@"
                    SET IDENTITY_INSERT [dbo].[User] ON
                    INSERT INTO [dbo].[User] 
                        ([Id]
                        ,[email])
                    SELECT  
                        -1
                        ,'system@user'
                    WHERE NOT EXISTS (
                        SELECT * 
                        FROM [dbo].[User] 
                        WHERE [Id] = -1)
                    SET IDENTITY_INSERT [dbo].[User] OFF
                    
                    SELECT *
                    FROM [dbo].[User] 
                    WHERE [Id] = -1;");

				return user;
			}
		}

		public async Task<User> RegisterUserSignInAsync(UserDto user) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var updatedUser = await connection.ExecuteAsync(@"
					INSERT INTO [dbo].[UserSignInActivity] ([UserId], [When])
					VALUES (@UserId, @When);
					
					UPDATE [dbo].[User] 
                    SET [LastSignIn] = @When
                    WHERE [Id] = @UserId;",
                    new {
						UserId = user.Id,
						When =  DateTime.UtcNow
                    });
            }

            return GetUser(user.Id);
        }

        public async Task UpdateUserAsync(User user) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"
                UPDATE [dbo].[User] 
                SET [FirstName] = @FirstName
                    ,[LastName] = @LastName
                WHERE [Id] = @Id;",
                user);
            }

			await RefreshCollaboratorOptionsAsync();
		}

        public async Task UpdateUserAsync(User user, Stream imageStream) {
            // Resize and save the new image.
            ResizeAndSaveUserImage(user, imageStream);
            
            // Update the db with the new pic version.
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"                    
                    UPDATE [dbo].[User] 
                    SET [ProfilePicVersion] = @ProfilePicVersion
                        ,[HasProfilePic] = 1
                    WHERE [Id] = @UserId;
                    ",
                    new {
                        ProfilePicVersion = user.ProfilePicVersion + 1,
                        UserId = user.Id
                    });
            }

            // Delete the current pic if needed.
            if(user.HasProfilePic) {
                DeleteUserImage(user);
            }

            // Update the user.
            await UpdateUserAsync(user);
        }

        public async Task RemoveUserImage(User user) {            
            // Update the db.
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"                    
                    UPDATE [dbo].[User] 
                    SET [HasProfilePic] = 0
                    WHERE [Id] = @UserId;
                    ",
                    new {
                        UserId = user.Id
                    });
            }

            // Delete the current pic if needed.
            if(user.HasProfilePic) {
                DeleteUserImage(user);
            }

	        await RefreshCollaboratorOptionsAsync();
        }

        /// <summary>
        /// Refresh the local collaborator options list which is used to autocomplete when sharing.
        /// </summary>
        /// <returns></returns>
        public async Task RefreshCollaboratorOptionsAsync() {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var users = await connection.QueryAsync<CollaboratorOptionDto, UserDto, CollaboratorOptionDto>(@"
                    SELECT [User].[Id]
							,ISNULL([User].[FirstName], '') + ' ' + ISNULL([User].[LastName], '') + ' &lt;' + [User].[Email] + '&gt;' AS [Name]
							,[User].*
                    FROM [dbo].[User]
					WHERE [Id] >= 0;",
					(collaboratorOptionDto, user) => { collaboratorOptionDto.User = user; return collaboratorOptionDto; });
				var groups = await connection.QueryParentChildAsync<CollaboratorOptionDto, UserDto, int, List<UserDto>>(@"
                    SELECT	[TuteeGroup].[Id]
							,[Title] + ' Tutor Group' AS [Name]
							,[User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
					ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
					ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
					ON [CourseEnrolment].[UserId] = [User].Id
					WHERE [CourseEnrolment].[Active] = 1
                    AND [TuteeGroup].[Removed] = 0
					AND [Tutee].[Removed] = 0
					UNION					
					SELECT	[TuteeGroup].[Id]
							,[Title] + ' Tutor Group' AS [Name]
							,[User].*
					FROM [dbo].[TuteeGroup]
					INNER JOIN [dbo].[User]
					ON [TuteeGroup].TutorId = [User].Id
                    WHERE [TuteeGroup].[Removed] = 0;",
					collaboratorOptionDto => collaboratorOptionDto.Id,
					collaboratorOptionDto => collaboratorOptionDto.Group ?? (collaboratorOptionDto.Group = new List<UserDto>()),
					(group, user) => group.Add(user));

				_collaboratorOptions.Clear();
				_collaboratorOptions.AddRange(users);
				_collaboratorOptions.AddRange(groups);
			}
		}

	    public IEnumerable<CollaboratorOptionDto> GetCollaboratorOptions(User user, string matchMe) {
		    return string.IsNullOrWhiteSpace(matchMe) 
				? null 
				: _collaboratorOptions.Where(c => c.Name.IndexOf(matchMe, StringComparison.OrdinalIgnoreCase) >= 0 && (!c.IsGroup || c.Group.Exists(u => u.Id == user.Id) /* the user exists within the group */));
        }

        public async Task<bool> CanViewUserDataAsync(User currentUser, User userToView) {
            // The current user can view another users data if they are the same user, the system user or are a tutor for the user.
            if (userToView == null) return false;
            if (userToView.Id == currentUser.Id) return true;
            var systemUser = await GetOrCreateSystemUserAsync();
            if (currentUser.Id == systemUser.Id) return true;
            if(userToView.HasTutor && await _tutorGroupServices.IsUsersTutorAsync(currentUser.Id, userToView.Id)) return true;
            // If the current user is a course admin on any of the courses the user has enrolled on.
            var courseEnrolments = _courseService.GetCourseEnrolments(userToView);
            var courses = await _courseService.GetCoursesAdministratedByUserAsync(currentUser);
            return courseEnrolments.Any(e => courses.Any(c => c.Id == e.CourseId));
        }

        private void DeleteUserImage(User user) {
			var fileName = $"{user.Id}_{user.ProfilePicVersion}.jpg";
			var filePath = _profilePicDirectory + fileName;
            File.Delete(filePath);
        }
        
        private void ResizeAndSaveUserImage(User user, Stream imageStream) {
	        if (imageStream.Length == 0) throw new InvalidDataException("Empty image stream");

			var fileName = $"{user.Id}_{user.ProfilePicVersion + 1}.jpg";
			var filePath = _profilePicDirectory + fileName;

			// Ensure the directory exists.
			if (!Directory.Exists(_profilePicDirectory)) {
		        Directory.CreateDirectory(_profilePicDirectory);
	        }

            // We want a 150x150 image.
			_logger.LogTrace($"Begin resize and crop image");
				
                // Create the requested thumbnail.
                using (var image = new MagickImage(imageStream)) {
                    var newImageSize = new MagickGeometry(150, 150);
                    newImageSize.FillArea = true;
                    image.Thumbnail(newImageSize);
                    image.Crop(150, 150, Gravity.Center);
                    image.RePage();

                    // Save the result
                    image.Write(filePath, MagickFormat.Jpeg);
                }
                
			_logger.LogTrace($"Finished resize and crop image");
		}

    }    
}