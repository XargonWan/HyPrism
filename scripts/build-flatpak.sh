#!/bin/bash
# Build Flatpak for HyPrism
# Requires: flatpak-builder, flatpak

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "Building HyPrism Flatpak..."

# Check if required tools are installed
if ! command -v flatpak-builder &> /dev/null; then
    echo "Error: flatpak-builder is not installed"
    echo "Install with: sudo apt install flatpak-builder (Debian/Ubuntu)"
    echo "           or: sudo dnf install flatpak-builder (Fedora)"
    exit 1
fi

# Ensure required Flatpak runtimes are installed
echo "Installing required Flatpak runtimes..."
flatpak install -y flathub org.gnome.Platform//46 org.gnome.Sdk//46 || true
flatpak install -y flathub org.freedesktop.Sdk.Extension.golang//23.08 || true
flatpak install -y flathub org.freedesktop.Sdk.Extension.node18//23.08 || true

# Build the Flatpak
cd "$PROJECT_DIR"
flatpak-builder \
    --force-clean \
    --repo=flatpak-repo \
    --state-dir=.flatpak-builder \
    flatpak-build \
    flatpak/dev.hyprism.HyPrism.json

# Create a bundle for distribution
echo "Creating Flatpak bundle..."
flatpak build-bundle flatpak-repo HyPrism.flatpak dev.hyprism.HyPrism

echo ""
echo "Build complete!"
echo "Flatpak bundle: HyPrism.flatpak"
echo ""
echo "To install locally: flatpak install HyPrism.flatpak"
echo "To run: flatpak run dev.hyprism.HyPrism"
