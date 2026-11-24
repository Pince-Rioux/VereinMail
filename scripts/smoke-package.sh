#!/usr/bin/env bash
# Exercise the actual README and packed DLL in a plain .NET 8 Generic Host.
set -euo pipefail
package=$(realpath "$1")
version=$2
root=$(cd -- "$(dirname -- "$0")/.." && pwd)
tmp=$(mktemp -d)
trap 'rm -rf -- "$tmp"' EXIT
mkdir "$tmp/feed"
cp -- "$package" "$tmp/feed/"
cat > "$tmp/Consumer.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <NuGetAuditMode>all</NuGetAuditMode>
    <WarningsAsErrors>NU1900;NU1901;NU1902;NU1903;NU1904;NU1905</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="VereinMail" Version="[$version]" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
  </ItemGroup>
</Project>
PROJECT
cat > "$tmp/NuGet.Config" <<'CONFIG'
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <auditSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </auditSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="VereinMail" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
CONFIG
awk '/^<!-- quickstart:start -->$/{show=1;next} /^<!-- quickstart:end -->$/{show=0} show && !/^```/' \
  "$root/README.md" > "$tmp/Program.cs"
cd "$tmp"
dotnet restore --configfile NuGet.Config --packages "$tmp/.packages" --verbosity quiet
Mail__Mode=File Mail__DefaultFrom__Email=from@example.com dotnet run --no-restore --verbosity quiet
test "$(find maildrop -name '*.eml' -type f | wc -l)" -eq 1
grep -q 'Subject: Quickstart' maildrop/*.eml
if grep -q 'Microsoft.AspNetCore.App' bin/Debug/net8.0/Consumer.runtimeconfig.json; then
  echo "Unexpected ASP.NET Core framework dependency" >&2
  exit 1
fi
