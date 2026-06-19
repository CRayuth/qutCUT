# qutCUT — Development Environment Setup

Complete guide for setting up Visual Studio 2022 to build and run qutCUT on Windows.

---

## Requirements

| Item | Minimum |
|---|---|
| OS | Windows 10 (2004) or Windows 11 |
| RAM | 8 GB (16 GB recommended) |
| Disk | 15 GB free (20 GB recommended) |
| Architecture | x64 |

> **Low disk space?** See the [Install to D: drive](#installing-to-d-drive) section before starting.

---

## Step 1 — Download Visual Studio 2022

1. Go to: https://visualstudio.microsoft.com/vs/community/
2. Click **Download Visual Studio Community 2022** (free)
3. Run the downloaded `VisualStudioSetup.exe`

---

## Step 2 — Choose Install Location (optional — skip if C: has space)

Before selecting workloads, the installer shows an **Installation locations** tab.

Click it and change all three paths to your D: drive:

```
Visual Studio IDE:          D:\VisualStudio\2022\Community
Download cache:             D:\VisualStudio\Downloads
Shared components & SDKs:  D:\VisualStudio\Shared
```

---

## Step 3 — Select Workloads

On the **Workloads** tab, check these three:

### Desktop & Mobile
- [x] **.NET desktop development**
- [x] **Desktop development with C++**
- [x] **WinUI application development**

> All three are required. WinUI 3 apps need the C++ toolchain even though the code is C#.

Click **Install** (bottom right). This will download ~8–10 GB.

---

## Step 4 — Move NuGet Cache to D: (optional — saves ~2–4 GB on C:)

Open **Command Prompt** or **PowerShell** and run:

```powershell
[System.Environment]::SetEnvironmentVariable("NUGET_PACKAGES", "D:\.nuget\packages", "User")
```

Or set it manually:
1. Search **"Environment Variables"** in Start
2. Click **"Edit the system environment variables"**
3. Click **Environment Variables**
4. Under **User variables**, click **New**
   - Name: `NUGET_PACKAGES`
   - Value: `D:\.nuget\packages`
5. Click OK

---

## Step 5 — Install FFmpeg

qutCUT uses FFmpeg for video processing. The app expects the FFmpeg binaries in a `ffmpeg\` folder next to the built executable.

**Easy option — let the app download it automatically:**

The project uses `Xabe.FFmpeg.Downloader`. On first run, FFmpeg will download itself automatically if it is not found.

**Manual option:**

1. Download FFmpeg full build from: https://www.gyan.dev/ffmpeg/builds/
   - Choose: `ffmpeg-release-full.7z`
2. Extract and copy `ffmpeg.exe`, `ffprobe.exe`, `ffplay.exe` into:
   ```
   qutCUT\qutCUT\bin\Debug\net9.0-windows10.0.26100.0\win-x64\ffmpeg\
   ```

---

## Step 6 — Open the Project

1. Open **Visual Studio 2022**
2. Click **Open a project or solution**
3. Navigate to the cloned repo and open:
   ```
   qutCUT\qutCUT.sln
   ```
4. Visual Studio will ask *"Do you want to open this as a solution?"* — click **Yes**

---

## Step 7 — Configure NuGet Package Source

If packages fail to restore:

1. Go to **Tools → NuGet Package Manager → Package Manager Settings**
2. Click **Package Sources** in the left panel
3. Click **+** and add:
   - Name: `nuget.org`
   - Source: `https://api.nuget.org/v3/index.json`
4. Click **Update → OK**

---

## Step 8 — Build the Project

1. Make sure the configuration is set to **Debug | x64** (dropdown in the toolbar)
2. Press **Ctrl+Shift+B** to build
3. NuGet will restore all packages on first build (~500 MB download)
4. Build should complete with 0 errors

---

## Step 9 — Run the App

Press **F5** to start with debugger, or **Ctrl+F5** to run without.

The app will open showing the **Home screen** where you can create or open a `.qcut` project.

---

## Troubleshooting

### `NU1101` — Package not found
NuGet is using the offline cache only. Follow Step 7 to add nuget.org as a package source.

### `MSB` — GetLatestMSVCVersion failed / VC\Tools\MSVC not found
The **Desktop development with C++** workload is not installed. Open **Visual Studio Installer → Modify** and check it.

### `NETSDK1045` — .NET version not supported
Install the **.NET 9 SDK** from: https://dotnet.microsoft.com/download/dotnet/9.0

### Build succeeds but app crashes on launch
FFmpeg binaries are missing. See Step 5.

### Packages restore to C: even after setting NUGET_PACKAGES
Restart Visual Studio after setting the environment variable.

---

## Installing to D: Drive

If you are setting up for the first time and C: is low on space:

1. When the VS Installer first opens (before any installation), click **Continue**
2. On the workload screen, click the **Installation locations** tab at the top
3. Change all paths from `C:\` to `D:\`
4. Then select your workloads and install

If Visual Studio is already installed on C:, you cannot move it without reinstalling. You can still redirect NuGet (Step 4) to save 2–4 GB on future package downloads.

---

## Summary of What Gets Installed

| Component | Size | Location |
|---|---|---|
| Visual Studio 2022 Community | ~4 GB | C: or D: (your choice) |
| Desktop dev with C++ workload | ~3 GB | Same as VS |
| WinUI application development | ~1.5 GB | Same as VS |
| NuGet packages | ~500 MB | D:\.nuget (if Step 4 done) |
| FFmpeg binaries | ~120 MB | Project output folder |

Total: ~9–10 GB
