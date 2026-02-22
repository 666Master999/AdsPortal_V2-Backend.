BEGIN TRANSACTION;
DROP INDEX [IX_Users_Login] ON [Users];
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Login');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Users] ALTER COLUMN [Login] nvarchar(50) NOT NULL;
CREATE UNIQUE INDEX [IX_Users_Login] ON [Users] ([Login]);

ALTER TABLE [Users] ADD [UserName] nvarchar(50) NULL;


                UPDATE Users 
                SET UserName = Login 
                WHERE UserName IS NULL
            

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UserName');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Users] ALTER COLUMN [UserName] nvarchar(50) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260222142431_AddUserNameAndUpdateMaxLengths', N'10.0.3');

COMMIT;
GO

