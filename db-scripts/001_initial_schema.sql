-- GitHub Discord Notifier Database Schema
-- Version: 1.0.0
-- Date: 2025-07-03

USE [master];
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GitHubDiscordNotifier')
BEGIN
    CREATE DATABASE [GitHubDiscordNotifier];
END
GO

USE [GitHubDiscordNotifier];
GO

-- Create Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[users] (
        [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [email] NVARCHAR(255) NOT NULL,
        [password_hash] NVARCHAR(255) NOT NULL,
        [github_id] NVARCHAR(255) NULL,
        [github_username] NVARCHAR(255) NULL,
        [avatar_url] NVARCHAR(500) NULL,
        [refresh_token] NVARCHAR(500) NULL,
        [refresh_token_expiry] DATETIME2 NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [updated_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_users] PRIMARY KEY ([id])
    );

    -- Create indexes
    CREATE UNIQUE INDEX [IX_users_email] ON [dbo].[users] ([email]);
    CREATE INDEX [IX_users_github_id] ON [dbo].[users] ([github_id]);
    CREATE INDEX [IX_users_created_at] ON [dbo].[users] ([created_at]);
END
GO

-- Create Systems table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='systems' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[systems] (
        [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [name] NVARCHAR(255) NOT NULL,
        [description] NVARCHAR(1000) NULL,
        [discord_server_id] NVARCHAR(255) NOT NULL,
        [discord_server_name] NVARCHAR(255) NULL,
        [discord_bot_token] NVARCHAR(500) NOT NULL,
        [owner_id] UNIQUEIDENTIFIER NOT NULL,
        [webhook_secret] NVARCHAR(255) NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [updated_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_systems] PRIMARY KEY ([id]),
        CONSTRAINT [FK_systems_owner] FOREIGN KEY ([owner_id]) REFERENCES [dbo].[users] ([id])
    );

    -- Create indexes
    CREATE INDEX [IX_systems_discord_server_id] ON [dbo].[systems] ([discord_server_id]);
    CREATE INDEX [IX_systems_owner_id] ON [dbo].[systems] ([owner_id]);
    CREATE INDEX [IX_systems_created_at] ON [dbo].[systems] ([created_at]);
END
GO

-- Create SystemMembers table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_members' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[system_members] (
        [system_id] UNIQUEIDENTIFIER NOT NULL,
        [user_id] UNIQUEIDENTIFIER NOT NULL,
        [role] NVARCHAR(50) NOT NULL DEFAULT 'member',
        [joined_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [updated_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_system_members] PRIMARY KEY ([system_id], [user_id]),
        CONSTRAINT [FK_system_members_system] FOREIGN KEY ([system_id]) REFERENCES [dbo].[systems] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_system_members_user] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CHK_system_members_role] CHECK ([role] IN ('owner', 'admin', 'member'))
    );

    -- Create indexes
    CREATE INDEX [IX_system_members_user_id] ON [dbo].[system_members] ([user_id]);
    CREATE INDEX [IX_system_members_role] ON [dbo].[system_members] ([role]);
END
GO

-- Create Repositories table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='repositories' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[repositories] (
        [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [system_id] UNIQUEIDENTIFIER NOT NULL,
        [github_repository_id] NVARCHAR(255) NOT NULL,
        [github_repository_name] NVARCHAR(255) NOT NULL,
        [github_repository_url] NVARCHAR(500) NOT NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [added_by_user_id] UNIQUEIDENTIFIER NOT NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [updated_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_repositories] PRIMARY KEY ([id]),
        CONSTRAINT [FK_repositories_system] FOREIGN KEY ([system_id]) REFERENCES [dbo].[systems] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_repositories_added_by] FOREIGN KEY ([added_by_user_id]) REFERENCES [dbo].[users] ([id])
    );

    -- Create indexes
    CREATE INDEX [IX_repositories_system_id] ON [dbo].[repositories] ([system_id]);
    CREATE INDEX [IX_repositories_github_id] ON [dbo].[repositories] ([github_repository_id]);
    CREATE INDEX [IX_repositories_active] ON [dbo].[repositories] ([is_active]);
END
GO

