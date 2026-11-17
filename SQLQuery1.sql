SELECT Username, UserRole
FROM dbo.[User]
WHERE Username = N'adminmystore';


USE MyStore;
GO

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    NUMERIC_PRECISION,
    NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Product'
ORDER BY ORDINAL_POSITION;



SELECT
    CategoryID,
    CategoryName
FROM dbo.Category;

USE MyStore;
GO

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Product'
ORDER BY ORDINAL_POSITION;


SELECT
    Username,
    LEN(Password) AS PasswordLength
FROM dbo.[User]
WHERE Username = 'huanhoahong';

SELECT
    Username,
    N'[' + Password + N']' AS StoredPassword,
    LEN(Password) AS ActualLength,
    DATALENGTH(Password) AS StorageLength
FROM dbo.[User]
WHERE Username = N'huanhoahong';



USE MyStore;
GO

IF COL_LENGTH('dbo.Product', 'StockQuantity') IS NULL
BEGIN
    ALTER TABLE dbo.Product
    ADD StockQuantity INT NOT NULL
        CONSTRAINT DF_Product_StockQuantity DEFAULT (0);
END
GO

SELECT ProductID, ProductName, StockQuantity
FROM dbo.Product;

USE MyStore;
GO

UPDATE dbo.Product
SET StockQuantity = 10;
GO

-- Kiểm tra kết quả
SELECT ProductID, ProductName, StockQuantity
FROM dbo.Product
ORDER BY ProductID;
GO


USE MyStore;
GO

SELECT
    u.Username,
    u.UserRole,
    c.CustomerID,
    c.CustomerName,
    c.Username AS CustomerUsername
FROM dbo.[User] u
LEFT JOIN dbo.Customer c
    ON u.Username = c.Username
ORDER BY u.Username;
GO




USE MyStore;
GO

SELECT
    t.name AS TableName,
    SUM(p.rows) AS TotalRecords
FROM sys.tables t
INNER JOIN sys.partitions p
    ON t.object_id = p.object_id
WHERE p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;
GO