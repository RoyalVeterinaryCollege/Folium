
CREATE TABLE [dbo].[FileProjector.File](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[FileName] [nvarchar](100) NOT NULL,
	[FilePath] [nvarchar](1000) NOT NULL,
	[Type] [nvarchar](1000) NOT NULL,
	[Size] [bigint] NOT NULL,
 CONSTRAINT [PK_FileProjector.File] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[EntryProjector.EntryFile](
	[EntryId] [uniqueidentifier] NOT NULL,
	[FileId] [uniqueidentifier] NOT NULL,
	[OnComment] [bit] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[FileName] [nvarchar](100) NOT NULL,
	[FilePath] [nvarchar](1000) NOT NULL,
	[Type] [nvarchar](1000) NOT NULL,
	[Size] [bigint] NOT NULL,
	[IsAudioVideoEncoded] [bit] NOT NULL,
	[EncodedAudioVideoDirectoryPath] [nvarchar](1000) NULL,
 CONSTRAINT [PK_EntryProjector.EntryFile] PRIMARY KEY CLUSTERED 
(
	[EntryId] ASC,
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[EntryProjector.EntryCommentFile](
	[CommentId] [int] NOT NULL,
	[EntryId] [uniqueidentifier] NOT NULL,
	[FileId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_EntryProjector.EntryCommentFile] PRIMARY KEY CLUSTERED 
(
	[CommentId] ASC,
	[EntryId] ASC,
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE dbo.[EntryProjector.EntryComment] ADD CONSTRAINT
	[PK_EntryProjector.EntryComment] PRIMARY KEY CLUSTERED 
	(
	Id,
	EntryId
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
