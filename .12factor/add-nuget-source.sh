#!/usr/bin/env bash
set -eox pipefail

source env.sh

dotnet nuget add source https://mdsol.jfrog.io/artifactory/api/nuget/nuget-local --name Artifactory \
  --username $ARTIFACTORY_USERNAME --password $ARTIFACTORY_PASSWORD --store-password-in-clear-text
