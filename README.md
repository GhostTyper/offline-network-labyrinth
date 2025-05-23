# offline-network-labyrinth
A offline variant of the network labyrinth hosted on https://labyrinth.ctrl-s.de/.

## Offline Usage

The `OfflineLabyrinth` solution contains a class library that simulates the server locally. Instantiate `OfflineLabyrinth` with the desired lag in milliseconds and use the exposed `Stream` to communicate using the same protocol as the online version.

```csharp
using var lab = new OfflineLabyrinth.OfflineLabyrinth(7);
using var writer = new StreamWriter(lab.Stream) { NewLine = "\r\n", AutoFlush = true };
using var reader = new StreamReader(lab.Stream);
// interact with the labyrinth using the same commands as the network version
```
