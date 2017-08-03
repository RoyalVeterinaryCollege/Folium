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
        Task<IReadOnlyList<TaxonomyTerm>> GetHierarchyTermsAsync(int skillSetId);
        Task<IReadOnlyList<SkillTaxonomyTerm>> GetHierarchySkillTermsAsync(int skillSetId);
        Task<IReadOnlyList<Taxonomy>> GetSkillFilterTaxonomysAsync(int skillSetId);
        Task<IReadOnlyList<TaxonomyTerm>> GetSkillFilterTermsAsync(int skillSetId);
        Task<IReadOnlyList<SkillTaxonomyTerm>> GetSkillFilterSkillTermsAsync(int skillSetId);
    }
    public class TaxonomyService : ITaxonomyService {
        private readonly IDbService _dbService;
        public TaxonomyService(IDbService dbService){
            _dbService = dbService;
        }

        public async Task<IReadOnlyList<TaxonomyTerm>> GetHierarchyTermsAsync(int skillSetId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<TaxonomyTerm>(@" 
                                                    SELECT TaxonomyTerm.*
                                                    FROM [dbo].[TaxonomyTerm]
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId
                                                        AND Taxonomy.Type = @TaxonomyType;", 
                                                    new {
                                                        SkillSetId = skillSetId, 
                                                        TaxonomyType = (int)TaxonomyType.SkillsHierarchies
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<SkillTaxonomyTerm>> GetHierarchySkillTermsAsync(int skillSetId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<SkillTaxonomyTerm>(@" 
                                                    SELECT SkillTaxonomyTerm.*
                                                    FROM [dbo].[SkillTaxonomyTerm]
                                                    INNER JOIN [dbo].[Skill]
                                                            ON SkillTaxonomyTerm.SkillId = Skill.Id
                                                    INNER JOIN [dbo].[TaxonomyTerm]
                                                            ON SkillTaxonomyTerm.TaxonomyTermId = TaxonomyTerm.Id          
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId
                                                    WHERE Skill.SkillSetId = @SkillSetId
                                                        AND Taxonomy.Type = @TaxonomyType;",
                                                    new {
                                                        SkillSetId = skillSetId, 
                                                        TaxonomyType = (int)TaxonomyType.SkillsHierarchies
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<Taxonomy>> GetSkillFilterTaxonomysAsync(int skillSetId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<Taxonomy>(@" 
                                                    SELECT Taxonomy.*
                                                    FROM [dbo].[Taxonomy]    
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId
                                                        AND Taxonomy.Type = @TaxonomyType;", 
                                                    new {
                                                        SkillSetId = skillSetId, 
                                                        TaxonomyType = (int)TaxonomyType.SkillsFilters
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<TaxonomyTerm>> GetSkillFilterTermsAsync(int skillSetId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<TaxonomyTerm>(@" 
                                                    SELECT TaxonomyTerm.*
                                                    FROM [dbo].[TaxonomyTerm]
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId
                                                    WHERE Taxonomy.SkillSetId = @SkillSetId
                                                        AND Taxonomy.Type = @TaxonomyType;",  
                                                    new {
                                                        SkillSetId = skillSetId, 
                                                        TaxonomyType = (int)TaxonomyType.SkillsFilters
                                                    });
                return terms.ToList();
            }
        }

        public async Task<IReadOnlyList<SkillTaxonomyTerm>> GetSkillFilterSkillTermsAsync(int skillSetId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var terms = await connection.QueryAsync<SkillTaxonomyTerm>(@" 
                                                    SELECT SkillTaxonomyTerm.*
                                                    FROM [dbo].[SkillTaxonomyTerm]
                                                    INNER JOIN [dbo].[Skill]
                                                            ON SkillTaxonomyTerm.SkillId = Skill.Id
                                                    INNER JOIN [dbo].[TaxonomyTerm]
                                                            ON SkillTaxonomyTerm.TaxonomyTermId = TaxonomyTerm.Id        
                                                    INNER JOIN [dbo].[Taxonomy]
                                                            ON Taxonomy.Id = TaxonomyTerm.TaxonomyId    
                                                    WHERE Skill.SkillSetId = @SkillSetId
                                                        AND Taxonomy.Type = @TaxonomyType;",  
                                                    new {
                                                        SkillSetId = skillSetId, 
                                                        TaxonomyType = (int)TaxonomyType.SkillsFilters
                                                    });
                return terms.ToList();
            }
        }
        
    }
}