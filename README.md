# Community Events Finder (Windows Desktop App)

COMP 425 Final Project. A desktop application for discovering, organizing, and visualizing local events. Built with **C# WinForms, SQL Server LocalDB, and GMap.NET (Bing Maps provider)**, this system aggregates monthly events, displays them on an interactive map, and provides advanced filtering, favorites, and calendar visualization.

## Overview

Students often struggle to track events because information is scattered across flyers, chats, and websites.  
This application solves that by providing a single unified desktop platform that supports:

- Browsing current events
- Map-based exploration
- Favorites tracking
- Distance filtering
- Calendar visualization
- ICS export
- Posting new events

## Key Features

Key features: 
1. Monthly activity list view: Browse all upcoming events within the next month in a clean, scrollable list.
2. Monthly map view: View events on an interactive map to quickly see what’s happening nearby.
3. Location-based search: Find activities within customizable distance ranges (e.g., 5 miles, 10 miles).
4. Favorite (Star) events: Save events interested in for quick access later.
5. Personal calendar view: See all starred events in a calendar format, and option to sync with Google / Apple Calendar.
6. Post new activities: Students and student organizations can easily post events like study groups, party, volunteer work, etc. (includes event details, location, time, and category).
7. Student-relevant event curation: The application displays events tailored to college students, such as:
   - Concerts and musicals
   - Campus org events
   - Parties and social mixers
   - Workshops, talks, and career events
   - Sports and recreational activities

## Data source:
• APIs:
   - Eventbrite API
   - Ticketmaster Discovery API
   - PredictHQ API
   - SeatGeek API
• Campus and university calendar
• Local venues and city calendars
• Student organizations and clubs
• Other user generated events

## Value proposition:
• One app replaces flyers, calendar and event creator
• Helps students discover events that they actually care about
• Encourages campus and community engagement
• Empowers students to create and promote their own events

## UML

![UML Diagram](https://github.com/bortiz-101/Community-Event-Finder/blob/yue/UML_DIAGRAM.svg)

## Current status

1. Monthly activity list view: Browse all upcoming events within the next month in a clean, scrollable list. **- can list activities within 1 months from local DB, need API to extract events to DB**
2. Monthly map view: View events on an interactive map to quickly see what’s happening nearby. **- done (interactive, zoom in/out, clustering)**
3. Location-based search: Find activities within customizable distance ranges (e.g., 5 miles, 10 miles). **- done (circle with radius, check address format)**
4. Favorite (Star) events: Save events interested in for quick access later. **- done**
5. Personal calendar view: See all starred events in a calendar format, and option to sync with Google / Apple Calendar. **- done. Users can download canlendar .ics file to open in ogle / Apple Calendar. Different colors for different category, allow events happening at the same time. Do we need extra API to sync?**
6. Post new activities: Students and student organizations can easily post events like study groups, party, volunteer work, etc. (includes event details, location, time, and category). **- done (with format check)**
7. Student-relevant event curation **- Need to use API to import events, and automatically filtration to only retain student related events**

![Status](https://github.com/bortiz-101/Community-Event-Finder/blob/yue/Status.png)

## Next steps

1. All open issues
2. Improve UI
3. API to import events and filtration
4. Need online DB