-- Create NotificationChannels table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='notification_channels' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[notification_channels] (
        [id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [system_id] UNIQUEIDENTIFIER NOT NULL,
        [repository_id] UNIQUEIDENTIFIER NOT NULL,
        [discord_channel_id] NVARCHAR(255) NOT NULL,
        [discord_channel_name] NVARCHAR(255) NULL,
        [event_types] NVARCHAR(1000) NOT NULL,
        [notification_template] NVARCHAR(4000) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [updated_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_notification_channels] PRIMARY KEY ([id]),
        CONSTRAINT [FK_notification_channels_system] FOREIGN KEY ([system_id]) REFERENCES [dbo].[systems] ([id]),
        CONSTRAINT [FK_notification_channels_repository] FOREIGN KEY ([repository_id]) REFERENCES [dbo].[repositories] ([id]) ON DELETE CASCADE
    );

    -- Create indexes
    CREATE INDEX [IX_notification_channels_system_id] ON [dbo].[notification_channels] ([system_id]);
    CREATE INDEX [IX_notification_channels_repository_id] ON [dbo].[notification_channels] ([repository_id]);
    CREATE INDEX [IX_notification_channels_discord_channel_id] ON [dbo].[notification_channels] ([discord_channel_id]);
    CREATE INDEX [IX_notification_channels_active] ON [dbo].[notification_channels] ([is_active]);
END
GO

-- Create EventTypes lookup table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='event_types' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[event_types] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] NVARCHAR(100) NOT NULL,
        [display_name] NVARCHAR(100) NOT NULL,
        [description] NVARCHAR(500) NULL,
        [default_enabled] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_event_types] PRIMARY KEY ([id])
    );

    -- Create unique index
    CREATE UNIQUE INDEX [IX_event_types_name] ON [dbo].[event_types] ([name]);
END
GO

-- Insert default event types
IF NOT EXISTS (SELECT * FROM [dbo].[event_types] WHERE [name] = 'push')
BEGIN
    INSERT INTO [dbo].[event_types] ([name], [display_name], [description], [default_enabled])
    VALUES 
        ('push', 'Push', 'Code pushed to repository', 1),
        ('pull_request', 'Pull Request', 'Pull request opened, closed, or merged', 1),
        ('issues', 'Issues', 'Issue opened, closed, or commented', 1),
        ('issue_comment', 'Issue Comment', 'Comment added to issue', 0),
        ('pull_request_review', 'PR Review', 'Pull request reviewed', 1),
        ('pull_request_review_comment', 'PR Review Comment', 'Comment on pull request review', 0),
        ('release', 'Release', 'Release published or updated', 1),
        ('create', 'Create', 'Branch or tag created', 0),
        ('delete', 'Delete', 'Branch or tag deleted', 0),
        ('fork', 'Fork', 'Repository forked', 0),
        ('star', 'Star', 'Repository starred', 0),
        ('watch', 'Watch', 'Repository watched', 0);
END
GO

-- Create update triggers for updated_at columns
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_users_updated_at')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[trg_users_updated_at]
    ON [dbo].[users]
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE [dbo].[users]
        SET [updated_at] = GETUTCDATE()
        FROM [dbo].[users] u
        INNER JOIN inserted i ON u.[id] = i.[id];
    END
    ');
END
GO

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_systems_updated_at')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[trg_systems_updated_at]
    ON [dbo].[systems]
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE [dbo].[systems]
        SET [updated_at] = GETUTCDATE()
        FROM [dbo].[systems] s
        INNER JOIN inserted i ON s.[id] = i.[id];
    END
    ');
END
GO

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_repositories_updated_at')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[trg_repositories_updated_at]
    ON [dbo].[repositories]
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE [dbo].[repositories]
        SET [updated_at] = GETUTCDATE()
        FROM [dbo].[repositories] r
        INNER JOIN inserted i ON r.[id] = i.[id];
    END
    ');
END
GO

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_notification_channels_updated_at')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[trg_notification_channels_updated_at]
    ON [dbo].[notification_channels]
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE [dbo].[notification_channels]
        SET [updated_at] = GETUTCDATE()
        FROM [dbo].[notification_channels] nc
        INNER JOIN inserted i ON nc.[id] = i.[id];
    END
    ');
END
GO

-- Create a procedure to automatically add system owner as admin member
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_create_system_owner_member')
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_create_system_owner_member]
        @system_id UNIQUEIDENTIFIER,
        @owner_id UNIQUEIDENTIFIER
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- Insert owner as admin member if not exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[system_members] WHERE [system_id] = @system_id AND [user_id] = @owner_id)
        BEGIN
            INSERT INTO [dbo].[system_members] ([system_id], [user_id], [role], [joined_at])
            VALUES (@system_id, @owner_id, ''owner'', GETUTCDATE());
        END
    END
    ');
END
GO

-- Create trigger to automatically add system owner as member
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_systems_insert_owner_member')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[trg_systems_insert_owner_member]
    ON [dbo].[systems]
    AFTER INSERT
    AS
    BEGIN
        SET NOCOUNT ON;
        
        INSERT INTO [dbo].[system_members] ([system_id], [user_id], [role], [joined_at])
        SELECT i.[id], i.[owner_id], ''owner'', GETUTCDATE()
        FROM inserted i;
    END
    ');
END
GO

PRINT 'Database schema created successfully!';
PRINT 'Tables created:';
PRINT '  - users';
PRINT '  - systems';
PRINT '  - system_members';
PRINT '  - repositories';
PRINT '  - notification_channels';
PRINT '  - event_types';
PRINT '';
PRINT 'Default event types inserted.';
PRINT 'Update triggers and procedures created.';
PRINT '';
PRINT 'Database is ready for use!';