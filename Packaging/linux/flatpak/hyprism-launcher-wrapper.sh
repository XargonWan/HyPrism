#!/bin/sh
# Bundled copy of hyprism-launcher-wrapper.sh for flatpak bundle packaging
# (keeps same behavior as Packaging/flatpak/hyprism-launcher-wrapper.sh)

# Minimal shim that execs the wrapper shipped in /app/lib/hyprism if present,
# otherwise execs the system wrapper (this file is kept to make bundle builds
# include the wrapper script). The real logic is in Packaging/flatpak/hyprism-launcher-wrapper.sh

if [ -x "/app/lib/hyprism/hyprism-launcher-wrapper.sh" ]; then
  exec /app/lib/hyprism/hyprism-launcher-wrapper.sh "$@"
fi

# Fallback to bundled binary
if [ -x "/app/lib/hyprism/HyPrism" ]; then
  exec /app/lib/hyprism/HyPrism "$@"
fi

# Last-resort: fail with message
echo "HyPrism launcher not available inside bundle" >&2
exit 1
