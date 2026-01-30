#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${VERSION:-2.0.0}"
ARTIFACTS="$ROOT/artifacts"
RIDS=("win-x64" "linux-x64" "osx-arm64")
AUTO_INSTALL="${AUTO_INSTALL:-0}"
REMOTE_LINUX_HOST="${REMOTE_LINUX_HOST:-${REMOTE_HOST:-}}"
REMOTE_LINUX_PATH="${REMOTE_LINUX_PATH:-${REMOTE_PATH:-~/HyPrism}}"
SKIP_REMOTE="${SKIP_REMOTE:-0}"

show_help() {
  cat <<'EOF'
Usage: ./scripts/build-all.sh [options]

Options:
  --help                Show this help message and exit
  --auto-install-deps   Auto-install missing dependencies (Ubuntu/Fedora)
  --no-appimage         Skip AppImage packaging
  --no-flatpak          Skip Flatpak packaging
  --no-deb              Skip deb/rpm packaging
  --only-appimage       Build only AppImage (skip deb/rpm and Flatpak)
  --only-flatpak        Build only Flatpak (skip deb/rpm and AppImage)
  --only-deb            Build only deb/rpm (skip Flatpak and AppImage)
EOF
}

# Packaging flags (Linux-only packaging step)
DO_APPIMAGE=1
DO_FLATPAK=1
DO_DEB=1

for arg in "$@"; do
  case "$arg" in
    --help|-h)
      show_help
      exit 0
      ;;
    --auto-install-deps)
      AUTO_INSTALL=1
      ;;
    --no-appimage)
      DO_APPIMAGE=0
      ;;
    --no-flatpak)
      DO_FLATPAK=0
      ;;
    --no-deb)
      DO_DEB=0
      ;;
    --only-appimage)
      DO_APPIMAGE=1
      DO_FLATPAK=0
      DO_DEB=0
      ;;
    --only-flatpak)
      DO_APPIMAGE=0
      DO_FLATPAK=1
      DO_DEB=0
      ;;
    --only-deb)
      DO_APPIMAGE=0
      DO_FLATPAK=0
      DO_DEB=1
      ;;
    *)
      ;;
  esac
done

if [[ "$(uname -s)" == "Linux" ]]; then
  detect_distro() {
    if [[ -f /etc/os-release ]]; then
      . /etc/os-release
      echo "${ID:-}"
      return 0
    fi
    echo ""
  }

  install_packages() {
    local ubuntu_pkgs="$1"
    local fedora_pkgs="$2"
    local distro
    distro="$(detect_distro)"

    case "$distro" in
      ubuntu|debian)
        sudo apt-get update -y && sudo apt-get install -y $ubuntu_pkgs || true
        ;;
      fedora)
        sudo dnf install -y $fedora_pkgs || true
        ;;
      *)
        return 1
        ;;
    esac
  }

  ensure_tool() {
    local cmd="$1" ubuntu_pkgs="$2" fedora_pkgs="$3" post_install="$4"
    if command -v "$cmd" >/dev/null 2>&1; then
      return 0
    fi

    if [[ "$AUTO_INSTALL" == "1" ]]; then
      echo "==> Installing dependencies for $cmd"
      if ! install_packages "$ubuntu_pkgs" "$fedora_pkgs"; then
        echo "!! Unsupported distro for auto-install. Please install: $ubuntu_pkgs / $fedora_pkgs"
      fi
      if [[ -n "$post_install" ]]; then
        eval "$post_install" || true
      fi
    else
      echo "!! Missing $cmd. Install: $ubuntu_pkgs (Ubuntu) or $fedora_pkgs (Fedora). Use --auto-install-deps to auto-install."
    fi
  }

  # Packaging helpers often missing on fresh VMs
  ensure_tool fpm "ruby ruby-dev rubygems build-essential rpm" "ruby ruby-devel rubygems @development-tools rpm-build" "command -v fpm >/dev/null 2>&1 || sudo gem install --no-document fpm"
  ensure_tool appimagetool "appimagetool" "appimagetool" ""
  ensure_tool flatpak-builder "flatpak flatpak-builder flatpak-builder-libs" "flatpak flatpak-builder appstream-util desktop-file-utils" ""
fi

mkdir -p "$ARTIFACTS"

echo "==> Building frontend"
if [[ -f "$ROOT/frontend/package-lock.json" ]]; then
  (cd "$ROOT/frontend" && npm ci)
else
  (cd "$ROOT/frontend" && npm install)
fi
(cd "$ROOT/frontend" && npm run build)

echo "==> Restoring and publishing backend"
dotnet restore "$ROOT/HyPrism.csproj"

for rid in "${RIDS[@]}"; do
  out="$ARTIFACTS/$rid/portable"
  echo "--> Publishing $rid to $out"
  dotnet publish "$ROOT/HyPrism.csproj" -c Release -r "$rid" --self-contained true \
    /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$out"

  case "$rid" in
    win-*)
      (cd "$out" && zip -9 -r "$ARTIFACTS/HyPrism-${rid}-portable.zip" .)
      ;;
    osx-*)
      (cd "$out" && zip -9 -r "$ARTIFACTS/HyPrism-${rid}-portable.zip" .)
      ;;
    linux-*)
      tar -czf "$ARTIFACTS/HyPrism-${rid}-portable.tar.gz" -C "$out" .
      ;;
  esac
done

