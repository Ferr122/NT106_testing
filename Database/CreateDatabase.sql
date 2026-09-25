IF DB_ID(N'OnlineChess') IS NULL CREATE DATABASE OnlineChess;
GO
USE OnlineChess;
GO
CREATE TABLE dbo.Users(
 UserId int IDENTITY(1,1) PRIMARY KEY,
 Username nvarchar(32) NOT NULL UNIQUE,
 PasswordHash nvarchar(128) NOT NULL,
 DisplayName nvarchar(64) NOT NULL,
 CreatedAt datetime2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
 LastLogin datetime2 NULL
);
CREATE TABLE dbo.Games(
 GameId uniqueidentifier PRIMARY KEY,
 WhiteUserId int NOT NULL REFERENCES dbo.Users(UserId),
 BlackUserId int NOT NULL REFERENCES dbo.Users(UserId),
 StartedAt datetime2 NOT NULL,
 EndedAt datetime2 NULL,
 Result nvarchar(64) NULL
);
CREATE INDEX IX_Games_White_Start ON dbo.Games(WhiteUserId,StartedAt DESC);
CREATE INDEX IX_Games_Black_Start ON dbo.Games(BlackUserId,StartedAt DESC);
CREATE TABLE dbo.Moves(
 MoveId bigint IDENTITY(1,1) PRIMARY KEY,
 GameId uniqueidentifier NOT NULL REFERENCES dbo.Games(GameId),
 Ply int NOT NULL,
 FromSquare char(2) NOT NULL,
 ToSquare char(2) NOT NULL,
 CreatedAt datetime2 NOT NULL CONSTRAINT DF_Moves_CreatedAt DEFAULT SYSUTCDATETIME(),
 CONSTRAINT UQ_Moves_Game_Ply UNIQUE(GameId,Ply)
);
CREATE TABLE dbo.ChatMessages(
 MessageId bigint IDENTITY(1,1) PRIMARY KEY,
 GameId uniqueidentifier NULL REFERENCES dbo.Games(GameId),
 Username nvarchar(32) NOT NULL,
 Message nvarchar(500) NOT NULL,
 CreatedAt datetime2 NOT NULL CONSTRAINT DF_Chat_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO
