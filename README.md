# Community-Event-Finder

## 3/1/2026 Update

* Auto-refresh after creating events and prevent duplicates (same name, user, time)
* Users can delete only their own events; others hide delete option
* Map ↔ List sync: clicking marker or list item highlights and centers the other
* Marker popups show event info; invalid addresses trigger warnings instead of moving map
* Fetch events from today through the next 30 days
* Favorites system with filter and ICS export
* Calendar supports month/week/day views, category colors, side-by-side events, and detailed info
* Category dropdown with extensible color-coded event types
* Start page with default address + filters; main page includes search, reset (“Show All”), and radius tools
* Built with ASP.NET Core, SQL Server, Leaflet, and FullCalendar

## Target Audience: College students (18-24)
## Problem: 

College students often miss out on nearby events because information is scattered across flyers, group chats, and multiple platforms. There’s no single, student-focused place to discover, save, post and share activities happening around campus and nearby neighborhoods.

## Objective and Solution: 
A .NET Framework application designed specifically for college students to discover nearby campus and community events in the Chicago area occuring during the current month, organize their social calendar, and post new events, all in one place.

## Core Focus: 
1. Help college-age students discover relevant, local events; 
2. Make it easy to plan ahead and stay organized; 
3. Encourage students and student organizations to post their own events.

## Current situation

There are many existing event discovery platforms, such as Eventbrite, Meetup, Facebook Events, and Fever, that help users find local concerts, meetups, and community activities. However, they are designed for broad audiences and general event types rather than the specific, campus-oriented routines of college students. As a result, students still face fragmented information across platforms and limited tools for browsing, saving, organizing, and posting events tailored to their schedules, locations, and social networks, creating an opportunity for a student-focused solution.

## Proposed solution

The proposed solution is a UI application with a SQL database backend for data storage. The UI will display two screens. The first will be a Maps view that display events with an icon showing the location. The UI will allow users to click on dots to view more details. The second screen will be a table view of events that allows users to save events to a favorites list (STAR THEM). If time allows, the application may also support creating calendar reminders. Below are the details of key features, data source and value proposition.

## Key features: 
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

### Data source:
- APIs:
   - Eventbrite API
   - Ticketmaster Discovery API
   - PredictHQ API
   - SeatGeek API
- Campus and university calendar
- Local venues and city calendars
- Student organizations and clubs
- Other user generated events

## Value proposition:
• One app replaces flyers, calendar and event creator
• Helps students discover events that they actually care about
• Encourages campus and community engagement
• Empowers students to create and promote their own events

