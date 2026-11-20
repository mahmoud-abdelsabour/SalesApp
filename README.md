# VTS Sales Web Application

> A full-featured sales management system built with ASP.NET MVC, Entity Framework 6, and SQL Server.

---

## 🔗 Quick Links

| Resource | Link |
|----------|------|
| **Live URL** | http://mahmoudahmed2003-001-site1.ltempurl.com |
| **Default Login** | Username: `admin` / Password: `Admin@123` |
| **GitHub Repo** | *https://github.com/mahmoud-abdelsabour/SalesApp* |

---

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Features](#features)
4. [Database Schema](#database-schema)
5. [Project Structure](#project-structure)
6. [Security Implementation](#security-implementation)
7. [Internationalization](#internationalization)
8. [UI & Responsiveness](#ui--responsiveness)
9. [Business Logic](#business-logic)
10. [Local Setup](#local-setup)
11. [Deployment](#deployment)
12. [Screenshots](#screenshots)

---

## Project Overview

VTS Sales is a web-based point-of-sale and sales management system designed for retail stores. It supports the full sales cycle from product catalog management through customer management and invoice creation, with multi-language support (Arabic and English) and full mobile responsiveness.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET MVC 5 (.NET Framework 4.8.1) |
| Language | C# 7.3+ |
| ORM | Entity Framework 6 (Code First) |
| Database | SQL Server 2019 |
| Frontend | Bootstrap 4.6, HTML5, CSS3 |
| UI Libraries | Select2 4.x (searchable dropdowns), SweetAlert2 (alerts & confirms) |
| Charts | Chart.js 3.9 |
| Auth | FormsAuthentication with custom concurrent-session control |
| Password hashing | PBKDF2 (HMAC-SHA256, 100,000 iterations, per-user salt) |
| Excel export | EPPlus 4.5.3.3 |
| PDF | Printable, RTL-aware pages saved as PDF from the browser's print dialog |
| i18n | .resx Resource Files (en-US + ar-EG) |
| Icons | Font Awesome 6 |
| Hosting | SmarterASP.NET (free tier) |

---

## Features

### Core Requirements and Features
- **Login** with 3-hour session, auto-redirect if already logged in within session window
- **Concurrent login prevention** — same account cannot be logged in from two devices simultaneously
- **Categories CRUD** (a category can't be deleted while it has active products)
- **Products CRUD** with soft delete for products with invoice history
- **Customers CRUD** with phone uniqueness validation
- **Sales Invoice** — multi-product, per-line discount, invoice-level discount
- **Idle timeout** — 2-minute inactivity warning via SweetAlert2 dialog
- **One-way password hashing** with PBKDF2; old reversible passwords are upgraded on first login
- **Client-side + server-side validation** on all forms
- **Responsive mobile layout** with different icon order on mobile vs desktop
- **Arabic + English** language support with RTL layout for Arabic
- **Code First** database using Entity Framework migrations
- **Stock management** — stock deducted on invoice save, validation prevents overselling
- **User management** — create/edit users, reset own password, activate/deactivate
- **Dashboard charts** — sales over 30 days (line), top 5 products (bar), revenue by category (doughnut)
- **Pagination** on all list pages (Categories: 10/page, Products/Customers: 15/page, Invoices: 20/page)
- **Invoice search & filter** — by date range, customer, and status
- **Collapsible sidebar navigation** — collapses to icon-only, mobile overlay, state persisted in localStorage, RTL-aware
- **Take over session** — signing in while already logged in elsewhere offers a confirmed take-over
- **Printable invoice / PDF** — standalone A4 invoice page with logo (Arabic/RTL-aware), opens the print dialog automatically after an invoice is created
- **Excel + PDF exports** for invoices, products and customers (honouring the current filters, never paginated), plus a single-invoice Excel export
- **Sales reports** — overview plus Sales by Date, by Customer and by Product, each with a chart, a server-side paginated table and Excel/PDF export
- **Concurrency safety** — row-version check on products, unique invoice numbers with retry, transactional stock deduction and restore on cancel

---

## Database Schema

The database consists of 7 tables managed via Entity Framework Code First migrations.

### Tables

#### Users
Stores login accounts. Passwords are stored as PBKDF2 hashes (`PBKDF2$iterations$salt$hash`), never plaintext or reversible. The column is still named `PasswordEncrypted`.
```
Id | Username | PasswordEncrypted | FullName | Email | IsActive | CreatedAt
```

#### ConcurrentSessions
Tracks active login tokens to enforce single-device login. A row is inserted on login and deleted on logout or session expiry.
```
Id | UserId (FK→Users) | SessionToken | LoginTime | LastActivity | DeviceInfo
```

#### Categories
Product groupings with bilingual names. Deleting a category cascades to delete all its products (if no invoice history exists).
```
Id | NameEn | NameAr | Description | IsActive | CreatedAt
```

#### Products
Items for sale. Linked to a Category. Uses soft delete (IsActive=false) if the product appears in any invoice, to preserve invoice history.
```
Id | NameEn | NameAr | CategoryId (FK→Categories) | Price | StockQuantity | Description | IsActive | CreatedAt
```

#### Customers
Buyer profiles. Phone number is unique. Soft-deleted if they have invoices.
```
Id | FullName | Phone | Email | Address | IsActive | CreatedAt
```

#### Invoices
Sales invoice header. InvoiceNumber is auto-generated as `INV-YYYYMMDD-####`. Status can be Draft, Confirmed, or Cancelled. Invoices are never hard-deleted.
```
Id | InvoiceNumber | CustomerId (FK→Customers) | InvoiceDate | SubTotal | InvoiceDiscountPct | InvoiceDiscountAmt | TotalAmount | Notes | CreatedBy (FK→Users) | CreatedAt | Status
```

#### InvoiceItems
Line items within an invoice. ProductName and UnitPrice are **snapshotted** at time of sale so invoice history is preserved even if the product price changes later.
```
Id | InvoiceId (FK→Invoices, CASCADE) | ProductId (FK→Products, NO CASCADE) | ProductName | UnitPrice | Quantity | DiscountPct | DiscountAmt | LineTotal
```

### Key Relationships
- `Categories → Products`: **CASCADE DELETE** — deleting a category deletes all its products
- `Invoices → InvoiceItems`: **CASCADE DELETE** — deleting an invoice deletes its line items
- `Products → InvoiceItems`: **NO CASCADE** — product history preserved in invoices
- `Customers → Invoices`: **NO CASCADE** — customer history preserved

---

## Project Structure

The app is layered: **controllers stay thin** (read the request, call a service, return a view / JSON / file) and **all application logic lives in services**.

```
WebApplication2/
├── Controllers/
│   ├── AppController.cs           Base class: login required, DbContext, shared HTTP helpers
│   ├── AuthController.cs          Login, take-over, logout, keep-alive, language switch
│   ├── HomeController.cs          Dashboard + chart data AJAX endpoints
│   ├── CategoriesController.cs    CRUD
│   ├── ProductsController.cs      CRUD + exports
│   ├── CustomersController.cs     CRUD + search AJAX + exports
│   ├── InvoicesController.cs      Create, list, details, print, cancel, exports
│   ├── ReportsController.cs       Reports overview + by date / customer / product
│   └── UsersController.cs         Create, edit, reset password, toggle active
│
├── Services/                      Application logic (one DbContext per request)
│   ├── Common/                    ServiceResult, PagedResult
│   ├── SessionService.cs          Login, take-over, single-session control
│   ├── UserService.cs
│   ├── CategoryService.cs
│   ├── CustomerService.cs
│   ├── ProductService.cs
│   ├── InvoiceService.cs          Create (stock, numbering, retry), cancel, details, exports
│   ├── DashboardService.cs
│   ├── ReportService.cs
│   └── SalesQueries.cs            One definition of "a sale" (Confirmed invoices)
│
├── Models/
│   ├── Entities/                  EF entity classes (7 tables) + InvoiceStatus constants
│   ├── ViewModels/                DTOs for views (incl. TableReport shared by Excel/PDF)
│   └── Data/
│       └── AppDbContext.cs        DbContext with relationship config
│
├── Helpers/
│   ├── PasswordHelper.cs          PBKDF2 hashing / verification
│   ├── EncryptionHelper.cs        LEGACY 3DES (only to upgrade old passwords)
│   ├── AuthCookieHelper.cs        Login cookie / session token handling
│   ├── ExcelExportHelper.cs       Styled .xlsx files (tables + single invoice)
│   └── LanguageHelper.cs          Language cookie, culture, T(en, ar)
│
├── Filters/
│   └── CustomAuthorizeAttribute.cs  Session token validation on every request
│
├── Resources/
│   ├── Labels.resx                English strings
│   └── Labels.ar.resx             Arabic strings
│
├── Views/
│   ├── Auth/Login.cshtml
│   ├── Home/Index.cshtml          Dashboard with charts + icon grid
│   ├── Categories/ Products/ Customers/ Users/
│   ├── Invoices/                  Index, Create, Details, Print
│   ├── Reports/                   Index (overview), ReportPage, shared partials
│   └── Shared/
│       ├── _Layout.cshtml         Master layout: sidebar, navbar, idle timer
│       ├── _Pagination.cshtml     Shared pagination partial
│       └── ListReport.cshtml      Printable list / report page (PDF)
│
├── Migrations/                    EF Code First migration files
├── Web.config                     Connection string, Forms auth config
└── Web.Release.config             Production connection string transform
```

---

## Security Implementation

### Password Hashing
Passwords are hashed with **PBKDF2 (HMAC-SHA256, 100,000 iterations, 16-byte random salt per user)** and compared in constant time. The stored value is `PBKDF2$iterations$salt$hash`; `PasswordHelper` provides `Hash`, `Verify` and `NeedsUpgrade`.

Accounts created before this change still hold the old reversible 3DES value. They keep working and are re-saved as PBKDF2 hashes the first time they log in. `EncryptionHelper` and the `EncryptionKey` setting only exist for that upgrade and can be removed once every user has logged in once.

### Session Management
- FormsAuthentication with a 3-hour sliding expiration
- A custom `FormsAuthenticationTicket` stores a GUID session token in the `UserData` field
- On every request, `CustomAuthorizeAttribute` decrypts the ticket, extracts the token, and verifies it exists in the `ConcurrentSessions` table
- If the token is missing or expired: cookie cleared, session row deleted, redirect to login

### Concurrent Login Prevention
When a user logs in, the system checks `ConcurrentSessions` for an existing row with the same `UserId`. If found, the login page explains the account is already in use and offers **Take over session** (with a confirmation and a password re-entry). Taking over deletes the old session row, so the other browser is signed out on its next request. Session rows are also deleted on logout, session expiry and when a user is deactivated.

Expired sessions (older than 3 hours) are automatically cleaned up on every login attempt.

### Anti-CSRF
All POST forms include `@Html.AntiForgeryToken()` and all POST actions are decorated with `[ValidateAntiForgeryToken]`.

---

## Internationalization

The app supports Arabic (ar-EG) and English (en-US) using .NET Resource Files.

- `Resources/Labels.resx` — English (default, generates `Labels` class)
- `Resources/Labels.ar.resx` — Arabic (no code generation; auto-selected when culture is ar-EG)

The user's language preference is stored in a 1-year cookie named `lang`. On every request, `Application_BeginRequest` in `Global.asax` reads the cookie and sets `Thread.CurrentThread.CurrentCulture` and `CurrentUICulture`.

RTL layout for Arabic is implemented via CSS variables and direction overrides in the `_Layout.cshtml` `<style>` block, applied when `dir="rtl"` is set on the `<html>` tag.

---

## UI & Responsiveness

### Sidebar Navigation
The sidebar is fixed to the left (right in Arabic RTL mode) and contains all navigation links. It:
- Collapses to icon-only mode on desktop (toggle button in navbar)
- Appears as a full-width overlay on mobile, hidden by default
- Remembers collapsed/expanded state in `localStorage`
- Highlights the active page link with a teal left-border accent

### Home Page Icon Order
The Quick Access icon grid uses Bootstrap's `order-*` and `order-md-*` classes to display icons in a different order on mobile vs desktop:

| Icon | Desktop | Mobile |
|------|---------|--------|
| Dashboard | 1st | 5th |
| Create Invoice | 2nd | 1st |
| Invoices | 3rd | 2nd |
| Customers | 4th | 3rd |
| Products | 5th | 4th |
| Categories | 6th | 6th |

### Idle Timeout
A JavaScript timer resets on every `mousemove`, `keydown`, `click`, and `scroll` event. After 2 minutes of inactivity a SweetAlert2 dialog appears with "Continue" and "Logout" buttons. If the user clicks Continue, an AJAX POST to `/Auth/KeepAlive` updates the `LastActivity` timestamp in `ConcurrentSessions`. If they click Logout, they are redirected to `/Auth/Logout`. If they ignore the dialog, it stays open indefinitely (no auto-logout per business requirement).

---

## Business Logic

### Invoice Calculation
```
LineTotal = (UnitPrice × Quantity) - (UnitPrice × Quantity × LineDiscountPct / 100)
SubTotal  = Σ LineTotals
InvoiceDiscountAmt = SubTotal × InvoiceDiscountPct / 100
TotalAmount = SubTotal - InvoiceDiscountAmt
```

All calculations happen both client-side (live JS preview) and server-side (authoritative save).

### Price Snapshot
When an invoice is saved, `ProductName` and `UnitPrice` are copied from the Product record into `InvoiceItems`. This means if a product's price changes later, all historical invoices retain the price at time of sale.

### Stock Management
When an invoice is saved (status = Confirmed):
1. Server validates each line: `Quantity > 0` and `Quantity <= Product.StockQuantity`
2. If any line fails: invoice is rejected with an error message
3. If all lines pass: stock is deducted inside the same database transaction

### Soft Delete Rules
| Entity | Condition | Action |
|--------|-----------|--------|
| Category | Has active products | Block delete, show error |
| Category | No active products | Hard delete (cascades to products) |
| Product | Has invoice history | Soft delete (IsActive=false) |
| Product | No invoice history | Hard delete |
| Customer | Has invoices | Soft delete |
| Customer | No invoices | Hard delete |
| User | Is last active user | Block deactivation |
| User | Deactivates self | Soft delete + auto logout |

---

## Local Setup

### Prerequisites
- Visual Studio 2022 Community
- SQL Server 2022 Developer Edition (or LocalDB)
- SQL Server Management Studio (SSMS)

### Steps

1. **Clone the repository**
   ```
   git clone https://github.com/YOURUSERNAME/VTSSales.git
   ```

2. **Open the solution**
   Open `WebApplication2.sln` in Visual Studio 2022

3. **Update the connection string**
   In `Web.config`, update `DefaultConnection`:
   ```xml
   <add name="DefaultConnection"
        connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VTSSalesDB;Integrated Security=True;MultipleActiveResultSets=True"
        providerName="System.Data.SqlClient" />
   ```

4. **Run migrations**
   In Package Manager Console:
   ```
   Update-Database
   ```
   This creates the database and seeds the admin user.

5. **Run the project**
   Press `F5`. The browser opens at the login page.

6. **Login**
   - Username: `admin`
   - Password: `Admin@123`

---

## Deployment

The app is hosted on **SmarterASP.NET** (free tier, supports .NET Framework 4.8.1).

**Production database:** `sql6030.site4now.net` (SQL Server 2019)

To update production after making changes:
1. Change build config to **Release** in Visual Studio
2. Build → Build Solution
3. Open FileZilla, connect to `win6215.site4now.net` with FTP credentials
4. Upload contents of `obj/Release/Package/PackageTmp/` to the `site1/` folder on the server
5. If migrations were added: connect to production DB in SSMS and run the migration SQL script

---

## Database ERD

![ERD](screenshots/00-erd.png)

## Screenshots

![Login page](screenshots/01-login.png)
![Dashboard](screenshots/02-dashboard.png)
![Dashboard Arabic RTL](screenshots/03-dashboard-arabic.png)
![Create Invoice](screenshots/04-create-invoice.png)
![Invoice Details](screenshots/05-invoice-details.png)
![Products list](screenshots/06-products.png)
![Mobile view](screenshots/07-mobile.png)
![Idle timeout warning](screenshots/08-idle-warning.png)
---

## License

This project was developed and published by Mahmoud Ahmed