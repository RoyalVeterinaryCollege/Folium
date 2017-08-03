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

namespace Folium.Api.Services {
    public interface ISkillService {
        Task<IReadOnlyList<string>> ImportSkillsAsync(int courseId, string description, Stream fileStream);
        Task<IReadOnlyList<SkillSet>> GetSkillSetsAsync(int userId);
	    Task<SkillSet> GetSkillSetAsync(int skillSetId);

		Task<IReadOnlyList<Skill>> GetSkillsAsync(int skillSetId);
	    Task<Skill> GetSkillAsync(int skillSetId, int skillId);

    }
    public class SkillService : ISkillService {
        private readonly IDbService _dbService;
		public SkillService(
			IDbService dbService) {
            _dbService = dbService;
		}
        public async Task<IReadOnlyList<string>> ImportSkillsAsync(int courseId, string description, Stream fileStream){
            var failureMessages = new List<string>();
            // Get the course and its skill sets and self assessment scales.
	        var categoryRegExMatcher = new Regex(@"(.*?)(?<!\/)(?:\/(?!\/)|$)");
            IList<SelfAssessmentScale> selfAssessmentScales = new List<SelfAssessmentScale>();
            IList<Taxonomy> taxonomies = new List<Taxonomy>();
            IList<TaxonomyTerm> taxonomyTerms = new List<TaxonomyTerm>();
            using (var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var course = await connection.QueryFirstAsync<Course>(@" 
                    SELECT *
                    FROM [dbo].[Course]
                    WHERE Id = @Id", 
	                new {Id = courseId});

                // Validate the course exists.
                if(course == null) {
                    failureMessages.Add("The course does not exist.");
                    return failureMessages;
                }

                // Do this in a transaction.
                using (var transaction = connection.BeginTransaction()){
                    var csvReader = new CsvReader(new StreamReader(fileStream));
                    csvReader.Configuration.HasHeaderRecord = false;
                    
                    // Flags to set what is currently being read in the file.
                    var processingSelfAssessmentScales = false;
                    var processingSkills = false;
                    var newSkills = new List<ImportedSkill>();

                    // Get the latest SkillsSet for the course.
                    var latestSkillSetVersion = (await connection.QueryAsync<int?>(@"
                        SELECT MAX(Version) 
                        FROM [dbo].[SkillSet]
						INNER JOIN [dbo].[CourseSkillSet]
								ON [SkillSet].[Id] = [CourseSkillSet].[SkillSetId]
                        WHERE [CourseId] = @CourseId",
                        new {
                            CourseId = courseId
                        },
                        transaction))
                    .Single();
                    var skillSetVersion = latestSkillSetVersion.HasValue ? latestSkillSetVersion.Value+1 : 1;

                    // Create the SkillsSet.
                    var skillSetId = (await connection.QueryAsync<int>(@"
                        INSERT INTO [dbo].[SkillSet]
                            ([Version]
                            ,[Name]
                            ,[Description]
                            ,[CreatedAt]
                            ,[CreatedBy]
                            ,[LastUpdatedAt]
                            ,[LastUpdatedBy]
                            ,[SelfAssignable])
                        VALUES (@Version
                                ,@Name
                                ,@Description
                                ,@CourseId
                                ,@When
                                ,-1 --System User
                                ,@When
                                ,-1 --System User
                                ,1);
                        SELECT CAST(SCOPE_IDENTITY() AS INT)",
                        new {
                            Version = skillSetVersion,
                            Name = $"{course.Title} Skills (v{skillSetVersion})",
                            Description = description,
                            CourseId = courseId,
							When = DateTime.UtcNow
                        },
                        transaction))
                    .Single();


					// Create the Course/SkillsSet relationship.
					await connection.ExecuteAsync(@"
                        INSERT INTO [dbo].[CourseSkillSet]
                            ([CourseId]
                            ,[SkillSetId])
                        VALUES (@CourseId
                                ,@SkillSetId)",
						new {
							CourseId = courseId,
							SkillSetId = skillSetId
						},
						transaction);

					// Read each line of the file. 
					while (csvReader.Read()) {
                        // Set Processing self assessment scales?
                        processingSkills = processingSkills || csvReader.IsSkillsListMarkerRow();
                        processingSelfAssessmentScales = !processingSkills && (processingSelfAssessmentScales || csvReader.IsSelfAssessmentScalesMarkerRow());
                        
                        // Process Self Assessment scales.
                        if(processingSelfAssessmentScales){
                            if(csvReader.IsSelfAssessmentScalesMarkerRow() || csvReader.IsSelfAssessmentScalesHeaderRow()) continue; // Move to the next row if we are on the marker or header.
                            // Check we don't already have a scale with the same name.
                            var scaleName = csvReader.GetField<string>(0);
                            
                            if(string.IsNullOrWhiteSpace(scaleName)) continue; // Empty row.

                            if(selfAssessmentScales.Any(s => s.Name.Equals(scaleName, StringComparison.OrdinalIgnoreCase))) {
                                failureMessages.Add($"There is an existing Self Assessment scale with the name '{scaleName}', names must be unique.");
                            } else {
                                // Create the scale.
                                var selfAssessmentScaleId = (await connection.QueryAsync<int>(@"
                                    INSERT INTO [dbo].[SelfAssessmentScale]
                                        ([Name])
                                    VALUES (@Name);
                                    SELECT CAST(SCOPE_IDENTITY() AS INT)",
                                    new {Name = scaleName},
                                    transaction))
                                .Single();
                                selfAssessmentScales.Add(new SelfAssessmentScale{Id = selfAssessmentScaleId, Name = scaleName});

                                // Calculate the equal score for each level.
                                var levels = csvReader.CurrentRecord.Skip(1).ToList();
                                // Trim off any empty levels at the end.
                                if(string.IsNullOrEmpty(levels.Last())){
                                    var lastLevelIndex = levels.FindLastIndex(l => !string.IsNullOrEmpty(l));
                                    levels.RemoveRange(lastLevelIndex + 1, levels.Count - lastLevelIndex - 1);
                                }
                                var score = 100 / (levels.Count - 1); // We discount a level, as the first is set to 0.
                                // Do we need to spread any remainder?
                                var scoreRemainder = 100 - (score * (levels.Count - 1));

                                // Add the levels.                                
                                await connection.ExecuteAsync(@"
                                    INSERT INTO [dbo].[SelfAssessmentLevel]
                                        ([Name], [Score], [SelfAssessmentScaleId])
                                    VALUES (@Name, @Score, @SelfAssessmentScaleId);",
                                    levels.Select((level, index) => 
                                            new { 
                                                Name = level, 
                                                Score = (score * (index)) + (scoreRemainder > 0 ? (index < scoreRemainder ? index : scoreRemainder) : 0), // Add to the score if needed to balance the total.
                                                SelfAssessmentScaleId = selfAssessmentScaleId
                                            }),
                                    transaction
                                );
                            }
                        }
                        
                        // Process Skills.
                        if(processingSkills){
                            if(csvReader.IsSkillsListMarkerRow() || csvReader.IsSkillsHeaderRow()) continue; // Move to the next row if we are on the marker or header.
                            var skill = new ImportedSkill {
                                Name = csvReader.GetField<string>(0), 
                                ParentSkill = csvReader.GetField<string>(1),
                                CanSelfAssess = csvReader.GetField<string>(2),
                                SelfAssessmentScale = csvReader.GetField<string>(3),
                                CanSelfCount = csvReader.GetField<string>(4),
                                Categories = csvReader.CurrentRecord.Skip(5).Where(r => !string.IsNullOrWhiteSpace(r)).ToList()
                            };
                            // Need to find the parent?
                            if(!string.IsNullOrWhiteSpace(skill.ParentSkill)){
                                var parentSkill = newSkills.FirstOrDefault(s => ((string)s.Name).Equals(skill.ParentSkill, StringComparison.OrdinalIgnoreCase));
                                if(parentSkill == null) {
                                    // Parent skill does not exist.
                                    failureMessages.Add($"The parent skill '{skill.ParentSkill}' does not exist in the file, make sure it appears before its children and is spelt the same.");
                                    continue;
                                }
                                skill.ParentSkillId = parentSkill.Id;
                            }
                            // Need to set the self assessment scale?
                            if(!string.IsNullOrWhiteSpace(skill.SelfAssessmentScale)){
                                var selfAssessmentScale = selfAssessmentScales.FirstOrDefault(s => ((string)s.Name).Equals(skill.SelfAssessmentScale, StringComparison.OrdinalIgnoreCase));
                                if(selfAssessmentScale == null) {
                                    // Self assessment scale skill does not exist.
                                    failureMessages.Add($"The Self Assessment Scale '{skill.SelfAssessmentScale}' does not exist.");
                                    continue;
                                }
                                skill.SelfAssessmentScaleId = selfAssessmentScale.Id;
                            }

                            // Create the skill.
                            skill.Id = (await connection.QueryAsync<int>(@"
                                INSERT INTO [dbo].[Skill]
                                    ([Name]
                                    ,[CanSelfAssess]
                                    ,[ParentSkillId]
                                    ,[SkillSetId]
                                    ,[CanSelfCount]
                                    ,[SelfAssessmentScaleId]
                                    ,[Removed]
                                    ,[CreatedAt]
                                    ,[CreatedBy]
                                    ,[LastUpdatedAt]
                                    ,[LastUpdatedBy])
                                VALUES (@Name
                                        ,@CanSelfAssess
                                        ,@ParentSkillId
                                        ,@SkillSetId
                                        ,@CanSelfCount
                                        ,@SelfAssessmentScaleId
                                        ,0
                                        ,@When
                                        ,-1 --System User
                                        ,@When
                                        ,-1 --System User
                                        );
                                SELECT CAST(SCOPE_IDENTITY() AS INT)",
                                new {
                                    Name = skill.Name,
                                    CanSelfAssess = skill.CanSelfAssess.Equals("yes", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                                    ParentSkillId = skill.ParentSkillId,
                                    SkillSetId = skillSetId,
                                    CanSelfCount = skill.CanSelfCount.Equals("yes", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                                    SelfAssessmentScaleId = skill.SelfAssessmentScaleId,
									When = DateTime.UtcNow
                                },
                                transaction))
                            .Single();

                            // Process the taxonomy terms.                            
                            foreach(var category in skill.Categories){
                                // Each skill entry can have multiple categories attached to it.
                                // Each category is in the form [TaxonomyName]/[TaxonomyTerm]/[TaxonomyTerm]
                                // So terms can be nested, i.e. a hierarchy 
                                var skillCategories = categoryRegExMatcher.Split(category).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                                // Check the taxonomy exists, otherwise create it.
                                var taxonomy = taxonomies.FirstOrDefault(t => t.Name.Equals(skillCategories[0], StringComparison.OrdinalIgnoreCase));
                                if(taxonomy == null){
                                    // Create the taxonomy.
                                    taxonomy = new Taxonomy {
                                        Name = skillCategories[0]
                                    };
                                    taxonomy.Id = (await connection.QueryAsync<int>(@"
                                        INSERT INTO [dbo].[Taxonomy]
                                            ([Name]
                                            ,[Type]
                                            ,[SkillSetId]
                                            ,[Removed]
                                            ,[CreatedAt]
                                            ,[CreatedBy]
                                            ,[LastUpdatedAt]
                                            ,[LastUpdatedBy])
                                        VALUES (@Name
                                                ,0
                                                ,@SkillSetId
                                                ,0
                                                ,@When
                                                ,-1 --System User
                                                ,@When
                                                ,-1 --System User
                                                );
                                        SELECT CAST(SCOPE_IDENTITY() AS INT)",
                                        new {
                                            Name = taxonomy.Name,
                                            SkillSetId = skillSetId,
											When = DateTime.UtcNow
                                        },
                                        transaction))
                                    .Single();
                                    // Add the new taxonomy into the list.
                                    taxonomies.Add(taxonomy);
                                }
                                // Check each of the terms exist and then associate them with the skill.
                                TaxonomyTerm parentTaxonomyTerm = null;
                                int index = 1;
                                foreach(var skillTaxonomyTerm in skillCategories.Skip(1)){
                                    var skillName = skillTaxonomyTerm.Replace("//", "/"); // Replace any escaped values.
                                    var taxonomyTerm = taxonomyTerms.FirstOrDefault(t=> t.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase) && t.TaxonomyId == taxonomy.Id && (parentTaxonomyTerm == null || t.ParentTaxonomyTermId == parentTaxonomyTerm.Id));
                                    if(taxonomyTerm == null){
                                        // Create the taxonomy term.
                                        taxonomyTerm = new TaxonomyTerm {
                                            Name = skillName,
                                            TaxonomyId = taxonomy.Id,
                                            ParentTaxonomyTermId = parentTaxonomyTerm == null ? null : (int?)parentTaxonomyTerm.Id
                                        };
                                        taxonomyTerm.Id = (await connection.QueryAsync<int>(@"
                                            INSERT INTO [dbo].[TaxonomyTerm]
                                                ([Name]
                                                ,[TaxonomyId]
                                                ,[ParentTaxonomyTermId]
                                                ,[Removed]
                                                ,[CreatedAt]
                                                ,[CreatedBy]
                                                ,[LastUpdatedAt]
                                                ,[LastUpdatedBy])
                                            VALUES (@Name
                                                    ,@TaxonomyId
                                                    ,@ParentTaxonomyTermId
                                                    ,0
                                                    ,@When
                                                    ,-1 --System User
                                                    ,@When
                                                    ,-1 --System User
                                                    );
                                            SELECT CAST(SCOPE_IDENTITY() AS INT)",
                                            new {
                                                Name = taxonomyTerm.Name,
                                                TaxonomyId = taxonomyTerm.TaxonomyId,
                                                ParentTaxonomyTermId = taxonomyTerm.ParentTaxonomyTermId,
												When = DateTime.UtcNow
                                            },
                                            transaction))
                                        .Single();
                                        // Add the new taxonomy term into the list.
                                        taxonomyTerms.Add(taxonomyTerm);
                                    }
                                    parentTaxonomyTerm = taxonomyTerm;
                                    index++;
                                    // If we at the end then associate this term with the skill.
                                    if(index == skillCategories.Count){
                                        await connection.ExecuteAsync(@"
                                            IF (SELECT COUNT(*) 
                                                FROM [dbo].[SkillTaxonomyTerm] 
                                                WHERE SkillId = @SkillId
                                                    AND TaxonomyTermId = @TaxonomyTermId) = 0
                                                INSERT INTO [dbo].[SkillTaxonomyTerm]
                                                    ([SkillId]
                                                    ,[TaxonomyTermId]
                                                    ,[CreatedAt]
                                                    ,[CreatedBy])
                                                VALUES (@SkillId
                                                        ,@TaxonomyTermId
                                                        ,@When
                                                        ,-1 --System User
                                                        );",
                                            new { SkillId = skill.Id, 
                                                  TaxonomyTermId = taxonomyTerm.Id,
												  When = DateTime.UtcNow
                                            },
                                            transaction
                                        );
                                    }
                                }
                            }

                            newSkills.Add(skill);
                        }                        
                    }
                    // Any failure messages?
                    if(failureMessages.Any()){
                        transaction.Rollback();
                    } else {
                        transaction.Commit();
                    }
                }                
            }
            return failureMessages;
        }

        public async Task<IReadOnlyList<SkillSet>> GetSkillSetsAsync(int userId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var skillSets = await connection.QueryAsync<SkillSet>(@" 
                    SELECT [SkillSet].*
                    FROM [dbo].[SkillSet]
					LEFT JOIN [dbo].[UserSkillSet]
							ON [SkillSet].[Id] = [UserSkillSet].[SkillSetId]
							AND [UserSkillSet].[UserId] = @UserId 
					WHERE ([SkillSet].[SelfAssignable] = 1 OR [UserSkillSet].[SkillSetId] IS NOT NULL)", 
                    new {UserId = userId});
                return skillSets.ToList();
            }
		}
		public async Task<SkillSet> GetSkillSetAsync(int skillSetId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var skillSet = await connection.QueryFirstOrDefaultAsync<SkillSet>(@" 
                    SELECT *
                    FROM [dbo].[SkillSet]
                    WHERE (Id = @SkillSetId)",
					new { SkillSetId = skillSetId });
				return skillSet;
			}
		}

