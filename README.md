# Online Chess — C# WinForms + TCP Server

A small, explainable course-project foundation with a separate Windows Forms client, concurrent TCP server, shared JSON protocol and SQL Server persistence. The server owns account validation, matchmaking and chess move validation. TCP uses one UTF-8 JSON object per line on port **5050**.

## Run on Windows

1. Install .NET 10 SDK, Visual Studio with the **.NET desktop development** workload, and SQL Server Express.
2. Create the database with `Database/CreateDatabase.sql` in SQL Server Management Studio.
3. Set the connection string if needed: `$env:CHESS_DB='Server=localhost\SQLEXPRESS;Database=OnlineChess;Trusted_Connection=True;TrustServerCertificate=True;'`.
4. Build with SQL provider: `dotnet build OnlineChess.slnx -p:UseSqlClientPackage=true`.
5. Start the server: `dotnet run --project Chess.Server -p:UseSqlClientPackage=true`. It listens on all network interfaces at port 5050. Change the port with `CHESS_PORT`.
6. Start two clients: `dotnet run --project Chess.Client` (repeat in another terminal). Client endpoint is currently localhost; edit `Chess.Client/ChessForm.cs` to point at the server host for LAN/Internet use.
7. Register two accounts with passwords of at least 8 characters, then each user clicks **Find opponent**. The first waits; the second is paired.

The SQL provider package must be available at runtime. The package reference is conditional because this environment could not authenticate to NuGet; `-p:UseSqlClientPackage=true` enables it for normal development. There is no fake or in-memory database fallback.

## Features

- Registration and login with salted PBKDF2 password hashes.
- Concurrent TCP sessions, matchmaking, opponent chat, draw offers and resignation.
- Board UI, server-validated turn/legal movement, king safety, check/mate/stalemate and promotion to queen.
- SQL schema and persistence for accounts, games and moves; history and games-count leaderboard query protocol.

Castling, en passant, timer, profile editing and history/leaderboard UI screens remain follow-up work. See `docs/TEAM_AND_SCOPE.md` for team responsibilities and analysis/design diagrams.

## Projects

- `Chess.Client`: WinForms UI and TCP client.
- `Chess.Server`: multi-client TCP listener and SQL Server access.
- `Chess.Shared`: protocol DTO and chess rules.
- `Database`: SQL Server initialization script.
- `docs`: requirements, diagrams, wireframes and protocol.
