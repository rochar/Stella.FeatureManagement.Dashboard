---
id: LRN-20261003-aspire-postgres-image
date: 2026-10-03
obsolete:
summary: Pin the AppHost Postgres image tag (18) before WithDataVolume; an unpinned Aspire default can change and break the data volume
---
## Lesson
Keep `.WithImageTag("18")` on `AddPostgres` in `Example.AppHost/AppHost.cs`, before `WithDataVolume()` (Aspire picks the mount path from the tag). The pin and the Testcontainers image (`postgres:18-alpine`) should stay on the same major. A volume created by Postgres 17 or older must be recreated or `pg_upgrade`d before the 18 image will start on it. After an Aspire upgrade that fails on Postgres startup, check the image tag and the persistent container/volume first.

## What happened
After updating Aspire, `aspire run` failed with the Postgres 18+ image error "there appears to be PostgreSQL data in /var/lib/postgresql". The new default image was 18+, but the persistent `WithDataVolume()` volume held data from an older major version, in the old `/var/lib/postgresql/data` layout. Fix: pin the image tag to the older major version. The alternative, deleting the volume, loses the local dev data.
