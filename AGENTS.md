### Collaboration guidelines
- Use TDD when making changes in this repo.
- Run the full test suite before declaring a task done.

### Build & test
- Default configuration is Release; Debug is also allowed (`-c Debug`).
- Build: `dotnet build Sharpmake.sln /p:Configuration=Release /p:Platform="Any CPU"`
- Unit tests (NUnit): `dotnet test Sharpmake.UnitTests -c Release`
- Public API tests: `dotnet test Sharpmake.PublicApiTests -c Release`
- Regression tests (compare samples output to reference): `python regression_test.py` after building

### Generated outputs (don't hand-edit)
- Samples reference output: regenerate with `UpdateSamplesOutput.bat`
- Verified public API snapshot: regenerate with `UpdateVerifiedPublicApi.bat` when the public API intentionally changes

### Layout
- `Sharpmake/`: core, `Sharpmake.Generators/`: generators, `Sharpmake.Platforms/`: platforms
- `Sharpmake.Application/`: entry point, `samples/`: sample definitions, `docs/`: documentation
- Adding a sample: see `docs/Samples.md`

### Further reading (only when relevant)
- Writing or changing Sharpmake definitions (targets, projects, samples): read `docs/Guidelines.md`.
- Adding a platform or changing test/reference-output workflow: read `CONTRIBUTING.md`.
