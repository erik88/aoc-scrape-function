# Scraper for Piktiv Advent of Code private leaderboard 2023

Scrapes Tim's leaderboard, and puts the result into a JSON-file in an Azure Storage Blob.

## Local setup

(I just bashed my head against Azure documentation until it worked. Ymmw)

### Prerequisites

* .net SDK (v6 as of writing)
* VsCode with extensions
  * Azure Functions
  * Azurite
  * Azure Tools
  * C# Dev Kit

### Steps

* Initialize and start azurite from VsCode command panel (`Ctrl+P`, type `>`, then search for Azurite).

* You should have a file called `local.settings.json`, containing

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SESSION_COOKIE": "session=xxx",
    "USE_DUMMY_DATA": "pleasedo"
  }
}
```

Replace xxx with the value you would see in the _request headers_ when logged in to Advent of Code (Open dev tools in webbrowser (Ctrl+Shift+C), then go to Network tab, then visit www.adventofcode.com)

* Replace `USE_DUMMY_DATA` with an empty string to activate real scraping.

* In `ScrapeLeaderboard.cs` switch the commented out header of the `Run` -function to use Http-based trigger.

* Debug with F5 and use a webbrowser to visit the address showing in the debug output.