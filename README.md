# TfL API Automation Coding Challenge

This repository contains a C# API test automation framework for the nine scenarios in the supplied Senior Test Analyst coding challenge. It uses Reqnroll for Gherkin scenarios, NUnit for assertions, and the .NET `HttpClient` for HTTP calls to DummyJSON.

## Run the scenarios

Install the .NET 8 SDK, then run from the repository root:

```powershell
dotnet test .\Tfl.DummyJson.ApiTests\Tfl.DummyJson.ApiTests.csproj
```

The API base URL defaults to `https://dummyjson.com/`. Set `DUMMYJSON_BASE_URL` to point the suite at another compatible instance.

## Design decisions

- Scenarios are written in Gherkin and map to Reqnroll step definitions.
- `ApiWorld` owns the HTTP client, response, parsed JSON, and scenario-scoped access token. Each scenario is independent; the authenticated-user scenario logs in before requesting `/auth/me`.
- Requests and assertions cover authentication, user filtering and creation, product search and pagination, simulated product update, cart creation, and simulated cart deletion.
- The red-hair scenario compares the filter endpoint's result and reported total against an independently retrieved full user list. This gives an exact count from the live dataset without embedding a potentially stale count in the test.
- The cart scenario calculates the aggregate discount percentage as `(total - discountedTotal) / total * 100`. `discountedTotal` is a currency amount; the calculated percentage is a rate. Since DummyJSON simulates writes, the checks validate each write response and do not expect later reads to contain the changes.
- The sample login credentials and catalog assertions follow the public DummyJSON sample data. If that public dataset changes, update the relevant expected values in `DummyJsonApi.feature`.

## AI use

AI was used to help draft the test framework and code in this repository. Review and adapt the implementation before submission.
