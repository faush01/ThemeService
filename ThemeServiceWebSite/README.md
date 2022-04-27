# ThemeServiceWebSite

Web Service to host Chromaprints of TV Shows intro music

You will need to add a development config file

appsettings.Development.json

With the following DB connection info:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DatabaseInfo": {
    "Server": "tcp:<MS SQL Server Address>,1433",
    "Database": "<the database to use>",
    "User": "<sql server user>",
    "Password": "<sql server password>"
  }
}
```