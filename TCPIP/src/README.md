# TCP/IP

## Build
To build the code, run:
```
dotnet run;
```
If it fails, try ```dotnet clean;```, and then run again.
## Preprocessor
Files in need for preprocessing ends with ```*.cpp.cs``` or ```*.t4.cs```.

Global macros for cpp can be found in  ```src/Components/macros.h```. These are included in every file with the ending ```*.cpp.cs```.

## Logging
Since SME would have to convert compiled binaries into vhdl  code when adding libraries, we cant include a logger library directly.

The logging therefore needs the cpp preprocessor to work correctly. To include a logging statement in SME classes, make sure that the file ends with ```*.cpp.cs```, And the logging statements are structured as follows:
```csharp
LOGGER.WARN("Some warning");
```
This makes the typechecker  and the SME compiler happy.

If you want non macro logging in a macro file, call this instead:
```csharp
Logging.log.Warn("Some warning");
```
