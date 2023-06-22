﻿CREATE TABLE [users].[Users]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Login] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR (255) NOT NULL,
    [Password] NVARCHAR(255) NOT NULL,
    [IsActive] BIT NOT NULL,
    [FirstName] NVARCHAR(50) NOT NULL,
    [LastName] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR (255) NOT NULL,
    CONSTRAINT [PK_users_Users_Id] PRIMARY KEY ([Id] ASC)
)
GO