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
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Folium.Api.Services {
    public class SqlDbService : IDbService {
        private readonly string _connectionString;

        public SqlDbService(IOptions<ConnectionStrings> connectionStrings) {
            _connectionString = connectionStrings.Value.SqlConnectionString;
        }

        public DbConnection GetConnection(string connectionString) {
            return new SqlConnection(connectionString);
        }

        public DbConnection GetConnection() {
            return GetConnection(_connectionString);
        }
	}
}