# Linux-only packaging helpers
if [[ "$(uname -s)" == "Linux" ]]; then
  LINUX_OUT="$ARTIFACTS/linux-x64/portable"
  PKGROOT="$ARTIFACTS/linux-x64/pkgroot"
  ICON_SRC="$ROOT/packaging/flatpak/dev.hyprism.HyPrism.png"
  DESKTOP_SRC="$ROOT/packaging/flatpak/dev.hyprism.HyPrism.desktop"

  echo "==> Staging Linux package tree"
  rm -rf "$PKGROOT"
  mkdir -p "$PKGROOT/opt/hyprism" "$PKGROOT/usr/bin" "$PKGROOT/usr/share/applications" "$PKGROOT/usr/share/icons/hicolor/256x256/apps"
  cp -R "$LINUX_OUT"/* "$PKGROOT/opt/hyprism/"
  ln -sf /opt/hyprism/HyPrism "$PKGROOT/usr/bin/hyprism"
  [[ -f "$DESKTOP_SRC" ]] && cp "$DESKTOP_SRC" "$PKGROOT/usr/share/applications/dev.hyprism.HyPrism.desktop"
  [[ -f "$ICON_SRC" ]] && cp "$ICON_SRC" "$PKGROOT/usr/share/icons/hicolor/256x256/apps/dev.hyprism.HyPrism.png"

  if [[ "$DO_DEB" == "1" ]] && command -v fpm >/dev/null 2>&1; then
    echo "==> Building .deb and .rpm via fpm"
    fpm -s dir -t deb -n hyprism -v "$VERSION" -C "$PKGROOT" \
      --description "HyPrism launcher" --url "https://github.com/yyyumeniku/HyPrism" \
      -p "$ARTIFACTS/HyPrism-linux-x64.deb" .
    fpm -s dir -t rpm -n hyprism -v "$VERSION" -C "$PKGROOT" \
      --description "HyPrism launcher" --url "https://github.com/yyyumeniku/HyPrism" \
      -p "$ARTIFACTS/HyPrism-linux-x64.rpm" .
  else
    if [[ "$DO_DEB" == "1" ]]; then
      echo "!! fpm not found; skipping deb/rpm generation"
    else
      echo "==> Skipping deb/rpm generation (--no-deb/--only-*)"
    fi
  fi

  if [[ "$DO_APPIMAGE" == "1" ]] && command -v appimagetool >/dev/null 2>&1; then
    echo "==> Building AppImage"
    APPDIR="$ARTIFACTS/linux-x64/AppDir"
    rm -rf "$APPDIR"
    mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"
    cp "$LINUX_OUT/HyPrism" "$APPDIR/usr/bin/HyPrism"
    chmod +x "$APPDIR/usr/bin/HyPrism"
    [[ -f "$DESKTOP_SRC" ]] && cp "$DESKTOP_SRC" "$APPDIR/usr/share/applications/dev.hyprism.HyPrism.desktop"
    [[ -f "$ICON_SRC" ]] && cp "$ICON_SRC" "$APPDIR/usr/share/icons/hicolor/256x256/apps/dev.hyprism.HyPrism.png"
    cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
exec "$(dirname "$0")/usr/bin/HyPrism" "$@"
EOF
    chmod +x "$APPDIR/AppRun"
    (cd "$APPDIR" && ln -sf usr/share/applications/dev.hyprism.HyPrism.desktop HyPrism.desktop)
    appimagetool "$APPDIR" "$ARTIFACTS/HyPrism-linux-x64.AppImage"
  else
    if [[ "$DO_APPIMAGE" == "1" ]]; then
      echo "!! appimagetool not found; skipping AppImage"
    else
      echo "==> Skipping AppImage (--no-appimage/--only-*)"
    fi
  fi

  if [[ "$DO_FLATPAK" == "1" ]] && command -v flatpak-builder >/dev/null 2>&1; then
    echo "==> Building Flatpak"
    FLATPAK_STAGE="$ARTIFACTS/linux-x64/flatpak-build"
    FLATPAK_REPO="$ARTIFACTS/flatpak-repo"
    flatpak remote-add --user --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
    flatpak remote-add --user --if-not-exists flathub-beta https://flathub.org/beta-repo/flathub-beta.flatpakrepo
    rm -rf "$ROOT/packaging/flatpak/bundle"
    mkdir -p "$ROOT/packaging/flatpak/bundle"
    cp -R "$LINUX_OUT"/* "$ROOT/packaging/flatpak/bundle/"
    cp "$ROOT/packaging/flatpak/dev.hyprism.HyPrism."* "$ROOT/packaging/flatpak/bundle/" || true
    chmod +x "$ROOT/packaging/flatpak/bundle/HyPrism" || true
    flatpak-builder --force-clean "$FLATPAK_STAGE" \
      --install-deps-from=flathub --install-deps-from=flathub-beta \
      "$ROOT/packaging/flatpak/dev.hyprism.HyPrism.json" --repo="$FLATPAK_REPO"
  else
    if [[ "$DO_FLATPAK" == "1" ]]; then
      echo "!! flatpak-builder not found; skipping Flatpak"
    else
      echo "==> Skipping Flatpak (--no-flatpak/--only-*)"
    fi
  fi
fi

# Optional: trigger remote Linux build (e.g., Parallels VM) after local steps
if [[ -n "$REMOTE_LINUX_HOST" && "$SKIP_REMOTE" != "1" ]]; then
  echo "==> Triggering remote build on $REMOTE_LINUX_HOST ($REMOTE_LINUX_PATH)"
  ssh "$REMOTE_LINUX_HOST" "cd '$REMOTE_LINUX_PATH' && SKIP_REMOTE=1 ./scripts/build-all.sh --auto-install-deps" || {
    echo "!! Remote build failed on $REMOTE_LINUX_HOST" >&2
    exit 1
  }
fi

echo "Done. Artifacts in $ARTIFACTS"
