Monitor service
===============

This project runs as a Windows Service and polls the `PontoEncontroEvents` table.
When it finds pending events it marks them as processed and starts the `PontoDeEncontroDireto` executable if not already running.

Build
-----

From the Monitor folder:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

Adjust `-r` and `--self-contained` as needed.

Install as Windows Service (simple)
----------------------------------

1. Copy published files to a folder on the server, e.g. `C:\Services\PontoMonitor`.
2. Create the service using `sc` (run as Administrator):

```powershell
sc create PontoMonitor binPath= "C:\Services\PontoMonitor\Monitor.exe " start= auto DisplayName= "Ponto Monitor"
```

3. Set the service to run under an account with permission to access the database (Local System may work if DB is reachable). Configure via Services MMC.
4. Start the service:

```powershell
sc start PontoMonitor
```

Run parameters
--------------
The service accepts the same arguments as the console version. In `sc create` you can include arguments after the exe path.

Arguments: `<connection-string>` `<path-to-exe>` `[pollSeconds]` `[batchSize]`

Example `binPath` with arguments:

```powershell
sc create PontoMonitor binPath= "C:\Services\PontoMonitor\Monitor.exe \"Server=MYSERVER;Database=WSP;User Id=sa;Password=sa123;\" \"C:\Trilobit\PontoDeEncontro\PontoDeEncontroDireto.exe\" 3 50" start= auto
```

Notes
-----
- The monitor marks events atomically (UPDATE ... OUTPUT) to avoid double-processing when multiple monitors run.
- The monitor checks process name to avoid launching duplicate direct apps; the direct app should also use a Mutex to guarantee single instance.
- Prefer installing the service under a dedicated service account with least privileges.
