"""Check the candidate's ZIP metadata before allowing the publish job to use it."""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from zipfile import ZipFile


def validate_version(version: str) -> None:
    number = r"(?:0|[1-9][0-9]*)"
    identifier = rf"(?:{number}|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)"
    if not re.fullmatch(rf"{number}\.{number}\.{number}(?:-{identifier}(?:\.{identifier})*)?", version):
        sys.exit("Expected major.minor.patch with optional SemVer prerelease (no build metadata).")


if len(sys.argv) not in (2, 3):
    sys.exit("usage: verify-release-package.py VERSION [PACKAGE]")
version = sys.argv[1]
validate_version(version)
if len(sys.argv) == 3:
    package = Path(sys.argv[2])
    if package.name != f"VereinMail.{version}.nupkg":
        sys.exit("Unexpected package filename.")
    with ZipFile(package) as archive:
        metadata = ET.fromstring(archive.read("VereinMail.nuspec")).find("{*}metadata")
        if metadata is None or metadata.findtext("{*}id") != "VereinMail" or metadata.findtext("{*}version") != version:
            sys.exit("Unexpected package ID or version.")
        readme = metadata.findtext("{*}readme")
        if readme != "README.md" or readme not in archive.namelist() or "lib/net8.0/VereinMail.dll" not in archive.namelist():
            sys.exit("Package is missing the advertised README or .NET 8 DLL.")
