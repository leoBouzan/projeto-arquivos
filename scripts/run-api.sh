#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

exec "$ROOT_DIR/scripts/use-local-toolchain.sh" \
  dotnet run --project "$ROOT_DIR/src/backend/FileShare.API"
