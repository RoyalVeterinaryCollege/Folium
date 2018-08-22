EXECUTE sp_rename N'dbo.CourseEnrolment.Removed', N'Active', 'COLUMN' 
GO

UPDATE [dbo].[CourseEnrolment]
SET [Active] = ~[Active]
GO

CREATE TABLE [dbo].[SkillSetEntryType]
	(
	SkillSetId int NOT NULL,
	EntryTypeId int NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.SkillSetEntryType ADD CONSTRAINT
	PK_SkillSetEntryType PRIMARY KEY CLUSTERED 
	(
	SkillSetId,
	EntryTypeId
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO

INSERT INTO [dbo].[SkillSetEntryType] 
	(SkillSetId,
	EntryTypeId)
SELECT SkillSetId, Id
FROM [dbo].[EntryType]
GO

ALTER TABLE dbo.EntryType
	DROP CONSTRAINT FK_EntryType_SkillSet
GO

ALTER TABLE dbo.EntryType
	DROP COLUMN SkillSetId
GO

ALTER TABLE dbo.[PlacementProjector.Placement] ADD
	Type nvarchar(50) NULL
GO

CREATE TABLE dbo.[ReportingProjector.SelfAssessmentEngagement]
	(
	[UserId] int NOT NULL,
	[SkillId] int NOT NULL,
	[SkillSetId] int NOT NULL,
	[Score] int NOT NULL,
	[Date] date NOT NULL,
	[When] date NOT NULL
	)  ON [PRIMARY]
GO

CREATE TABLE dbo.[ReportingProjector.EntryEngagement]
	(
	[UserId] int NOT NULL,
	[EntryId] uniqueidentifier NOT NULL,
	[Where] nvarchar(1050) NOT NULL,
	[EntryTypeId] int NULL,
	[When] datetime NOT NULL,
	[SharedCount] int NOT NULL,
	[SharedWithTutorCount] int NOT NULL,
	[CommentCount] int NOT NULL,
	)  ON [PRIMARY]
GO

CREATE TABLE dbo.[ReportingProjector.PlacementEngagement]
	(
	[UserId] int NOT NULL,
	[PlacementId] uniqueidentifier NOT NULL,
	[Start] date NOT NULL,
	[End] date NOT NULL,
	[FullyQualifiedTitle] nvarchar(1050) NOT NULL,
	[Type] nvarchar(50) NULL,
	[EntryCount] int NOT NULL,
	[SharedEntryCount] int NOT NULL,
	[SharedEntryWithTutorCount] int NOT NULL
	)  ON [PRIMARY]
GO

ALTER TABLE dbo.[EntryProjector.Entry]
ALTER COLUMN [Where] nvarchar(1050) NOT NULL;
GO

ALTER TABLE dbo.[PlacementProjector.Entry]
ALTER COLUMN [Where] nvarchar(1050) NOT NULL;
GO

ALTER TABLE dbo.[WhereProjector.Entry]
ALTER COLUMN [Where] nvarchar(1050) NOT NULL;
GO

ALTER TABLE dbo.[WhereProjector.Placement]
ALTER COLUMN [Where] nvarchar(1050) NOT NULL;
GO

ALTER TABLE dbo.[WhereProjector.Where]
ALTER COLUMN [Name] nvarchar(1050) NOT NULL;
GO

CREATE TABLE [dbo].[CourseAdministrator]
	(
	CourseId int NOT NULL,
	UserId int NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE [dbo].[CourseAdministrator] ADD CONSTRAINT
	PK_CourseAdministrator PRIMARY KEY CLUSTERED 
	(
	CourseId,
	UserId
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO