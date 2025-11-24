# About factos.json files

Factos.json files indicate the configuration for this library, there are 2 places where
factos.json files can be located:

1. In the root of the library, nromally used to call dotnet test on the root of the library or on CI pipelines.
2. In each test project (AppTester.csproj),used by Visual Studio Test Explorer and dotnet test when called on the test project.

The only difference between the 2 files is the path to the test runner apps.
You can always specify the config file by using the --run-settings option e.g.:

```
dotnet test --run-settings path\to\factos.json
```