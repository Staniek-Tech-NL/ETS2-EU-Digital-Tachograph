# Installation - ETS2 Digital Tachograph

## Requirements

- Windows x64;
- Euro Truck Simulator 2;
- the `win-x64` application package containing the application directory and
  the `plugin` directory.

The application is published as self-contained, so a separate .NET installation
is not required.

## 1. Extract the application

1. Download the correct release ZIP.
2. Extract the complete archive to a directory of your choice. Do not run the
   application directly from the archive.
3. Do not mix individual files from different package versions.

## 2. Install the SCS plugin

1. Close ETS2.
2. In the extracted package, locate:

   ```text
   plugin\ETS2Tachograph.ScsPlugin.dll
   ```

3. If Windows marked the DLL as downloaded from the Internet, right-click it,
   choose **Properties**, select **Unblock**, and confirm.
4. Copy the DLL to:

   ```text
   Euro Truck Simulator 2\bin\win_x64\plugins\
   ```

   The most common Steam path is:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\plugins\
   ```

5. Start ETS2 and accept the SDK usage prompt.

Restart the game whenever the plugin is replaced. The `sdk reload` command is
intended for development use only.

## 3. First start

1. Run `ETS2Tachograph.Desktop.exe` from the extracted application directory.
2. Wait until the connection status confirms active ETS2 telemetry.
3. Polish is used by default on the first start.
4. To select English, open **Settings**, choose
   **English (United Kingdom)**, save the settings, and restart the application.

The language change takes effect after the application restarts. PDF reports
use the language that is active when they are exported.

## User data and diagnostics

User data is stored outside the application directory:

```text
%LocalAppData%\ETS2Tachograph\
```

Important items:

- `tachograph.db` - the SQLite database;
- `tachograph.db.bak.YYYYMMDD-HHMMSS-fff` - pre-migration backups;
- `ui-culture.json` - the selected interface language;
- `Logs\tachograph-YYYY-MM-DD.log` - the diagnostic log;
- `Printouts\` - virtual-device printouts.

Updating the application files does not delete this database. Back up the data
directory before removing it manually.

## Common problems

- **No ETS2 connection:** verify the DLL location, accept the SDK prompt, and
  restart the game.
- **Protocol version mismatch:** replace the plugin DLL with the version from
  the same package as the application, then restart ETS2.
- **Application does not start:** use the path displayed in the error message
  or inspect the newest file in the `Logs` directory.
- **Data required for a bug report:** select **Diagnostic report** on the
  Dashboard and keep the generated ZIP.

This program is a gameplay support simulator. It is not a certified tachograph
and must not be used for legal or employment records.
