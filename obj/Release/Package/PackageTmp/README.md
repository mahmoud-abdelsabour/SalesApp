# VTS Sales Web Application

## Live URL
http://mahmoudahmed2003-001-site1.ltempurl.com

## Default Login Credentials
- Username: admin
- Password: Admin@123

## Technology Stack
- ASP.NET MVC (.NET Framework 4.8.1)
- Entity Framework 6 (Code First)
- SQL Server 2019
- Bootstrap 4.6
- Select2 4.x
- SweetAlert2

## Features
- Login with 3-hour session management
- Concurrent login prevention (one device at a time)
- 2-minute idle timeout warning
- 2-way password encryption (3DES)
- Categories CRUD with cascading delete
- Products CRUD with soft delete
- Customers CRUD with phone uniqueness check
- Sales Invoice with multi-product lines
- Per-line and invoice-level discount
- Arabic and English language support (RTL)
- Fully responsive for mobile screens
- Different icon order on mobile vs desktop

## Local Setup Instructions
1. Clone the repository
2. Open VTSSales.sln in Visual Studio 2022
3. Update connection string in Web.config to point to your SQL Server
4. Open Package Manager Console and run: Update-Database
5. Press F5 to run
6. Login with admin / Admin@123

## Database
- Production: sql6030.site4now.net
- Database script included: VTSSales_DB.sql

## Project Structure
- Controllers/ - MVC Controllers
- Models/Entities/ - EF Entity Classes
- Models/ViewModels/ - View Models
- Views/ - Razor Views
- Helpers/ - Encryption, Language, Invoice Number helpers
- Filters/ - Custom Authorization filter
- Resources/ - English and Arabic resource files