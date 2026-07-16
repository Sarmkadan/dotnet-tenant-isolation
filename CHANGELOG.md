## [Unreleased]
### Changed
- Architecture documentation rewritten to match the current codebase; now lives in docs/ARCHITECTURE.md
- Extracted ITenantResolutionService; middleware, DbContext factory and controllers now depend on the interface (concrete registration kept, no breaking change)

## [2.0.2] - 2026-05-27
### Fixed
- Fix cross-tenant data leak when connection pool returns wrong connection
- Added regression test for the fix

## [2.0.1] - 2026-05-26
### Security
- Added input validation and length limits
- Added request timeout configuration
- Added security policy and vulnerability reporting