		public async Task<IReadOnlyList<Skill>> GetSkillsAsync(int skillSetId) {
            using(var connection = _dbService.GetConnection()){
                await connection.OpenAsync();
                var skills = await connection.QueryAsync<Skill>(@" 
                    SELECT *
                    FROM [dbo].[Skill]
                    WHERE Skill.SkillSetId = @SkillSetId", 
                    new {
	                    SkillSetId = skillSetId
                    });
                return skills.ToList();
            }
		}

		public async Task<Skill> GetSkillAsync(int skillSetId, int skillId) {
			using (var connection = _dbService.GetConnection()) {
				await connection.OpenAsync();
				var skill = await connection.QueryFirstOrDefaultAsync<Skill>(@" 
                    SELECT *
                    FROM [dbo].[Skill]
                    WHERE Skill.SkillSetId = @SkillSetId
					AND Skill.Id = @SkillId",
					new {
						SkillSetId = skillSetId,
						SkillId = skillId
					});
				return skill;
			}
		}
	}

    class ImportedSkill{
        public int Id { get; set; } 
        public string Name { get; set; } 
        public string CanSelfAssess { get; set; }
        public string ParentSkill { get; set; } 
        public int? ParentSkillId { get; set; } 
        public string CanSelfCount { get; set; } 
        public string SelfAssessmentScale { get; set; }
        public int? SelfAssessmentScaleId { get; set; }
        public List<string> Categories { get; set; }
    }
    public static class CsvReaderExtensions {
        public static bool IsSkillsListMarkerRow(this CsvReader csvReader){
            return csvReader.GetField<string>(0).Equals("SKILLS LIST", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsSkillsHeaderRow(this CsvReader csvReader){
            return csvReader.GetField<string>(0).Equals("Skill", StringComparison.OrdinalIgnoreCase) && csvReader.GetField<string>(1).Equals("Parent Skill", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsSelfAssessmentScalesMarkerRow(this CsvReader csvReader){
            return csvReader.GetField<string>(0).Equals("SELF ASSESSMENT SCALES", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsSelfAssessmentScalesHeaderRow(this CsvReader csvReader){
            return csvReader.GetField<string>(0).Equals("Name", StringComparison.OrdinalIgnoreCase) && csvReader.GetField<string>(1).Equals("Levels", StringComparison.OrdinalIgnoreCase);
        }
    }
}