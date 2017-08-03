
BEGIN TRANSACTION
GO
CREATE TABLE [dbo].[EntryProjector.SharedWith](
	[EntryId] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_EntryProjector.SharedWith] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
COMMIT

BEGIN TRANSACTION
GO

CREATE TABLE [dbo].[ActivityProjector.Activity](
	[UserId] [int] NOT NULL,
	[Type] [int] NOT NULL,
	[When] [datetime] NOT NULL,
	[Title] [nvarchar](1000) NULL,
	[Link] [nvarchar](1000) NULL
) ON [PRIMARY]

GO
COMMIT

BEGIN TRANSACTION
GO

CREATE TABLE [dbo].[ActivityProjector.Entry](
	[EntryId] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_ActivityProjector.Entry] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
COMMIT

BEGIN TRANSACTION
GO

CREATE TABLE [dbo].[EntryProjector.EntryComment](
	[Id] [int] NOT NULL,
	[EntryId] [uniqueidentifier] NOT NULL,
	[Comment] [nvarchar](max) NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL
) ON [PRIMARY]

GO
COMMIT

BEGIN TRANSACTION
GO

ALTER TABLE dbo.[EntryProjector.Entry] ADD
	[Shared] bit NULL
GO
ALTER TABLE dbo.[EntryProjector.Entry] SET (LOCK_ESCALATION = TABLE)
GO
COMMIT

BEGIN TRANSACTION
GO

UPDATE dbo.[EntryProjector.Entry]
SET [Shared] = 0

GO
COMMIT

BEGIN TRANSACTION
GO
ALTER TABLE dbo.[EntryProjector.Entry] ALTER COLUMN [Shared] bit NOT NULL
GO
COMMIT

BEGIN TRANSACTION
GO
EXECUTE sp_rename N'dbo.UserActivity', N'UserSignInActivity', 'OBJECT' 
GO
ALTER TABLE dbo.UserSignInActivity
	DROP COLUMN Id, Type, Title, Link
GO
ALTER TABLE dbo.UserSignInActivity SET (LOCK_ESCALATION = TABLE)
GO
COMMIT

BEGIN TRANSACTION
GO
CREATE TABLE [dbo].[PlacementProjector.EntrySharedWith](
	[EntryId] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_PlacementProjector.EntrySharedWith] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
COMMIT

BEGIN TRANSACTION
GO

ALTER TABLE dbo.[PlacementProjector.Entry] ADD
	[Shared] bit NULL
GO
ALTER TABLE dbo.[PlacementProjector.Entry] SET (LOCK_ESCALATION = TABLE)
GO
COMMIT

BEGIN TRANSACTION
GO

UPDATE dbo.[PlacementProjector.Entry]
SET [Shared] = 0

GO
COMMIT

BEGIN TRANSACTION
GO
ALTER TABLE dbo.[PlacementProjector.Entry] ALTER COLUMN [Shared] bit NOT NULL
GO
COMMIT

BEGIN TRANSACTION
GO

CREATE TABLE [dbo].[ActivityProjector.EmailNotification](
	[Id] [uniqueidentifier] NOT NULL,
	[To] [nvarchar](1000) NOT NULL,
	[Subject] [nvarchar](max) NOT NULL,
	[HtmlBody] [nvarchar](max) NOT NULL,
	[ActionLink] [nvarchar](1000) NULL,
	[ActionTitle] [nvarchar](1000) NULL,
	[When] [datetime] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_ActivityProjector.EmailNotification] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
COMMIT