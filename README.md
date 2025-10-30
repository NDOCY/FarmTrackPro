# 🌾 FarmTrack Pro  
### Smart Farm Management & E-Commerce System  

FarmTrack Pro is a comprehensive web-based farm management system developed in ASP.NET MVC (Entity Framework, SQL Server).  
It provides an end-to-end solution for managing agricultural operations, workforce activities, livestock records, inventory, crop lifecycles, and an integrated e-commerce marketplace for farm products.

---

## 🚀 Project Overview

FarmTrack Pro was developed in two major increments across the academic year:

### **Increment 1 – Core Farm Management**
Focused on digitizing internal farm operations and human resource workflows:
- **Job Applications Module:** Admins post vacancies for various farm departments (e.g., crop management, livestock, logistics).  
  Employees and registered users can apply directly through the system.  
- **Farm Tasks Module:** Enables admins to assign **recurring or one-time tasks** to employees or entire departments.  
  Supports task scheduling, progress tracking, and completion logging.  
- **Inventory Management:**  
  Tracks all farm inputs and tools, including fertilizers, seeds, and equipment.  
  - Logs **usage and restocking**.  
  - Integrates with **supplier lists** to automatically generate restock requests when stock reaches a threshold.  
- **Livestock Management:**  
  Records both purchased and newborn livestock.  
  Tracks health metrics, weight, vaccination schedules, and reproduction methods (natural or artificial insemination).  
- **Equipment Records:**  
  Manages tractors, irrigation pumps, and other farm machinery—covering usage logs, maintenance schedules, and performance status.

---

### **Increment 2 – Crop Management, Mapping & E-Commerce**
Expanded functionality to include crop lifecycle tracking and an integrated sales platform:
- **Plot & Crop Management:**  
  - Assign crops to specific plots with detailed soil, seed, and growth information.  
  - Interactive **map view** for plot visualization.  
  - Tracks entire **crop lifecycle** from soil preparation to harvesting.  
- **PlotCrop Lifecycle Dashboard:**  
  - Monitors key farming operations (weeding, fertilization, pest control, irrigation, harvesting).  
  - Includes **Activity Log** for field events like pest sightings, fertilizer applications, or growth measurements.
- **E-Commerce Integration:**  
  The farm can now directly sell products online via the built-in store.  
  - **Products** sourced from livestock, crops, or inventory.  
  - **Discount Vouchers** with fixed or percentage-based discounts.  
  - **Shopping Cart & Checkout** with real-time validation and stock synchronization.  
  - **Delivery Tracking** using a **unique delivery verification code** system.  
  - **Customer Reviews** restricted to **verified buyers**.  
  - **Sales Analytics** providing insights into top-selling products, voucher redemptions, and total revenue trends.

---

## 🧩 Core Features Summary

| Module | Key Features |
|--------|---------------|
| **Job Applications** | Post farm jobs, view applicants, and manage recruitment workflows. |
| **Farm Tasks** | Assign and monitor tasks by department or employee, with recurrence support. |
| **Inventory Management** | Track usage, restocking, supplier auto-notifications, and threshold alerts. |
| **Livestock Management** | Health monitoring, reproduction tracking, weight logs, and medical schedules. |
| **Equipment Records** | Machinery registration, maintenance tracking, and performance logs. |
| **Crop & Plot Management** | Crop assignment to plots, lifecycle activity tracking, and map visualization. |
| **E-Commerce** | Product catalog, discount vouchers, cart management, order tracking, and reviews. |
| **Delivery System** | Delivery confirmation via customer-issued code, with status updates. |
| **Sales Analytics** | Graphical sales insights, voucher performance, and order history visualization. |

---

## 🛠️ Tech Stack

| Category | Technology |
|-----------|-------------|
| **Frontend** | HTML5, CSS3, Bootstrap, JavaScript, jQuery |
| **Backend** | ASP.NET MVC (Framework, not Core) |
| **Database** | Microsoft SQL Server (Entity Framework Code-First) |
| **Authentication** | Session-based login with role access control (Admin, Employee, Customer, Driver) |
| **Mapping API** | Leaflet / Google Maps integration for plot visualization |
| **Analytics & Charts** | Chart.js / Custom Razor dashboard components |
| **Version Control** | Git & GitHub |
| **Hosting (Dev/Academic)** | Azure App Service with Azure SQL Database |

---

## 🔐 Roles and Access Levels

| Role | Permissions |
|------|--------------|
| **Admin / Owner** | Full access: manage jobs, tasks, inventory, products, vouchers, and analytics. |
| **Employee** | View assigned tasks, log activity, update completion status. |
| **Driver** | View assigned deliveries, update delivery status using customer code. |
| **Customer** | Browse store, manage cart, checkout, review products, and track orders. |

---

## 📈 Example Analytics Insights
- Total Revenue & Growth Rate  
- Most Redeemed Voucher Codes  
- Most Popular Product Categories  
- Average Order Value per Period  
- Sales Distribution by Customer Type  

---

## 💾 Installation & Setup

1. Clone the repository:  
   ```bash
   git clone https://github.com/NDOCY/FarmTrackPro.git
