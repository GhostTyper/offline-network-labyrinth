All C# code changes must comply with these guidelines:

* Do not use `var`; declare variables with explicit types.
* Use native type keywords (e.g., `long` instead of `Int64`, `string` instead of `String`).
* Implement properties with explicit backing fields; avoid auto-properties.
* Avoid adding NuGet packages, except for dependencies used exclusively by the test suite.
* Ignore NetworkLabyrinth (it's only there to look how the oritional, networked version was implemented) and only work on OfflineLabyrinth.
* Use .NET 8.
* Please not that you are offline. The test suite has been made availabe, but you can't check online for other/new packages.
