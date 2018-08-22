CREATE TABLE [dbo].[TaxonomySkillGrouping](
	[TaxonomyId] [int] NOT NULL,
 CONSTRAINT [PK_TaxonomySkillGrouping] PRIMARY KEY CLUSTERED 
(
	[TaxonomyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[TaxonomySkillGrouping]  WITH CHECK ADD  CONSTRAINT [FK_TaxonomySkillGrouping_Taxonomy] FOREIGN KEY([TaxonomyId])
REFERENCES [dbo].[Taxonomy] ([Id])
GO

ALTER TABLE [dbo].[TaxonomySkillGrouping] CHECK CONSTRAINT [FK_TaxonomySkillGrouping_Taxonomy]
GO

INSERT INTO [dbo].[TaxonomySkillGrouping]
SELECT Taxonomy.Id
FROM [dbo].[Taxonomy]
WHERE [Type] = 1
GO

CREATE TABLE [dbo].[TaxonomySkillFilter](
	[TaxonomyId] [int] NOT NULL,
 CONSTRAINT [PK_TaxonomySkillFilter] PRIMARY KEY CLUSTERED 
(
	[TaxonomyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[TaxonomySkillFilter]  WITH CHECK ADD  CONSTRAINT [FK_TaxonomySkillFilter_Taxonomy] FOREIGN KEY([TaxonomyId])
REFERENCES [dbo].[Taxonomy] ([Id])
GO

ALTER TABLE [dbo].[TaxonomySkillFilter] CHECK CONSTRAINT [FK_TaxonomySkillFilter_Taxonomy]
GO

INSERT INTO [dbo].[TaxonomySkillFilter]
SELECT Taxonomy.Id
FROM [dbo].[Taxonomy]
WHERE [Type] = 2
GO

ALTER TABLE dbo.Taxonomy
	DROP COLUMN [Type]
GO

ALTER TABLE dbo.[EntryProjector.Entry] ADD
	SkillGroupingId int NULL
GO
