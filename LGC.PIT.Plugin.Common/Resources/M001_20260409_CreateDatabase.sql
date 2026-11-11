USE [DATABASE_NAME]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[Configuration]') IS NULL
BEGIN

	CREATE TABLE [dbo].[Configuration](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [varchar](50) NOT NULL,
		[Value] [varchar](MAX) NOT NULL,
		[CreatedBy] [varchar](50) NOT NULL,
		[CreatedOn] [datetime] NOT NULL,
		[ModifiedBy] [varchar](50) NOT NULL,
		[ModifiedOn] [datetime] NOT NULL,
    CONSTRAINT [PK_Configuration] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
	WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]

	ALTER TABLE [dbo].[Configuration] ADD CONSTRAINT [DF_Configuration_ModifiedBy]  DEFAULT (suser_sname()) FOR [ModifiedBy]
	ALTER TABLE [dbo].[Configuration] ADD CONSTRAINT [DF_Configuration_ModifiedOn]  DEFAULT (getdate()) FOR [ModifiedOn]

	CREATE UNIQUE NONCLUSTERED INDEX [IX_Configuration_Key] ON [dbo].[Configuration]([Key] ASC) ON [PRIMARY]

    INSERT INTO [dbo].[Configuration]([Key], [Value], [CreatedBy], [CreatedOn], [ModifiedBy], [ModifiedOn])
    VALUES ('FirstTimeRun', 'True', suser_sname(), getdate(), suser_sname(), getdate())

END
