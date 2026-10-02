# Task 3 report: Station buy requests

## Delivered

- Added per-material Station requests with target stock, bid, enabled state, production reservations, and Merchant inbound quantities.
- Deficit is clamped at zero after subtracting Station stock, reserved quantity, and inbound units.
- Added direct Station purchase settlement with request and treasury limits. Returned sale data separates Station quantity, Merchant quantity, unsold quantity, gross, tax, and seller net.
- Tax withholding is recorded as its own ledger posting. For a direct Station purchase the tax account is the HUB treasury, so net HUB outflow equals seller proceeds while gross and tax remain visible in the transaction.

## Verification

- RED: `StationTests` failed to compile because `BuyRequest` and `Station` did not exist.
- GREEN: `dotnet build src/Game.Domain` succeeded with 0 warnings/errors; `dotnet test tests/Game.Domain.Tests` passed 130/130.

## Scope note

The legacy `FixedPriceMarket` remains wired through `HubWorld` until Task 7, where the request-aware Station and Merchant settlement is integrated into the Trainer return flow. Task 3 introduces the replacement domain behavior without prematurely changing the public world event loop.
