# Bulk Email Sender - .NET Worker Service

## 📌 Description
This is a **.NET Worker Service** that reads an **Excel file** containing email addresses, subjects, and message templates, then sends **bulk emails** using **SMTP** and stores email audit logs in a **database**.

## 🚀 Features
- Reads **email data** from an Excel sheet
- Sends **bulk emails** using SMTP
- Stores email audit logs in a **database**
- Runs as a **background worker service**
- Uses **appsettings.json** for configuration
- Supports logging
- Supports attachments for emails

## 🛠️ Requirements
- **.NET 6 or later**
- **Visual Studio 2022**
- **Excel file (.xlsx) with Email Data**
- **SMTP credentials** (e.g., Gmail, Outlook, etc.)
- **SQL Database** (e.g., SQL Server, MySQL, or PostgreSQL)

## 📂 Project Structure
```
sendbulkemail/
│── Attachments/               # Folder for attachments
│   ├── channels4_profile.jpg  # Example attachment file
│── sendfiles/                 # Folder for storing email lists
│   ├── Job_Reference_Emails.xlsx # Sample Excel file
│── appsettings.json           # SMTP & database configuration
│── Program.cs                 # Entry point
│── Worker.cs                  # Background worker service
│── ExcelReader.cs             # Reads emails from Excel
│── EmailSender.cs             # Sends emails via SMTP
│── EmailData.cs               # Data model for emails
│── DatabaseService.cs         # Handles database interactions
│── EmailAudit.cs              # Stores email sending logs
│── Dependencies/              # Required libraries
```

## ⚙️ Setup & Installation
### 1️⃣ Clone Repository
```sh
git clone https://github.com/surajkumar04sk/sendbulkemail.git
cd sendbulkemail
```

### 2️⃣ Open in Visual Studio
- Open `sendbulkemail.sln` in **Visual Studio 2022**.

### 3️⃣ Configure SMTP, Database & Excel File
- Open `appsettings.json` and update:
```json
{
  "SMTP": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-email-password"
  },
  "Database": {
    "ConnectionString": "Server=your-server;Database=your-database;User Id=your-user;Password=your-password;"
  },
  "ExcelFilePath": "C:\\path\\to\\sendfiles\\Job_Reference_Emails.xlsx",
  "AttachmentsPath": "C:\\path\\to\\Attachments"
}
```

### 4️⃣ Run Database Migrations (if applicable)
- Ensure the database is running and execute the required SQL script to create the audit log table.

### 5️⃣ Run the Worker Service
```sh
dotnet run
```

## 📧 How to Use
1. Place the **Excel file** in the `sendfiles/` folder.
2. Add any required **attachments** in the `Attachments/` folder.
3. Start the **worker service** (`dotnet run`).
4. The emails will be sent automatically and logged in the database!

## 🛠️ Customize
- Modify `EmailSender.cs` to change email formatting and handle attachments.
- Adjust `Worker.cs` to modify processing logic.
- Update `DatabaseService.cs` to manage database operations.

## 🤝 Contributing
- Fork the repo & submit **pull requests**.
- Report issues & suggest features in **GitHub Issues**.

## 📜 License
MIT License

