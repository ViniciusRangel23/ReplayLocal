# Build inputs

- Base Kairos 1.0.1: 04fd8bfc85f87b41a0c49e0fce6a778968e60401, GPL-2.0-or-later.
- SDK .NET 10.0.402 Windows x64: official builds.dotnet.microsoft.com release metadata, ZIP SHA512 ae4fd528cb7e9f6af6aaf7de36715a69f761f183744f1bb3025d19f5a92447090ec561410e46266c1d10a63781ed6f3de771760bc76c876d87daabc367b898cd.
- OBS Studio 32.2.2 official Windows x64 ZIP: github.com/obsproject/obs-studio/releases/tag/32.2.2, SHA256 4d6e40e3ab155f56b30de517380566a206d74b63cdf5ad49aa596924768f97e1.
- Packages restored from nuget.org only; exact transitive identities/content hashes in packages.lock.json.

SDK and OBS extracted locally; no global installation. No components taken from Kairos-Setup.exe. Hash validation establishes artifact identity, not absence of vulnerabilities. Native sources/licensing: THIRD-PARTY-NOTICES.md. Packaging records hashes of every distributed file.
