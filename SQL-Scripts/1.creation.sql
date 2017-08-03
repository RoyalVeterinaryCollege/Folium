
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Course](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Reference] [nvarchar](50) NOT NULL,
	[Title] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_Course] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CourseEnrolment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[CourseId] [int] NOT NULL,
	[CourseYear] [int] NOT NULL,
	[EnrolmentYear] [int] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[Removed] [bit] NOT NULL,
 CONSTRAINT [PK_TuteeEnrolment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CourseSkillSet](
	[CourseId] [int] NOT NULL,
	[SkillSetId] [int] NOT NULL,
 CONSTRAINT [PK_CourseSkillSet] PRIMARY KEY CLUSTERED 
(
	[CourseId] ASC,
	[SkillSetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntryProjector.Entry](
	[Id] [uniqueidentifier] NOT NULL,
	[SkillSetId] [int] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Title] [nvarchar](1000) NOT NULL,
	[UserId] [int] NOT NULL,
	[Where] [nvarchar](200) NOT NULL,
	[When] [datetime] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[TypeId] [int] NULL,
	[TypeName] [nvarchar](1000) NULL,
 CONSTRAINT [PK_EntryProjector.Entry] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntryProjector.SelfAssessment](
	[EntryId] [uniqueidentifier] NOT NULL,
	[SkillId] [int] NOT NULL,
	[SelfAssessmentLevelId] [int] NOT NULL,
	[Score] [int] NOT NULL,
 CONSTRAINT [PK_EntrySelfAssessment] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC,
	[SkillId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntryType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SkillSetId] [int] NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[Retired] [bit] NOT NULL,
	[Template] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_EntryType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlacementProjector.Entry](
	[Id] [uniqueidentifier] NOT NULL,
	[PlacementId] [uniqueidentifier] NOT NULL,
	[SkillSetId] [int] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Title] [nvarchar](1000) NOT NULL,
	[UserId] [int] NOT NULL,
	[Where] [nvarchar](200) NOT NULL,
	[When] [datetime] NOT NULL,
	[TypeName] [nvarchar](1000) NULL,
 CONSTRAINT [PK_PlacementProjector.Entry] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlacementProjector.Placement](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
	[Title] [nvarchar](1000) NOT NULL,
	[FullyQualifiedTitle] [nvarchar](1050) NOT NULL,
	[Start] [date] NOT NULL,
	[End] [date] NOT NULL,
	[Reference] [nvarchar](50) NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Placement] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SelfAssessmentLevel](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[Score] [int] NOT NULL,
	[SelfAssessmentScaleId] [int] NOT NULL,
 CONSTRAINT [PK_SelfAssessmentLevel] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SelfAssessmentProjector.SelfAssessment](
	[UserId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[SkillSetId] [int] NOT NULL,
	[SelfAssessmentLevelId] [int] NOT NULL,
	[Score] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_SelfAssessment] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[SkillId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SelfAssessmentScale](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_SelfAssessmentScale] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Skill](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[CanSelfAssess] [bit] NOT NULL,
	[ParentSkillId] [int] NULL,
	[SkillSetId] [int] NOT NULL,
	[CanSelfCount] [bit] NOT NULL,
	[SelfAssessmentScaleId] [int] NULL,
	[DraftOf] [int] NULL,
	[Description] [nvarchar](max) NULL,
	[Removed] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
 CONSTRAINT [PK_Skill] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SkillSet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Version] [int] NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[DraftOf] [int] NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
	[SelfAssignable] [bit] NOT NULL,
 CONSTRAINT [PK_SkillSet] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SkillTaxonomyTerm](
	[SkillId] [int] NOT NULL,
	[TaxonomyTermId] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
 CONSTRAINT [PK_SkillTaxonomyTerm] PRIMARY KEY CLUSTERED 
(
	[SkillId] ASC,
	[TaxonomyTermId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Taxonomy](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[Type] [int] NOT NULL,
	[SkillSetId] [int] NOT NULL,
	[DraftOf] [int] NULL,
	[Removed] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
 CONSTRAINT [PK_Taxonomy] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaxonomyTerm](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaxonomyId] [int] NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[ParentTaxonomyTermId] [int] NULL,
	[DraftOf] [int] NULL,
	[Description] [nvarchar](max) NULL,
	[Removed] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
 CONSTRAINT [PK_TaxonomyTerm] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tutee](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TuteeGroupId] [int] NOT NULL,
	[CourseEnrolmentId] [int] NOT NULL,
	[Removed] [bit] NOT NULL,
 CONSTRAINT [PK_Tutee] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TuteeGroup](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TutorId] [int] NOT NULL,
	[CourseId] [int] NOT NULL,
	[Title] [nvarchar](1000) NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[LastUpdatedBy] [int] NOT NULL,
	[LastUpdatedAt] [datetime] NOT NULL,
	[Removed] [bit] NOT NULL,
 CONSTRAINT [PK_TuteeGroup_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[SkillsBundlesTaxonomyId] [int] NULL,
	[EntryTagsTaxonomyId] [int] NULL,
	[HasProfilePic] [bit] NOT NULL,
	[ProfilePicVersion] [int] NOT NULL,
	[FirstName] [nvarchar](1000) NULL,
	[LastName] [nvarchar](1000) NULL,
	[LastSignIn] [datetime] NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserActivity](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Type] [int] NOT NULL,
	[When] [datetime] NOT NULL,
	[Title] [nvarchar](1000) NULL,
	[Link] [nvarchar](1000) NULL
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserSkillSet](
	[UserId] [int] NOT NULL,
	[SkillSetId] [int] NOT NULL,
 CONSTRAINT [PK_UserSkillSet] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[SkillSetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WhereProjector.Entry](
	[EntryId] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
	[Where] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_WhereProjector.Entry] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WhereProjector.Placement](
	[PlacementId] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
	[Where] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_WhereProjector.Placement] PRIMARY KEY CLUSTERED 
