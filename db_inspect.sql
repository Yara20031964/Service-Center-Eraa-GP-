/* ===========================================================================
   KHDMA — data inspection (SELECT * for every table)
   Run against the shared db48682 database after the app has started once
   (which applies pending migrations). Connect in SSMS / Azure Data Studio to:
       Server:   db48682.public.databaseasp.net
       Login:    db48682   Password: 8Eq%Z@9m3o?T   (SQL auth)
   or from the CLI:
       sqlcmd -S db48682.public.databaseasp.net -U db48682 -P "8Eq%Z@9m3o?T" -d db48682 -i db_inspect.sql
   (For a local LocalDB instead, change the USE line to your local DB name.)

   Enum codes (stored as int):
     UserRole            0 Customer   1 Admin      2 Provider
     UserStatus          0 Active     1 Suspended  2 Banned
     ProviderState       0 Pending    1 Active     2 Suspended  3 Banned
     AvailabilityStatus  0 Online     1 Offline    2 Busy
     BookingType         0 Immediate  1 Scheduled
     BookingStatus       0 Pending 1 Dispatching 2 Accepted 3 EnRoute 4 Arrived
                         5 InProgress 6 Completed 7 Cancelled 8 NoProviderFound 9 Failed
     PaymentStatus       0 Pending    1 Paid       2 Failed   3 Refunded
     PayoutStatus        0 Requested  1 Approved   2 Paid     3 Rejected
   =========================================================================== */

USE db48682;
GO

/* ---- Identity ------------------------------------------------------------ */
SELECT * FROM AspNetUsers;
SELECT * FROM AspNetRoles;

SELECT u.Email, u.FullName, r.Name AS Role
FROM   AspNetUserRoles ur
       JOIN AspNetUsers u ON u.Id = ur.UserId
       JOIN AspNetRoles r ON r.Id = ur.RoleId;

SELECT ucl.*, u.Email
FROM   AspNetUserClaims ucl JOIN AspNetUsers u ON u.Id = ucl.UserId;

SELECT ul.*, u.Email
FROM   AspNetUserLogins ul JOIN AspNetUsers u ON u.Id = ul.UserId;

SELECT ut.*, u.Email
FROM   AspNetUserTokens ut JOIN AspNetUsers u ON u.Id = ut.UserId;

SELECT rc.*, r.Name AS Role
FROM   AspNetRoleClaims rc JOIN AspNetRoles r ON r.Id = rc.RoleId;

/* ---- People (joined to AspNetUsers so you see the real person) ----------- */
SELECT a.*, u.Email, u.FullName, u.PhoneNumber, u.Status, u.EmailConfirmed, u.CreateAt
FROM   Admins a JOIN AspNetUsers u ON u.Id = a.ApplicationUserId;

SELECT c.*, u.Email, u.FullName, u.PhoneNumber, u.Status, u.EmailConfirmed, u.CreateAt
FROM   Customers c JOIN AspNetUsers u ON u.Id = c.ApplicationUserId;

SELECT u.Email, u.FullName, u.PhoneNumber, u.Status, p.*
FROM   Providers p JOIN AspNetUsers u ON u.Id = p.ApplicationUserId;

/* ---- Catalogue ----------------------------------------------------------- */
SELECT * FROM Categories;

SELECT s.*, c.NameEn AS CategoryEn, c.NameAr AS CategoryAr
FROM   Services s JOIN Categories c ON c.id = s.CategoryId;

SELECT si.*, s.NameEn AS Service
FROM   ServiceImages si JOIN Services s ON s.id = si.ServiceId;

SELECT ps.*, u.Email AS ProviderEmail, u.FullName AS Provider, s.NameEn AS Service
FROM   ProviderServices ps
       JOIN AspNetUsers u ON u.Id = ps.ProviderId
       JOIN Services s    ON s.id = ps.ServiceId;

/* ---- Provider media ------------------------------------------------------ */
SELECT pi.*, u.Email AS ProviderEmail, u.FullName AS Provider
FROM   ProviderPortfolioImages pi JOIN AspNetUsers u ON u.Id = pi.ProviderId;

SELECT ci.*, u.Email AS ProviderEmail, u.FullName AS Provider
FROM   ProviderCertificateImages ci JOIN AspNetUsers u ON u.Id = ci.ProviderId;

/* ---- Bookings (provider is LEFT-joined: null while Pending/Dispatching) --- */
SELECT b.*, s.NameEn AS Service,
       cu.Email AS CustomerEmail, cu.FullName AS Customer,
       pu.Email AS ProviderEmail, pu.FullName AS Provider
FROM   Bookings b
       JOIN Services s        ON s.id = b.ServiceId
       JOIN AspNetUsers cu    ON cu.Id = b.CustomerId
       LEFT JOIN AspNetUsers pu ON pu.Id = b.ProviderId;

