# Create local database

### Overview

```
C:\Users\Administrator\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB
```

* Name: CampusEvents
* Tables: Events, Users, Favorites

### Create databases:

```
CREATE TABLE Users (
    UserId NVARCHAR(50) PRIMARY KEY DEFAULT CONVERT(NVARCHAR(50), NEWID()),
    Email NVARCHAR(200) UNIQUE,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Events (
    EventId NVARCHAR(50) PRIMARY KEY DEFAULT CONVERT(NVARCHAR(50), NEWID()),

    Source NVARCHAR(50) NOT NULL,
    SourceEventId NVARCHAR(200) NULL,

    Title NVARCHAR(300) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Category NVARCHAR(100) NULL,

    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,

    VenueName NVARCHAR(200) NULL,
    Address NVARCHAR(500) NULL,
    City NVARCHAR(100) NOT NULL DEFAULT 'Chicago',
    State NVARCHAR(50) NULL,
    Zip NVARCHAR(20) NULL,

    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,

    Url NVARCHAR(500) NULL,
    CreatedByUserId NVARCHAR(50) NULL,

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE UNIQUE INDEX UX_Events_Source_SourceEventId
ON Events(Source, SourceEventId)
WHERE SourceEventId IS NOT NULL;

CREATE INDEX IX_Events_StartTime ON Events(StartTime);

CREATE TABLE Favorites (
    UserId NVARCHAR(50) NOT NULL,
    EventId NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    PRIMARY KEY (UserId, EventId),

    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (EventId) REFERENCES Events(EventId)
);

CREATE INDEX IX_Favorites_EventId ON Favorites(EventId);
```

### Add test data:

```
INSERT INTO Users (Email)
VALUES ('testuser@demo.com');

INSERT INTO Events
(Source, Title, Description, Category, StartTime, EndTime,
 VenueName, Address, Latitude, Longitude, CreatedByUserId)
VALUES
('Manual',
 'Chicago AI Meetup',
 'Networking + AI talks',
 'Tech',
 DATEADD(day,2,GETDATE()),
 DATEADD(hour,2,DATEADD(day,2,GETDATE())),
 'Tech Hub',
 '123 Innovation Ave',
 41.881832,
 -87.623177,
 (SELECT TOP 1 UserId FROM Users));

 INSERT INTO Favorites (UserId, EventId)
VALUES (
 (SELECT TOP 1 UserId FROM Users),
 (SELECT TOP 1 EventId FROM Events)
);
```

### February

```
DECLARE @y INT = YEAR(GETDATE());
DECLARE @m INT = MONTH(GETDATE());

INSERT INTO Events
(EventId, Source, Title, Description, Category,
 StartTime, EndTime, VenueName, Address, City, State, Zip,
 Latitude, Longitude, Url, CreatedByUserId)
VALUES
(CONVERT(NVARCHAR(36), NEWID()), 'Seed',
 'Study Group @ LUC Library',
 'Group study session for CS students.',
 'Study',
 DATETIMEFROMPARTS(@y,@m,10,18,0,0,0),
 DATETIMEFROMPARTS(@y,@m,10,20,0,0,0),
 'Cudahy Library',
 '6525 N Sheridan Rd', 'Chicago', 'IL', '60626',
 41.9987, -87.6583,
 NULL, NULL),

(CONVERT(NVARCHAR(36), NEWID()), 'Seed',
 'Downtown Jazz Night',
 'Live jazz night near the Loop.',
 'Music',
 DATETIMEFROMPARTS(@y,@m,12,19,30,0,0),
 DATETIMEFROMPARTS(@y,@m,12,22,0,0,0),
 'Jazz Showcase',
 '806 S Plymouth Ct', 'Chicago', 'IL', '60605',
 41.8719, -87.6282,
 NULL, NULL),

(CONVERT(NVARCHAR(36), NEWID()), 'Seed',
 'Career Talk @ UChicago',
 'Career talk and networking.',
 'Career',
 DATETIMEFROMPARTS(@y,@m,15,17,0,0,0),
 DATETIMEFROMPARTS(@y,@m,15,18,30,0,0),
 'Ida Noyes Hall',
 '1212 E 59th St', 'Chicago', 'IL', '60637',
 41.7878, -87.5986,
 NULL, NULL),

(CONVERT(NVARCHAR(36), NEWID()), 'Seed',
 'Airport Volunteer Meetup',
 'Volunteer meetup near O’Hare.',
 'Volunteer',
 DATETIMEFROMPARTS(@y,@m,18,10,0,0,0),
 DATETIMEFROMPARTS(@y,@m,18,12,0,0,0),
 'O’Hare Terminal 2',
 '10000 W O''Hare Ave', 'Chicago', 'IL', '60666',
 41.9742, -87.9073,
 NULL, NULL),

(CONVERT(NVARCHAR(36), NEWID()), 'Seed',
 'Evanston Board Game Social',
 'Board games + snacks.',
 'Social',
 DATETIMEFROMPARTS(@y,@m,20,18,30,0,0),
 DATETIMEFROMPARTS(@y,@m,20,21,0,0,0),
 'Coffee Lab',
 '1800 Maple Ave', 'Evanston', 'IL', '60201',
 42.0447, -87.6895,
 NULL, NULL);
```

