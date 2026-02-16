# Copilot Instructions

## Project Guidelines
- Spawn packs are stored in `Data/PACKS`; applying a pack replaces live spawn/settings files in `Data/UOR_DATA` and relies on the data watcher to sync to the server, with `PACKS` acting as a backup/staging area.
- Use Entities instead of DTO Models for data objects, following ServUO-style custom save approach.