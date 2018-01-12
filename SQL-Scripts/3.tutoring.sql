SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ActivityProjector.ActivitySummary](
	[UserId] [int] NOT NULL,
	[TotalEntries] [int] NOT NULL,
	[TotalSelfAssessments] [int] NOT NULL,
	[TotalPlacements] [int] NOT NULL,
 CONSTRAINT [PK_ActivityProjector.ActivitySummary] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO