#!/usr/bin/env bash

set -euo pipefail

export PATH="$HOME/.dotnet:$HOME/.local/node/bin:$PATH"

exec "$@"
