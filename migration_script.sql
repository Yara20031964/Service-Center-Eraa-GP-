IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [ProfilePictureUrl] nvarchar(max) NULL,
        [DateOfBirth] datetime2 NULL,
        [Role] int NOT NULL,
        [Status] int NOT NULL,
        [CreateAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Categories] (
        [id] uniqueidentifier NOT NULL,
        [NameEn] nvarchar(max) NOT NULL,
        [NameAr] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IconUrl] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Admins] (
        [ApplicationUserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Admins] PRIMARY KEY ([ApplicationUserId]),
        CONSTRAINT [FK_Admins_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Customers] (
        [ApplicationUserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([ApplicationUserId]),
        CONSTRAINT [FK_Customers_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Providers] (
        [ApplicationUserId] nvarchar(450) NOT NULL,
        [Rating] float NOT NULL,
        [ReviewCount] int NOT NULL,
        [State] int NOT NULL,
        [AvailabilityStatus] int NOT NULL,
        [ServiceArea] nvarchar(max) NULL,
        [HourlyRate] decimal(18,2) NULL,
        [CurrentLatitude] float NULL,
        [CurrentLongitude] float NULL,
        CONSTRAINT [PK_Providers] PRIMARY KEY ([ApplicationUserId]),
        CONSTRAINT [FK_Providers_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Services] (
        [id] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [NameEn] nvarchar(max) NOT NULL,
        [NameAr] nvarchar(max) NOT NULL,
        [FixedPrice] decimal(18,2) NULL,
        [EstimatedDurationMin] int NULL,
        [EstimatedDurationMax] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([id]),
        CONSTRAINT [FK_Services_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Addresses] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [AddresssLine] nvarchar(max) NOT NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([ApplicationUserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [BookingType] int NOT NULL,
        [ScheduledTime] datetime2 NULL,
        [Address] nvarchar(max) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Status] int NOT NULL,
        [TotalPrice] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CancelReason] nvarchar(max) NULL,
        [CreateAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([ApplicationUserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([ApplicationUserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [ProviderServices] (
        [Id] uniqueidentifier NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreateAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProviderServices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderServices_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([ApplicationUserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProviderServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [MessageType] nvarchar(max) NOT NULL,
        [MessageText] nvarchar(max) NULL,
        [AttachmentUrl] nvarchar(max) NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatMessages_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2 NULL,
        [SentAt] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notifications_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CommissionAmount] decimal(18,2) NOT NULL,
        [ProviderEarning] decimal(18,2) NOT NULL,
        [PaymentStatus] int NOT NULL,
        [TransactionReference] nvarchar(max) NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NULL,
        [PunctualityRating] int NULL,
        [WorkQualityRating] int NULL,
        [CleanlinesRating] int NULL,
        [CreateAt] datetime2 NOT NULL,
        [IsHidden] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([ApplicationUserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([ApplicationUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Addresses_CustomerId] ON [Addresses] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Bookings_CustomerId] ON [Bookings] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Bookings_ProviderId] ON [Bookings] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Bookings_ServiceId] ON [Bookings] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_BookingId] ON [ChatMessages] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Notifications_BookingId] ON [Notifications] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_ProviderServices_ProviderId] ON [ProviderServices] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_ProviderServices_ServiceId] ON [ProviderServices] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_BookingId] ON [Reviews] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Reviews_ProviderId] ON [Reviews] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    CREATE INDEX [IX_Services_CategoryId] ON [Services] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327075524_InitialAzureRevert'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260327075524_InitialAzureRevert', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329011419_AddCommissionSettings'
)
BEGIN
    CREATE TABLE [CommissionSettings] (
        [Id] int NOT NULL IDENTITY,
        [Rate] decimal(18,2) NOT NULL,
        [LastUpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CommissionSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329011419_AddCommissionSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260329011419_AddCommissionSettings', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    ALTER TABLE [Services] ADD [Description] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    ALTER TABLE [Services] ADD [Image] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    ALTER TABLE [Services] ADD [Rating] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    ALTER TABLE [Services] ADD [ReviewCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    CREATE TABLE [CustomerFavorites] (
        [CustomerId] nvarchar(450) NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomerFavorites] PRIMARY KEY ([CustomerId], [ServiceId]),
        CONSTRAINT [FK_CustomerFavorites_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([ApplicationUserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomerFavorites_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [Expires] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    CREATE TABLE [ServiceImages] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ServiceImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceImages_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [CommissionSettings] SET [LastUpdatedAt] = ''2026-03-29T00:00:00.0000000Z''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    CREATE INDEX [IX_CustomerFavorites_ServiceId] ON [CustomerFavorites] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    CREATE INDEX [IX_ServiceImages_ServiceId] ON [ServiceImages] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125322_AddServiceDetailsAndFavorites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260420125322_AddServiceDetailsAndFavorites', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125543_AddServiceDetailsAndFavorites_v2'
)
BEGIN
    EXEC(N'UPDATE [CommissionSettings] SET [LastUpdatedAt] = ''2026-03-29T00:00:00.0000000Z''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420125543_AddServiceDetailsAndFavorites_v2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260420125543_AddServiceDetailsAndFavorites_v2', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    ALTER TABLE [Addresses] DROP CONSTRAINT [FK_Addresses_Customers_CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    EXEC sp_rename N'[Addresses].[CustomerId]', N'UserId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    EXEC sp_rename N'[Addresses].[IX_Addresses_CustomerId]', N'IX_Addresses_UserId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    ALTER TABLE [Providers] ADD [Description] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    ALTER TABLE [Providers] ADD [ExperienceYears] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    ALTER TABLE [Providers] ADD [JobTitle] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    ALTER TABLE [Providers] ADD [NumberOfJobsDone] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    CREATE TABLE [CustomerFavoriteProviders] (
        [CustomerId] nvarchar(450) NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomerFavoriteProviders] PRIMARY KEY ([CustomerId], [ProviderId]),
        CONSTRAINT [FK_CustomerFavoriteProviders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([ApplicationUserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomerFavoriteProviders_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([ApplicationUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    CREATE TABLE [ProviderCertificateImages] (
        [Id] uniqueidentifier NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ProviderCertificateImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderCertificateImages_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([ApplicationUserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    CREATE TABLE [ProviderPortfolioImages] (
        [Id] uniqueidentifier NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ProviderPortfolioImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderPortfolioImages_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([ApplicationUserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    CREATE INDEX [IX_CustomerFavoriteProviders_ProviderId] ON [CustomerFavoriteProviders] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    CREATE INDEX [IX_ProviderCertificateImages_ProviderId] ON [ProviderCertificateImages] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    CREATE INDEX [IX_ProviderPortfolioImages_ProviderId] ON [ProviderPortfolioImages] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    ALTER TABLE [Addresses] ADD CONSTRAINT [FK_Addresses_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420151005_AddProviderFieldsAndFavoritesAndImages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260420151005_AddProviderFieldsAndFavoritesAndImages', N'9.0.13');
END;

COMMIT;
GO

