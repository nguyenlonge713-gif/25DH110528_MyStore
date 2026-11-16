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