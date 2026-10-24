# Build inputs

- Base Kairos 1.0.1: 04fd8bfc85f87b41a0c49e0fce6a778968e60401, GPL-2.0-or-later.
- SDK .NET 10.0.402 Windows x64: official builds.dotnet.microsoft.com release metadata, ZIP SHA512 ae4fd528cb7e9f6af6aaf7de36715a69f761f183744f1bb3025d19f5a92447090ec561410e46266c1d10a63781ed6f3de771760bc76c876d87daabc367b898cd.
- OBS Studio 32.2.2 official Windows x64 ZIP: github.com/obsproject/obs-studio/releases/tag/32.2.2, SHA256 4d6e40e3ab155f56b30de517380566a206d74b63cdf5ad49aa596924768f97e1.
- Packages restored from nuget.org only; exact transitive identities/content hashes in packages.lock.json.

SDK and OBS extracted locally; no global installation. No components taken from Kairos-Setup.exe. Hash validation establishes artifact identity, not absence of vulnerabilities. Native sources/licensing: THIRD-PARTY-NOTICES.md. Packaging records hashes of every distributed file.

## Corresponding source for portable releases

The portable release has a separate `ReplayLocal-Fontes-e-Licencas.zip`. `tools/package-sources.py --cache <source-cache> --output <new-zip>` checks cached archives against `licenses/native-sources.json` and bundles the exact Git snapshot and native sources. It does not execute downloaded source or build scripts.

OBS 32.2.2 pins obs-deps **2026-07-15**, Windows x64 ZIP SHA256 `6f90e9598fa10cff5ad23cdcfae49b87868c07bf896b02cd464582b4ce2f2ba9`. All PE sections of the twelve distributed dependency DLLs match that package; OBS adds signatures changing whole-file hashes. FFmpeg 8.1.2 declares GPL-3.0-or-later. Options, identities, commits, hashes and selections are recorded in `licenses/FFmpeg-build-configuration.txt` and `licenses/native-sources.json`. The full native stack has not been rebuilt here; the application uses unchanged official OBS runtime components.
