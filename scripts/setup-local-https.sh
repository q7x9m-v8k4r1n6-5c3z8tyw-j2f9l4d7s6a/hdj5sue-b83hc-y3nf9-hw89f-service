#!/usr/bin/env bash
set -euo pipefail

certificate_dir="$HOME/.aspnet/https"
certificate_path="$certificate_dir/ovc-move-app.pfx"
certificate_password="local-dev-certificate"

dotnet dev-certs https --trust
mkdir -p "$certificate_dir"
dotnet dev-certs https -ep "$certificate_path" -p "$certificate_password"
chmod 600 "$certificate_path"

echo "Local HTTPS certificate is ready: $certificate_path"
