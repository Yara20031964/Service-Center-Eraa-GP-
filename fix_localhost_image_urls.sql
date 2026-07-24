/*
    Rewrites uploaded-file URLs that were saved with a localhost host
    (e.g. http://localhost:5000/uploads/xxx or https://localhost:7123/uploads/xxx)
    to the production origin https://khdma.runasp.net/uploads/xxx.

    Why this happened: ImageUrlResolver fell back to the request host, and on
    MonsterASP the app sees requests forwarded from IIS as localhost. Now fixed
    by setting App:PublicBaseUrl; this script repairs rows written before that.

    Strategy: find the '/uploads/' segment in each value and replace everything
    before it with the production base. Port-agnostic, and the WHERE clause only
    matches localhost rows so re-running it is a no-op.

    SAFE TO RUN ONCE. Wrapped in a transaction with a preview + verification.
    Review the "before" counts, then keep COMMIT (or switch to ROLLBACK to test).
*/

SET NOCOUNT ON;

DECLARE @base       nvarchar(200) = N'https://khdma.runasp.net';
DECLARE @needle     nvarchar(50)  = N'/uploads/';
DECLARE @likeHttp   nvarchar(60)  = N'http://localhost%/uploads/%';
DECLARE @likeHttps  nvarchar(60)  = N'https://localhost%/uploads/%';

/* ---------- PREVIEW: how many rows will change ---------- */
SELECT 'AspNetUsers.ProfilePictureUrl' AS [Column], COUNT(*) AS RowsToFix
    FROM AspNetUsers WHERE ProfilePictureUrl LIKE @likeHttp OR ProfilePictureUrl LIKE @likeHttps
UNION ALL SELECT 'Banners.ImageUrl', COUNT(*)
    FROM Banners WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps
UNION ALL SELECT 'ChatMessages.AttachmentUrl', COUNT(*)
    FROM ChatMessages WHERE AttachmentUrl LIKE @likeHttp OR AttachmentUrl LIKE @likeHttps
UNION ALL SELECT 'ProviderCertificateImages.ImageUrl', COUNT(*)
    FROM ProviderCertificateImages WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps
UNION ALL SELECT 'ProviderPortfolioImages.ImageUrl', COUNT(*)
    FROM ProviderPortfolioImages WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps
UNION ALL SELECT 'Services.Image', COUNT(*)
    FROM Services WHERE Image LIKE @likeHttp OR Image LIKE @likeHttps
UNION ALL SELECT 'ServiceImages.ImageUrl', COUNT(*)
    FROM ServiceImages WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps;

BEGIN TRANSACTION;

UPDATE AspNetUsers
   SET ProfilePictureUrl = @base + SUBSTRING(ProfilePictureUrl, CHARINDEX(@needle, ProfilePictureUrl), LEN(ProfilePictureUrl))
 WHERE ProfilePictureUrl LIKE @likeHttp OR ProfilePictureUrl LIKE @likeHttps;

UPDATE Banners
   SET ImageUrl = @base + SUBSTRING(ImageUrl, CHARINDEX(@needle, ImageUrl), LEN(ImageUrl))
 WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps;

UPDATE ChatMessages
   SET AttachmentUrl = @base + SUBSTRING(AttachmentUrl, CHARINDEX(@needle, AttachmentUrl), LEN(AttachmentUrl))
 WHERE AttachmentUrl LIKE @likeHttp OR AttachmentUrl LIKE @likeHttps;

UPDATE ProviderCertificateImages
   SET ImageUrl = @base + SUBSTRING(ImageUrl, CHARINDEX(@needle, ImageUrl), LEN(ImageUrl))
 WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps;

UPDATE ProviderPortfolioImages
   SET ImageUrl = @base + SUBSTRING(ImageUrl, CHARINDEX(@needle, ImageUrl), LEN(ImageUrl))
 WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps;

UPDATE Services
   SET Image = @base + SUBSTRING(Image, CHARINDEX(@needle, Image), LEN(Image))
 WHERE Image LIKE @likeHttp OR Image LIKE @likeHttps;

UPDATE ServiceImages
   SET ImageUrl = @base + SUBSTRING(ImageUrl, CHARINDEX(@needle, ImageUrl), LEN(ImageUrl))
 WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps;

/* ---------- VERIFY: should all be 0 after the update ---------- */
SELECT 'AspNetUsers.ProfilePictureUrl' AS [Column], COUNT(*) AS RemainingLocalhost
    FROM AspNetUsers WHERE ProfilePictureUrl LIKE @likeHttp OR ProfilePictureUrl LIKE @likeHttps
UNION ALL SELECT 'Banners.ImageUrl', COUNT(*)
    FROM Banners WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps
UNION ALL SELECT 'ChatMessages.AttachmentUrl', COUNT(*)
    FROM ChatMessages WHERE AttachmentUrl LIKE @likeHttp OR AttachmentUrl LIKE @likeHttps
UNION ALL SELECT 'ProviderCertificateImages.ImageUrl', COUNT(*)
    FROM ProviderCertificateImages WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps
UNION ALL SELECT 'ProviderPortfolioImages.ImageUrl', COUNT(*)
    FROM ProviderPortfolioImages WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps
UNION ALL SELECT 'Services.Image', COUNT(*)
    FROM Services WHERE Image LIKE @likeHttp OR Image LIKE @likeHttps
UNION ALL SELECT 'ServiceImages.ImageUrl', COUNT(*)
    FROM ServiceImages WHERE ImageUrl LIKE @likeHttp OR ImageUrl LIKE @likeHttps;

-- If the "before" preview looked right and you're happy, keep COMMIT.
-- To dry-run instead, change the next line to ROLLBACK TRANSACTION;
COMMIT TRANSACTION;
