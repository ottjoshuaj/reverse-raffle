
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 11/07/2017 21:58:49
-- Generated from EDMX file: D:\Work\YSU\ReverseRaffler\ReverseRaffler\Data\RevRaffleModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [RevRaffle];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[RegisteredApiUsers]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RegisteredApiUsers];
GO
IF OBJECT_ID(N'[dbo].[TicketHolders]', 'U') IS NOT NULL
    DROP TABLE [dbo].[TicketHolders];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'RegisteredApiUsers'
CREATE TABLE [dbo].[RegisteredApiUsers] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [RegisteredUser] nvarchar(250)  NOT NULL,
    [APIKey] uniqueidentifier  NULL
);
GO

-- Creating table 'TicketHolders'
CREATE TABLE [dbo].[TicketHolders] (
    [TicketNumber] int  NOT NULL,
    [TicketHolder1] nvarchar(350)  NULL,
    [TicketHolder2] nvarchar(350)  NULL,
    [TicketHolder3] nvarchar(350)  NULL,
    [MarkedAsNotWinner] bit  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'RegisteredApiUsers'
ALTER TABLE [dbo].[RegisteredApiUsers]
ADD CONSTRAINT [PK_RegisteredApiUsers]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [TicketNumber] in table 'TicketHolders'
ALTER TABLE [dbo].[TicketHolders]
ADD CONSTRAINT [PK_TicketHolders]
    PRIMARY KEY CLUSTERED ([TicketNumber] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------