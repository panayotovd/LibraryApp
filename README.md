LibraryApp
==========

Small library system built with **ASP.NET Core MVC** and **EF Core**. Domains: **Books**, **Authors**, **Members**, **Events**. Uses ASP.NET Identity with roles and a CanWrite policy.

Features
--------
**Books**
*   List with sorting by Title, Author, Year
*   Search and filters by author and year range
*   CRUD with validation and flash messages via TempData
    
**Authors**
*   List with sorting by Name  
*   CRUD  
*   Details page shows related books
    
**Members**
*   List with sorting by Name, Email, Joined date  
*   CRUD
    

**Events**
*   List with sorting by Start, Title, Participants count 
*   CRUD
*   Participants managed on the **Edit** page 
    *   Add with op=add-member and memberId
    *   Remove with op=remove-member and memberId 
*   Details page shows participants and an available-members dropdown
    
**UI**
*   Unified table styling
*   Clickable sort headers with ↑/↓ indicators
*   Pager that preserves filters and sort
    
Tech Stack
----------
*   ASP.NET Core MVC 8 
*   Entity Framework Core
*   SQL Server (configured via DefaultConnection)
*   ASP.NET Identity (roles: Admin, Librarian)
    
Setup
-----
1.  **Configure database**: set DefaultConnection in appsettings.json.  
2.  **Apply migrations**: run dotnet ef database update.   
3.  **Identity seeding (no hardcoded secrets)**: provide admin credentials via User Secrets or environment variables; on startup the app seeds roles (Admin, Librarian) and an admin user if missing.    
    *   User Secrets (local dev): dotnet user-secrets init, then set Admin:Email and Admin:Password.        
    *   Environment variables: set ADMIN\_EMAIL and ADMIN\_PASSWORD.       
4.  **Run**: dotnet run.
    

Authorization
-------------
*   Policy CanWrite protects create, edit, delete, and event participant management.   
*   Index and Details actions are available to anonymous users.
    
Notes
-----
*   Delete flow follows MVC: **GET Delete** (confirmation) and **POST Delete** (action via ActionName("Delete")).
    
*   Trying to delete an Author with books results in throwing suitable error message; an Author cannot be deleted if it has relations to book(s).
    

Project Structure (high level)
------------------------------
*   **Controllers**: AuthorsController, BooksController, MembersController, EventsController    
*   **Data**: LibraryDbContext (for normal data), IdentitySeed (for users)    
*   **Models**: Author, Book, Member, Event, EventMember   
*   **ViewModels**: EventFormViewModel    
*   **Views**: Razor views per domain; shared partials like \_CrudActions, \_Pager, \_ValidationScriptsPartial
    

Key Flows to Verify
-------------------
*   Sorting by clicking column headers; filters persist across pages.    
*   Event participants: add on **Edit** with op=add-member; remove on **Edit** with op=remove-member; duplicates blocked and empty selection rejected.    
*   Flash messages via TempData\["Success"\] and TempData\["Error"\].