SELECT h.*, cb.Email AS ChangedByEmail, cb.FullName AS ChangedBy
FROM   BookingStatusHistories h
       LEFT JOIN AspNetUsers cb ON cb.Id = h.ChangedByUserId;

/* ---- Money --------------------------------------------------------------- */
SELECT p.*, s.NameEn AS Service,
       cu.Email AS CustomerEmail, pu.Email AS ProviderEmail
FROM   Payments p
       JOIN Bookings b        ON b.Id = p.BookingId
       JOIN Services s        ON s.id = b.ServiceId
       JOIN AspNetUsers cu    ON cu.Id = b.CustomerId
       LEFT JOIN AspNetUsers pu ON pu.Id = b.ProviderId;

SELECT po.*, u.Email AS ProviderEmail, u.FullName AS Provider
FROM   Payouts po JOIN AspNetUsers u ON u.Id = po.ProviderId;

SELECT * FROM CommissionSettings;
SELECT * FROM CancellationPolicies;

/* ---- Engagement ---------------------------------------------------------- */
SELECT r.*, cu.Email AS CustomerEmail, cu.FullName AS Customer,
       pu.Email AS ProviderEmail, pu.FullName AS Provider
FROM   Reviews r
       JOIN AspNetUsers cu ON cu.Id = r.CustomerId
       JOIN AspNetUsers pu ON pu.Id = r.ProviderId;

SELECT cm.*, u.Email AS SenderEmail, u.FullName AS Sender
FROM   ChatMessages cm JOIN AspNetUsers u ON u.Id = cm.SenderId;

SELECT n.*, u.Email AS UserEmail, u.FullName AS [User]
FROM   Notifications n JOIN AspNetUsers u ON u.Id = n.UserId;

SELECT * FROM NotificationTemplates;

/* ---- Misc ---------------------------------------------------------------- */
SELECT ad.*, u.Email, u.FullName
FROM   Addresses ad JOIN AspNetUsers u ON u.Id = ad.UserId;

SELECT cf.*, u.Email AS CustomerEmail, s.NameEn AS Service
FROM   CustomerFavorites cf
       JOIN AspNetUsers u ON u.Id = cf.CustomerId
       JOIN Services s    ON s.id = cf.ServiceId;

SELECT cfp.*, cu.Email AS CustomerEmail, pu.Email AS ProviderEmail, pu.FullName AS Provider
FROM   CustomerFavoriteProviders cfp
       JOIN AspNetUsers cu ON cu.Id = cfp.CustomerId
       JOIN AspNetUsers pu ON pu.Id = cfp.ProviderId;

SELECT rt.*, u.Email
FROM   RefreshTokens rt JOIN AspNetUsers u ON u.Id = rt.UserId;

SELECT * FROM Banners;

SELECT al.*, u.Email
FROM   AuditLogs al LEFT JOIN AspNetUsers u ON u.Id = al.UserId;

SELECT * FROM __EFMigrationsHistory;
GO

/* ===========================================================================
   MAKE ONE PROVIDER DISPATCH-ELIGIBLE
   ---------------------------------------------------------------------------
   Run this block ONLY when you want a dispatch to find someone. It sets the
   provider (identified BY EMAIL) to Active + Online near Cairo, and makes them
   offer every active service. Change the email on each line to your provider.
   These statements WRITE — they are separated from the read-only section above.
   =========================================================================== */

UPDATE Providers
SET    State = 1,                 -- Active
       AvailabilityStatus = 0,    -- Online
       CurrentLatitude  = 30.0444,
       CurrentLongitude = 31.2357
WHERE  ApplicationUserId = (SELECT Id FROM AspNetUsers WHERE Email = 'provider@test.com');

INSERT INTO ProviderServices (Id, ProviderId, ServiceId, IsActive, CreateAt)
SELECT NEWID(),
       (SELECT Id FROM AspNetUsers WHERE Email = 'provider@test.com'),
       s.id, 1, SYSUTCDATETIME()
FROM   Services s
WHERE  s.IsActive = 1
  AND  NOT EXISTS (
        SELECT 1 FROM ProviderServices ps
        WHERE ps.ProviderId = (SELECT Id FROM AspNetUsers WHERE Email = 'provider@test.com')
          AND ps.ServiceId  = s.id);

-- Confirm the provider is now eligible:
SELECT u.Email, u.FullName, p.State, p.AvailabilityStatus,
       p.CurrentLatitude, p.CurrentLongitude,
       (SELECT COUNT(*) FROM ProviderServices ps WHERE ps.ProviderId = p.ApplicationUserId) AS ServicesOffered
FROM   Providers p JOIN AspNetUsers u ON u.Id = p.ApplicationUserId
WHERE  u.Email = 'provider@test.com';
GO
