CREATE TABLE [dbo].[EntryProjector.SignOffRequest](
	[EntryId] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
	[When] [datetime] NOT NULL,
 CONSTRAINT [PK_EntryProjector.SignOffRequest] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC,
	[UserId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE dbo.[ReportingProjector.EntryEngagement] ADD
	IsSignOffCompatible bit NOT NULL CONSTRAINT [DF_ReportingProjector.EntryEngagement_IsSignOffCompatible] DEFAULT 0,
	SignOffRequestCount int NOT NULL CONSTRAINT [DF_ReportingProjector.EntryEngagement_SignOffRequestCount] DEFAULT 0,
	SignedOff bit NOT NULL CONSTRAINT [DF_ReportingProjector.EntryEngagement_SignedOff] DEFAULT 0
GO

ALTER TABLE dbo.[ReportingProjector.PlacementEngagement] ADD
	EntrySignOffCompatibleCount int NOT NULL CONSTRAINT [DF_ReportingProjector.EntryEngagement_EntrySignOffCompatibleCount] DEFAULT 0,
	EntrySignOffRequestCount int NOT NULL CONSTRAINT [DF_ReportingProjector.PlacementEngagement_EntrySignOffRequestCount] DEFAULT 0,
	EntrySignedOffCount bit NOT NULL CONSTRAINT [DF_ReportingProjector.PlacementEngagement_EntrySignedOffCount] DEFAULT 0
GO

ALTER TABLE dbo.[EntryProjector.Entry] ADD
	IsSignOffCompatible bit NOT NULL CONSTRAINT [DF_EntryProjector.Entry_IsSignOffCompatible] DEFAULT 0,
	SignOffRequested bit NOT NULL CONSTRAINT [DF_EntryProjector.Entry_SignOffRequested] DEFAULT 0,
	SignedOffAt datetime NULL,
	SignedOffBy int NULL,
	SignedOff bit NOT NULL CONSTRAINT [DF_EntryProjector.Entry_SignedOff] DEFAULT 0
GO

ALTER TABLE dbo.[PlacementProjector.Entry] ADD
	TypeId int NULL,
	IsSignOffCompatible bit NOT NULL CONSTRAINT [DF_PlacementProjector.Entry_IsSignOffCompatible] DEFAULT 0,
	SignOffRequested bit NOT NULL CONSTRAINT [DF_PlacementProjector.Entry_SignOffRequested] DEFAULT 0,
	SignedOffAt datetime NULL,
	SignedOffBy int NULL,
	SignedOff bit NOT NULL CONSTRAINT [DF_PlacementProjector.Entry_SignedOff] DEFAULT 0
GO

UPDATE [Entry]
SET TypeId = [EntryType].[Id]
FROM [PlacementProjector.Entry] [Entry]
INNER JOIN [EntryType] 
		ON [Entry].[TypeName] = [EntryType].[Name]
GO

ALTER TABLE dbo.[EntryProjector.EntryComment] ADD
	ForSignOff bit NOT NULL CONSTRAINT [DF_EntryProjector.EntryComment_ForSignOff] DEFAULT 0
GO