(
	[PlacementId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WhereProjector.Where](
	[UserId] [int] NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[UsageCount] [int] NOT NULL,
 CONSTRAINT [PK_WhereProjector.Where] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_HasProfilePic]  DEFAULT ((0)) FOR [HasProfilePic]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_ProfilePicVersion]  DEFAULT ((0)) FOR [ProfilePicVersion]
GO
ALTER TABLE [dbo].[CourseEnrolment]  WITH CHECK ADD  CONSTRAINT [FK_CourseEnrolment_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CourseEnrolment] CHECK CONSTRAINT [FK_CourseEnrolment_User]
GO
ALTER TABLE [dbo].[CourseEnrolment]  WITH CHECK ADD  CONSTRAINT [FK_CourseEnrolment_User1] FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CourseEnrolment] CHECK CONSTRAINT [FK_CourseEnrolment_User1]
GO
ALTER TABLE [dbo].[CourseEnrolment]  WITH CHECK ADD  CONSTRAINT [FK_CourseEnrolment_User2] FOREIGN KEY([LastUpdatedBy])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CourseEnrolment] CHECK CONSTRAINT [FK_CourseEnrolment_User2]
GO
ALTER TABLE [dbo].[CourseSkillSet]  WITH CHECK ADD  CONSTRAINT [FK_CourseSkillSet_Course] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Course] ([Id])
GO
ALTER TABLE [dbo].[CourseSkillSet] CHECK CONSTRAINT [FK_CourseSkillSet_Course]
GO
ALTER TABLE [dbo].[CourseSkillSet]  WITH CHECK ADD  CONSTRAINT [FK_CourseSkillSet_SkillSet] FOREIGN KEY([SkillSetId])
REFERENCES [dbo].[SkillSet] ([Id])
GO
ALTER TABLE [dbo].[CourseSkillSet] CHECK CONSTRAINT [FK_CourseSkillSet_SkillSet]
GO
ALTER TABLE [dbo].[EntryType]  WITH CHECK ADD  CONSTRAINT [FK_EntryType_SkillSet] FOREIGN KEY([SkillSetId])
REFERENCES [dbo].[SkillSet] ([Id])
GO
ALTER TABLE [dbo].[EntryType] CHECK CONSTRAINT [FK_EntryType_SkillSet]
GO
ALTER TABLE [dbo].[SelfAssessmentLevel]  WITH CHECK ADD  CONSTRAINT [FK_SelfAssessmentLevel_SelfAssessmentScale] FOREIGN KEY([SelfAssessmentScaleId])
REFERENCES [dbo].[SelfAssessmentScale] ([Id])
GO
ALTER TABLE [dbo].[SelfAssessmentLevel] CHECK CONSTRAINT [FK_SelfAssessmentLevel_SelfAssessmentScale]
GO
ALTER TABLE [dbo].[Skill]  WITH CHECK ADD  CONSTRAINT [FK_ParentSkill_Skill] FOREIGN KEY([ParentSkillId])
REFERENCES [dbo].[Skill] ([Id])
GO
ALTER TABLE [dbo].[Skill] CHECK CONSTRAINT [FK_ParentSkill_Skill]
GO
ALTER TABLE [dbo].[Skill]  WITH CHECK ADD  CONSTRAINT [FK_Skill_SelfAssessmentScale] FOREIGN KEY([SelfAssessmentScaleId])
REFERENCES [dbo].[SelfAssessmentScale] ([Id])
GO
ALTER TABLE [dbo].[Skill] CHECK CONSTRAINT [FK_Skill_SelfAssessmentScale]
GO
ALTER TABLE [dbo].[Skill]  WITH CHECK ADD  CONSTRAINT [FK_Skill_Skill] FOREIGN KEY([DraftOf])
REFERENCES [dbo].[Skill] ([Id])
GO
ALTER TABLE [dbo].[Skill] CHECK CONSTRAINT [FK_Skill_Skill]
GO
ALTER TABLE [dbo].[Skill]  WITH CHECK ADD  CONSTRAINT [FK_Skill_SkillSet] FOREIGN KEY([SkillSetId])
REFERENCES [dbo].[SkillSet] ([Id])
GO
ALTER TABLE [dbo].[Skill] CHECK CONSTRAINT [FK_Skill_SkillSet]
GO
ALTER TABLE [dbo].[SkillSet]  WITH CHECK ADD  CONSTRAINT [FK_SkillSet_SkillSet] FOREIGN KEY([DraftOf])
REFERENCES [dbo].[SkillSet] ([Id])
GO
ALTER TABLE [dbo].[SkillSet] CHECK CONSTRAINT [FK_SkillSet_SkillSet]
GO
ALTER TABLE [dbo].[SkillTaxonomyTerm]  WITH CHECK ADD  CONSTRAINT [FK_SkillTaxonomyTerm_Skill] FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skill] ([Id])
GO
ALTER TABLE [dbo].[SkillTaxonomyTerm] CHECK CONSTRAINT [FK_SkillTaxonomyTerm_Skill]
GO
ALTER TABLE [dbo].[SkillTaxonomyTerm]  WITH CHECK ADD  CONSTRAINT [FK_SkillTaxonomyTerm_TaxonomyTerm] FOREIGN KEY([TaxonomyTermId])
REFERENCES [dbo].[TaxonomyTerm] ([Id])
GO
ALTER TABLE [dbo].[SkillTaxonomyTerm] CHECK CONSTRAINT [FK_SkillTaxonomyTerm_TaxonomyTerm]
GO
ALTER TABLE [dbo].[Taxonomy]  WITH CHECK ADD  CONSTRAINT [FK_Taxonomy_SkillSet] FOREIGN KEY([SkillSetId])
REFERENCES [dbo].[SkillSet] ([Id])
GO
ALTER TABLE [dbo].[Taxonomy] CHECK CONSTRAINT [FK_Taxonomy_SkillSet]
GO
ALTER TABLE [dbo].[Taxonomy]  WITH CHECK ADD  CONSTRAINT [FK_Taxonomy_Taxonomy] FOREIGN KEY([DraftOf])
REFERENCES [dbo].[Taxonomy] ([Id])
GO
ALTER TABLE [dbo].[Taxonomy] CHECK CONSTRAINT [FK_Taxonomy_Taxonomy]
GO
ALTER TABLE [dbo].[TaxonomyTerm]  WITH CHECK ADD  CONSTRAINT [FK_ParentTaxonomyTerm_TaxonomyTerm] FOREIGN KEY([ParentTaxonomyTermId])
REFERENCES [dbo].[TaxonomyTerm] ([Id])
GO
ALTER TABLE [dbo].[TaxonomyTerm] CHECK CONSTRAINT [FK_ParentTaxonomyTerm_TaxonomyTerm]
GO
ALTER TABLE [dbo].[TaxonomyTerm]  WITH CHECK ADD  CONSTRAINT [FK_TaxonomyTerm_Taxonomy] FOREIGN KEY([TaxonomyId])
REFERENCES [dbo].[Taxonomy] ([Id])
GO
ALTER TABLE [dbo].[TaxonomyTerm] CHECK CONSTRAINT [FK_TaxonomyTerm_Taxonomy]
GO
ALTER TABLE [dbo].[TaxonomyTerm]  WITH CHECK ADD  CONSTRAINT [FK_TaxonomyTerm_TaxonomyTerm] FOREIGN KEY([DraftOf])
REFERENCES [dbo].[TaxonomyTerm] ([Id])
GO
ALTER TABLE [dbo].[TaxonomyTerm] CHECK CONSTRAINT [FK_TaxonomyTerm_TaxonomyTerm]
GO
ALTER TABLE [dbo].[Tutee]  WITH CHECK ADD  CONSTRAINT [FK_Tutee_CourseEnrolment] FOREIGN KEY([CourseEnrolmentId])
REFERENCES [dbo].[CourseEnrolment] ([Id])
GO
ALTER TABLE [dbo].[Tutee] CHECK CONSTRAINT [FK_Tutee_CourseEnrolment]
GO
ALTER TABLE [dbo].[Tutee]  WITH CHECK ADD  CONSTRAINT [FK_Tutee_TuteeGroup] FOREIGN KEY([TuteeGroupId])
REFERENCES [dbo].[TuteeGroup] ([Id])
GO
ALTER TABLE [dbo].[Tutee] CHECK CONSTRAINT [FK_Tutee_TuteeGroup]
GO
ALTER TABLE [dbo].[TuteeGroup]  WITH CHECK ADD  CONSTRAINT [FK_TuteeGroup_Course] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Course] ([Id])
GO
ALTER TABLE [dbo].[TuteeGroup] CHECK CONSTRAINT [FK_TuteeGroup_Course]
GO
ALTER TABLE [dbo].[TuteeGroup]  WITH CHECK ADD  CONSTRAINT [FK_TuteeGroup_User] FOREIGN KEY([TutorId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[TuteeGroup] CHECK CONSTRAINT [FK_TuteeGroup_User]
GO
ALTER TABLE [dbo].[TuteeGroup]  WITH CHECK ADD  CONSTRAINT [FK_TuteeGroup_User1] FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[TuteeGroup] CHECK CONSTRAINT [FK_TuteeGroup_User1]
GO
ALTER TABLE [dbo].[TuteeGroup]  WITH CHECK ADD  CONSTRAINT [FK_TuteeGroup_User2] FOREIGN KEY([LastUpdatedBy])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[TuteeGroup] CHECK CONSTRAINT [FK_TuteeGroup_User2]
GO
ALTER TABLE [dbo].[User]  WITH CHECK ADD  CONSTRAINT [FK_User_Taxonomy] FOREIGN KEY([SkillsBundlesTaxonomyId])
REFERENCES [dbo].[Taxonomy] ([Id])
GO
ALTER TABLE [dbo].[User] CHECK CONSTRAINT [FK_User_Taxonomy]
GO
ALTER TABLE [dbo].[UserSkillSet]  WITH CHECK ADD  CONSTRAINT [FK_UserSkillSet_SkillSet] FOREIGN KEY([SkillSetId])
REFERENCES [dbo].[SkillSet] ([Id])
GO
ALTER TABLE [dbo].[UserSkillSet] CHECK CONSTRAINT [FK_UserSkillSet_SkillSet]
GO
ALTER TABLE [dbo].[UserSkillSet]  WITH CHECK ADD  CONSTRAINT [FK_UserSkillSet_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[UserSkillSet] CHECK CONSTRAINT [FK_UserSkillSet_User]
GO

-- Insert system user.
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
