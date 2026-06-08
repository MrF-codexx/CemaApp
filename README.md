# CEMA - Cinema Management System 🎬

CEMA is a modern Cinema Management System built with **ASP.NET Core MVC** . It manages movies, screenings, halls, and seat bookings.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (Recommended) or VS Code.

### Setup

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/BelalWaheed/cema-mvc.git
    ```
2.  **Restore Dependencies**:
    The project will automatically restore NuGet packages upon build.
3.  **Run the Project**:
    Press `F5` in Visual Studio or run `dotnet run` in the terminal.

---

## Collaborator Workflow

To maintain a clean project structure, every developer has their own branch. Please follow these steps to ensure you are working correctly:

### 1. Checkout Your Branch

Before you start coding, switch to the branch assigned to you:

```powershell
# Fetch the latest branch information
git fetch

# Switch to your branch (replace [name] with: belal, essam, feras, mohamed, or ziad)
git checkout [name]
```

### 2. Stay Updated with Master

To avoid merge conflicts, regularly pull the latest changes from the `master` branch into your branch:

```powershell
# While on your branch:
git pull origin master
```

### 3. Committing Your Work

When you finish a task, commit and push your changes to **your** branch:

```powershell
# Stage all changes
git add .

# Commit with a meaningful message
git commit -m "Added [feature name] or Fixed [issue]"

# Push to your remote branch
git push origin [your-branch-name]
```

---

## Project Structure

- **Controllers**: Logic for handling web requests.
- **Models**: Database entities (Movie, Hall, Screening, etc.).
- **Views**: Razor pages for the UI.
- **wwwroot**: Static assets (CSS, JS, Images).

---

## Database

The project connects to a remote SQL Server hosted at `databaseasp.net`.

> [!IMPORTANT]
> Ensure you have an active internet connection while running the app, as the database is hosted online.
