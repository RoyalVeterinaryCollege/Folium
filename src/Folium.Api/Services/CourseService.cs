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
using Folium.Api.Models;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System;

namespace Folium.Api.Services {
    public interface ICourseService {
        Task<IReadOnlyList<Course>> GetCoursesAsync();        
        Task<IEnumerable<Course>> GetCoursesAdministratedByUserAsync(User user);
        IEnumerable<CourseEnrolment> GetCourseEnrolments(User user, IDbTransaction transaction = null);
        IEnumerable<CourseEnrolment> GetActiveCourseEnrolments(User user, IDbTransaction transaction = null);
        IEnumerable<CourseEnrolment> GetCourseEnrolments(int courseId, int enrolmentYear, int courseYear, IDbTransaction transaction = null);
    }
    public class CourseService : ICourseService {
        private readonly IDbService _dbService;
        public CourseService(IDbService dbService){
            _dbService = dbService;
        }

        public async Task<IReadOnlyList<Course>> GetCoursesAsync(){			
            using (var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var courses = await connection.QueryAsync<Course>(@"
                    SELECT *
                    FROM [dbo].[Course]");
                return courses.ToList();
            }
        }
        
        public IEnumerable<CourseEnrolment> GetCourseEnrolments(User user, IDbTransaction transaction = null) {
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
                return (connection.Query<CourseEnrolment>(@"
					    SELECT *, [Course].Title AS CourseTitle
					    FROM [dbo].[CourseEnrolment]
                        INNER JOIN [dbo].[Course]
                            ON [CourseEnrolment].[CourseId] = [Course].[Id]
					    WHERE [UserId] = @UserId
					",
                    new {
                        UserId = user.Id
                    }, transaction)).ToList();
            }
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }

        public IEnumerable<CourseEnrolment> GetCourseEnrolments(int courseId, int enrolmentYear, int courseYear, IDbTransaction transaction = null) {
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
                return (connection.Query<CourseEnrolment>(@"
					    SELECT *, [Course].Title AS CourseTitle
					    FROM [dbo].[CourseEnrolment]
                        INNER JOIN [dbo].[Course]
                            ON [CourseEnrolment].[CourseId] = [Course].[Id]
					    WHERE [Course].[Id] = @CourseId
                        AND [EnrolmentYear] = @EnrolmentYear
                        AND [CourseYear] = @CourseYear
					",
                    new {
                        CourseId = courseId,
                        EnrolmentYear = enrolmentYear,
                        CourseYear = courseYear
                    }, transaction)).ToList();
            }
            finally {
                if (connection != null && closedConnection && connection is IDisposable) {
                    connection.Dispose();
                }
            }
        }

        public async Task<IEnumerable<Course>> GetCoursesAdministratedByUserAsync(User user) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                return (connection.Query<Course>(@"
					    SELECT *
					    FROM [dbo].[Course]
                        INNER JOIN [dbo].[CourseAdministrator]
                            ON [Course].[Id] = [CourseAdministrator].[CourseId]
					    WHERE [UserId] = @UserId
					",
                    new {
                        UserId = user.Id
                    }));
            }
        }

        public IEnumerable<CourseEnrolment> GetActiveCourseEnrolments(User user, IDbTransaction transaction = null) {
            return GetCourseEnrolments(user, transaction).Where(c => c.Active);
        }
    }
}