# Access database and create tables

## How to access database

Before running, set environment variable: ConnectionStrings__DefaultConnection

Example (Windows PowerShell):

```
setx ConnectionStrings__DefaultConnection "Server=147.126.2.58;Database=Community_Event_Finder;User Id=XXXX;Password=XXXX;TrustServerCertificate=True;"
```

## Create databases

### Create tables

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

### Add index

```
CREATE UNIQUE INDEX UX_Event_Title_Time
ON Events(Title, StartTime, CreatedByUserId);

CREATE INDEX idx_events_starttime
ON Events(StartTime);

CREATE INDEX idx_favorites_user_event
ON Favorites(UserId, EventId);

CREATE INDEX idx_events_owner
ON Events(CreatedByUserId);
```

### Create test user

```
INSERT INTO Users (UserId, Email)
VALUES ('test-user', 'test@local');
```

### Add March test data

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
 'Study',
 '2026-03-05T18:00:00','2026-03-05T20:00:00',
 'Tech Hub Chicago','1871 W Sheridan Rd','Chicago','IL','60613',
 41.9532,-87.6533,'https://example.com/tech',@uid),

('System','sys001','Jazz Night Downtown',
 'Live jazz performance in the heart of Chicago.',
 'Music and theather',
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


