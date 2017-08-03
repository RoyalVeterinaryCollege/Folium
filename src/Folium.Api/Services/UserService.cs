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
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using System.Data;

namespace Folium.Api.Services {
    public interface IUserService {
        Task<User> GetUserAsync(ClaimsPrincipal claimsPrincipal);
	    Task<User> GetUserAsync(int userId);
        User GetUser(int userId, IDbTransaction transaction = null);

        Task<User> CreateUserAsync(User user);
	    Task<User> GetOrCreateSystemUserAsync();

		Task<User> RegisterUserSignInAsync(UserDto user);
        Task UpdateUserAsync(User user);
        Task UpdateUserAsync(User user, Stream imageStream);
        Task RemoveUserImage(User user);
	    Task<IEnumerable<User>> GetTutorsAsync(User user, int courseId);
	    Task RefreshCollaboratorOptionsAsync();
	    IEnumerable<CollaboratorOptionDto> GetCollaboratorOptions(User user, string matchMe);
    }
    public class UserService : IUserService {
        private readonly IDbService _dbService;
        private readonly string _profilePicDirectory;		
        private readonly ILogger<UserService> _logger;
	    private readonly List<CollaboratorOptionDto> _collaboratorOptions; // local cache of all possible collaborators. 
        public UserService(
            ILogger<UserService> logger,
            IHostingEnvironment hostingEnvironment,
            IDbService dbService) {
            _logger = logger;
            _dbService = dbService;            
            _profilePicDirectory = $"{hostingEnvironment.WebRootPath}{Path.DirectorySeparatorChar}images{Path.DirectorySeparatorChar}profiles{Path.DirectorySeparatorChar}150x150{Path.DirectorySeparatorChar}";
			_collaboratorOptions = new List<CollaboratorOptionDto>();
        }

        public async Task<User> GetUserAsync(ClaimsPrincipal claimsPrincipal) {
	        if (claimsPrincipal.HasClaim("is_system_user", "1")) {
		        return await GetOrCreateSystemUserAsync();
	        }
            using (var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var user = await connection.QueryFirstOrDefaultAsync<User>(@"
                    SELECT *
                    FROM [dbo].[User]
                    WHERE [Email] = @Email",
                    new {
                        Email = claimsPrincipal.Email()
                    });
				if(user != null) {
					user.Courses = (await connection.QueryAsync<int>(@"
					SELECT [CourseId]
					FROM [dbo].[CourseEnrolment]
					WHERE [UserId] = @UserId
					AND [Removed] = 0
					",
						new {
							UserId = user.Id
						})).ToList();
				}
	            return user;
            }
		}
		public async Task<User> GetUserAsync(int userId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var user = await connection.QueryFirstOrDefaultAsync<User>(@"
                    SELECT *
                    FROM [dbo].[User]
                    WHERE [Id] = @UserId",
					new {
						UserId = userId
					});
				if (user != null) {
					user.Courses = (await connection.QueryAsync<int>(@"
					SELECT [CourseId]
					FROM [dbo].[CourseEnrolment]
					WHERE [UserId] = @UserId
					AND [Removed] = 0
					",
						new {
							UserId = userId
						})).ToList();
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
				var user = connection.QueryFirstOrDefault<User>(@"
                    SELECT *
                    FROM [dbo].[User]
                    WHERE [Id] = @UserId",
					new {
						UserId = userId
					}, transaction);
				if (user != null) {
					user.Courses = connection.Query<int>(@"
					    SELECT [CourseId]
					    FROM [dbo].[CourseEnrolment]
					    WHERE [UserId] = @UserId
					    AND [Removed] = 0
					    ",
						new {
							UserId = userId
						}, transaction).ToList();
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
                var updatedUser = await connection.QueryFirstAsync<User>(@"
					INSERT INTO [dbo].[UserSignInActivity] ([UserId], [When])
					VALUES (@UserId, @When);
					
					UPDATE [dbo].[User] 
                    SET [LastSignIn] = @When
                    WHERE [Email] = @Email;

                    SELECT *
                    FROM [dbo].[User]
                    WHERE [Email] = @Email;",
                    new {
                        Email = user.Email,
						UserId = user.Id,
						When =  DateTime.UtcNow
                    });
                return updatedUser;
            }
        }
        public async Task UpdateUserAsync(User user) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"
                UPDATE [dbo].[User] 
                SET [FirstName] = @FirstName
                    ,[LastName] = @LastName
                WHERE [Email] = @email;",                
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
                    WHERE [Email] = @email
                    ",
                    new {
                        ProfilePicVersion = user.ProfilePicVersion + 1,
                        user.Email
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
                    WHERE [Email] = @email
                    ",
                    new {
                        user.Email
                    });
            }

            // Delete the current pic if needed.
            if(user.HasProfilePic) {
                DeleteUserImage(user);
            }

	        await RefreshCollaboratorOptionsAsync();
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
					AND [CourseEnrolment].[Removed] = 0;",
					new {
						userId = user.Id,
						courseId
					});
				return tutors;
			}
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
							,'""' + ISNULL([User].[FirstName], '') + ' ' + ISNULL([User].[LastName], '') + '"" &lt;' + [User].[Email] + '&gt;' AS [Name]
							,[User].*
                    FROM [dbo].[User]
					WHERE [Id] >= 0;",
					(collaboratorOptionDto, user) => { collaboratorOptionDto.User = user; return collaboratorOptionDto; });
				var groups = await connection.QueryParentChildAsync<CollaboratorOptionDto, UserDto, int, List<UserDto>>(@"
                    SELECT	[TuteeGroup].[Id]
							,'""' + [Title] + ' Tutor Group""' AS [Name]
							,[User].*
					FROM [dbo].[CourseEnrolment]
					INNER JOIN [dbo].[Tutee]
					ON [CourseEnrolment].[Id] = [Tutee].[CourseEnrolmentId]
					INNER JOIN [dbo].[TuteeGroup]
					ON [Tutee].[TuteeGroupId] = [TuteeGroup].[Id]
					INNER JOIN [dbo].[User]
					ON [CourseEnrolment].[UserId] = [User].Id
					UNION					
					SELECT	[TuteeGroup].[Id]
							,'""' + [Title] + ' Tutor Group""' AS [Name]
							,[User].*
					FROM [dbo].[TuteeGroup]
					INNER JOIN [dbo].[User]
					ON [TuteeGroup].TutorId = [User].Id;",
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
			using(var originalImage = Image.Load<Rgba32>(imageStream)) {
				_logger.LogTrace($"Initialised image from stream");
				// First we want to resize the image so that the smallist side is 150,  passing a 0 will maintain the aspect ratio.
				var size = originalImage.Width == originalImage.Height
                    ? new Size(150, 150)
                    : originalImage.Width > originalImage.Height
                        ? new Size(0, 150)
                        : new Size(150, 0);

                // The crop rectange needs to be adjusted on the x axis if the width>150, so we crop it in the centre.
                var cropRectangle = originalImage.Width > originalImage.Height
                    ? new Rectangle(((150 * originalImage.Width / originalImage.Height)-150)/2, 0, 150, 150)
                    : new Rectangle(0, 0, 150, 150);

				_logger.LogTrace($"Begin resize and crop image");
				
				using (var outputStream = new FileStream(filePath, FileMode.CreateNew)) {
					originalImage.Mutate(x => x
						.Resize(size) // resize to min 150 width or height.
						.Crop(cropRectangle)); // crop to 150x150.
					originalImage.SaveAsJpeg(outputStream); // save to disk.
				}
				_logger.LogTrace($"Finished resize and crop image");
			}
        }
    }    
}