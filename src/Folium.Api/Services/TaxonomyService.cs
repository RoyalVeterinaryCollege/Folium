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

namespace Folium.Api.Services {
    public interface ITaxonomyService {
        Task<Taxonomy> GetTaxonomyAsync(int taxonomyId);
        Task<IReadOnlyList<Taxonomy>> GetSkillGroupingTaxonomysAsync(int skillSetId);
        Task<IReadOnlyList<Taxonomy>> GetSkillFilterTaxonomysAsync(int skillSetId);
        Task<IReadOnlyList<TaxonomyTerm>> GetTermsAsync(int taxonomyId);
        Task<IReadOnlyList<SkillTaxonomyTerm>> GetSkillTermsAsync(int? taxonomyId = null, int? taxonomyTermId = null);
        Task<IReadOnlyList<TaxonomyTerm>> GetAllTaxonomyTermFiltersAsync(int skillSetId);
        Task<IReadOnlyList<SkillTaxonomyTerm>> GetAllSkillTermFiltersAsync(int skillSetId);
    }
    public class TaxonomyService : ITaxonomyService {
        private readonly IDbService _dbService;
        public TaxonomyService(IDbService dbService){
            _dbService = dbService;
        }

        public async Task<Taxonomy> GetTaxonomyAsync(int taxonomyId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var taxonomy = await connection.QueryFirstAsync<Taxonomy>(@" 
                                                    SELECT *
                                                    FROM [dbo].[Taxonomy]
                                                    WHERE Id = @TaxonomyId;",
                                                    new {
                                                        TaxonomyId = taxonomyId
                                                    });
                return taxonomy;
            }
        }

        public async Task<IReadOnlyList<Taxonomy>> GetSkillGroupingTaxonomysAsync(int skillSetId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<Taxonomy>(@" 
                                                    SELECT Taxonomy.*
                                                    FROM [dbo].[Taxonomy]
                                                    INNER JOIN [dbo].[TaxonomySkillGrouping]
                                                            ON [Taxonomy].Id = [TaxonomySkillGrouping].TaxonomyId
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId;",
                                                    new {
                                                        SkillSetId = skillSetId
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<Taxonomy>> GetSkillFilterTaxonomysAsync(int skillSetId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<Taxonomy>(@" 
                                                    SELECT Taxonomy.*
                                                    FROM [dbo].[Taxonomy]
                                                    INNER JOIN [dbo].[TaxonomySkillFilter]
                                                            ON [Taxonomy].Id = [TaxonomySkillFilter].TaxonomyId
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId;",
                                                    new {
                                                        SkillSetId = skillSetId
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<TaxonomyTerm>> GetTermsAsync(int taxonomyId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<TaxonomyTerm>(@" 
                                                    SELECT TaxonomyTerm.*
                                                    FROM [dbo].[TaxonomyTerm]
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId
                                                    WHERE Taxonomy.Id = @TaxonomyId;", 
                                                    new {
                                                        TaxonomyId = taxonomyId,
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<SkillTaxonomyTerm>> GetSkillTermsAsync(int? taxonomyId = null, int? taxonomyTermId = null) {
            if (taxonomyId == null && taxonomyTermId == null) return new List<SkillTaxonomyTerm> { };

            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var sql = taxonomyId.HasValue
                    ? @" 
                        SELECT SkillTaxonomyTerm.*
                        FROM [dbo].[SkillTaxonomyTerm]
                        INNER JOIN [dbo].[TaxonomyTerm]
                                ON SkillTaxonomyTerm.TaxonomyTermId = TaxonomyTerm.Id          
                        INNER JOIN [dbo].[Taxonomy]
                                ON Taxonomy.Id = TaxonomyTerm.TaxonomyId
                        WHERE Taxonomy.Id = @TaxonomyId;"
                    : @" 
                        SELECT SkillTaxonomyTerm.*
                        FROM [dbo].[SkillTaxonomyTerm]
                        INNER JOIN [dbo].[TaxonomyTerm]
                                ON SkillTaxonomyTerm.TaxonomyTermId = TaxonomyTerm.Id   
                        WHERE TaxonomyTerm.Id = @TaxonomyTermId;";

                var terms = await connection.QueryAsync<SkillTaxonomyTerm>(
                    sql,
                    new {
                        TaxonomyTermId = taxonomyTermId,
                        TaxonomyId = taxonomyId
                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<TaxonomyTerm>> GetAllTaxonomyTermFiltersAsync(int skillSetId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<TaxonomyTerm>(@" 
                                                    SELECT TaxonomyTerm.*
                                                    FROM [dbo].[TaxonomyTerm]
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId
                                                    INNER JOIN [dbo].[TaxonomySkillFilter]
                                                            ON [Taxonomy].Id = [TaxonomySkillFilter].TaxonomyId
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId;",
                                                    new {
                                                        SkillSetId = skillSetId
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<SkillTaxonomyTerm>> GetAllSkillTermFiltersAsync(int skillSetId) {
            using (var connection = _dbService.GetConnection()) {
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<SkillTaxonomyTerm>(@" 
                                                    SELECT SkillTaxonomyTerm.*
                                                    FROM [dbo].[SkillTaxonomyTerm]
                                                    INNER JOIN [dbo].[TaxonomyTerm]
                                                            ON SkillTaxonomyTerm.TaxonomyTermId = TaxonomyTerm.Id        
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId  
                                                    INNER JOIN [dbo].[TaxonomySkillFilter]
                                                            ON [Taxonomy].Id = [TaxonomySkillFilter].TaxonomyId  
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId;",
                                                    new {
                                                        SkillSetId = skillSetId
                                                    });
                return terms.ToList();
            }
        }
    }
}