### March

```
DECLARE @uid NVARCHAR(50)

SELECT @uid = UserId
FROM Users
WHERE Email = 'test@local'

IF @uid IS NULL
BEGIN
    PRINT 'User test@local not found'
    RETURN
END


INSERT INTO Events
(Source, SourceEventId, Title, Description, Category,
 StartTime, EndTime, VenueName, Address, City, State, Zip,
 Latitude, Longitude, Url, CreatedByUserId)
SELECT * FROM (VALUES

('User',NULL,'Chicago Tech Meetup',
 'Monthly developer meetup focusing on .NET and AI.',
 'Technology',
 '2026-03-05T18:00:00','2026-03-05T20:00:00',
 'Tech Hub Chicago','1871 W Sheridan Rd','Chicago','IL','60613',
 41.9532,-87.6533,'https://example.com/tech',@uid),

('System','sys001','Jazz Night Downtown',
 'Live jazz performance in the heart of Chicago.',
 'Music',
 '2026-03-08T19:00:00','2026-03-08T22:00:00',
 'Blue Note Lounge','212 W Chicago Ave','Chicago','IL','60654',
 41.8968,-87.6340,NULL,NULL),

('User',NULL,'Spring Food Festival',
 'Taste dishes from 20+ local restaurants.',
 'Food',
 '2026-03-12T12:00:00','2026-03-12T17:00:00',
 'Grant Park','337 E Randolph St','Chicago','IL','60601',
 41.8841,-87.6195,NULL,@uid),

('System','sys002','Community Yoga',
 'Outdoor yoga session open to all levels.',
 'Health',
 '2026-03-15T09:00:00','2026-03-15T10:30:00',
 'Lincoln Park','2045 N Lincoln Park W','Chicago','IL','60614',
 41.9194,-87.6336,NULL,NULL),

('User',NULL,'Art & Wine Evening',
 'Local artists exhibition with wine tasting.',
 'Arts',
 '2026-03-18T18:30:00','2026-03-18T21:00:00',
 'River North Gallery','445 N Clark St','Chicago','IL','60654',
 41.8893,-87.6312,NULL,@uid),

('System','sys003','Startup Pitch Night',
 'Early-stage startups pitch to investors.',
 'Business',
 '2026-03-22T17:00:00','2026-03-22T20:00:00',
 'UIC Innovation Center','1200 W Harrison St','Chicago','IL','60607',
 41.8744,-87.6586,NULL,NULL),

('User',NULL,'Family Movie Night',
 'Outdoor movie screening for families.',
 'Family',
 '2026-03-26T19:30:00','2026-03-26T21:30:00',
 'Millennium Park','201 E Randolph St','Chicago','IL','60602',
 41.8827,-87.6233,NULL,@uid),

('System','sys004','AI Workshop',
 'Hands-on machine learning workshop.',
 'Technology',
 '2026-03-29T10:00:00','2026-03-29T15:00:00',
 'Chicago Public Library','400 S State St','Chicago','IL','60605',
 41.8763,-87.6280,NULL,NULL)

) AS v
(Source,SourceEventId,Title,Description,Category,
 StartTime,EndTime,VenueName,Address,City,State,Zip,
 Latitude,Longitude,Url,CreatedByUserId)

WHERE NOT EXISTS (
    SELECT 1 FROM Events e
    WHERE e.Title = v.Title
      AND e.StartTime = v.StartTime
);
```


