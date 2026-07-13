#!/usr/bin/env sh
set -eu

APP_ID="imageviewer.desktop"
APP_NAME="Image Viewer"
ICON_NAME="imageviewer"
TEMPLATE_PATH="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/imageviewer.desktop.template"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
APPLICATIONS_DIR="$DATA_HOME/applications"
DESKTOP_FILE="$APPLICATIONS_DIR/$APP_ID"
MIME_TYPES="image/jpeg image/png image/bmp image/gif image/webp image/tiff"
APPLY=0
UNINSTALL=0
SET_DEFAULTS=0
EXECUTABLE_PATH=""
ICON_VALUE="$ICON_NAME"

usage() {
cat <<'USAGE'
Usage:
  install-imageviewer-integration.sh --executable /path/to/ImageViewer [--icon imageviewer] [--set-defaults] [--apply]
  install-imageviewer-integration.sh --uninstall [--apply]

Safe by default:
  Without --apply this script only prints planned actions and writes no persistent user or system files.

Options:
  --executable PATH  Published ImageViewer executable path used by the desktop Exec line.
  --icon NAME        Icon name or absolute icon path. Defaults to imageviewer.
  --set-defaults     Set Image Viewer as the default handler for supported image MIME types.
  --apply            Apply user-level XDG changes.
  --uninstall        Remove the generated user-level desktop entry; choose a new default app manually if needed.
  -h, --help         Show this help.
USAGE
}

shell_quote() {
printf "%s" "$1" | sed "s/'/'\\''/g; s/^/'/; s/$/'/"
}

desktop_exec_escape() {
printf "%s" "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/%/%%/g'
}

sed_replacement_escape() {
printf "%s" "$1" | sed 's/[&|\\]/\\&/g'
}

plan() {
printf '[plan] %s\n' "$1"
}

require_command_if_apply() {
if [ "$APPLY" -eq 1 ] && ! command -v "$1" >/dev/null 2>&1; then
printf 'Required command not found: %s\n' "$1" >&2
exit 1
fi
}

while [ "$#" -gt 0 ]; do
case "$1" in
--executable)
[ "$#" -ge 2 ] || { printf 'Missing value for --executable\n' >&2; exit 1; }
EXECUTABLE_PATH="$2"
shift 2
;;
--icon)
[ "$#" -ge 2 ] || { printf 'Missing value for --icon\n' >&2; exit 1; }
ICON_VALUE="$2"
shift 2
;;
--set-defaults)
SET_DEFAULTS=1
shift
;;
--apply)
APPLY=1
shift
;;
--uninstall)
UNINSTALL=1
shift
;;
-h|--help)
usage
exit 0
;;
*)
printf 'Unknown option: %s\n' "$1" >&2
usage >&2
exit 1
;;
esac
done

if [ "$APPLY" -ne 1 ]; then
printf 'Dry run only. Re-run with --apply to change user-level XDG files and MIME defaults.\n' >&2
fi

remove_integration() {
plan "Remove desktop file $DESKTOP_FILE"
if [ "$APPLY" -eq 1 ]; then
rm -f "$DESKTOP_FILE"
fi

plan "Desktop file removal does not reset MIME defaults; choose a replacement default manually if needed"
for mime_type in $MIME_TYPES; do
if [ "$APPLY" -eq 1 ] && command -v xdg-mime >/dev/null 2>&1; then
current_default="$(xdg-mime query default "$mime_type" 2>/dev/null || true)"
if [ "$current_default" = "$APP_ID" ]; then
printf '%s remains the recorded default for %s until another application is selected.\n' "$APP_ID" "$mime_type" >&2
fi
fi
done

if [ "$APPLY" -eq 1 ] && command -v update-desktop-database >/dev/null 2>&1; then
update-desktop-database "$APPLICATIONS_DIR" >/dev/null 2>&1 || true
fi
}

install_integration() {
[ -n "$EXECUTABLE_PATH" ] || { printf '--executable is required unless --uninstall is used.\n' >&2; exit 1; }
[ -f "$EXECUTABLE_PATH" ] || { printf 'Executable does not exist: %s\n' "$EXECUTABLE_PATH" >&2; exit 1; }
[ -f "$TEMPLATE_PATH" ] || { printf 'Template does not exist: %s\n' "$TEMPLATE_PATH" >&2; exit 1; }

require_command_if_apply desktop-file-install

escaped_exec="$(desktop_exec_escape "$EXECUTABLE_PATH")"
escaped_exec="$(sed_replacement_escape "$escaped_exec")"
escaped_icon="$(desktop_exec_escape "$ICON_VALUE")"
escaped_icon="$(sed_replacement_escape "$escaped_icon")"
temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/imageviewer.desktop.XXXXXX")"
temp_desktop="$temp_dir/$APP_ID"
trap 'rm -rf "$temp_dir"' EXIT HUP INT TERM

sed \
-e "s|{{EXECUTABLE_PATH}}|$escaped_exec|g" \
-e "s|{{ICON_NAME}}|$escaped_icon|g" \
"$TEMPLATE_PATH" > "$temp_desktop"
chmod 600 "$temp_desktop"

plan "Generate desktop file from $TEMPLATE_PATH"
plan "Install desktop file to $DESKTOP_FILE"
if [ "$APPLY" -eq 1 ]; then
mkdir -p "$APPLICATIONS_DIR"
desktop-file-install --dir="$APPLICATIONS_DIR" --set-key=Name --set-value="$APP_NAME" "$temp_desktop"
fi

if command -v desktop-file-validate >/dev/null 2>&1; then
plan "Validate generated desktop file with desktop-file-validate"
if [ "$APPLY" -eq 1 ]; then
desktop-file-validate "$DESKTOP_FILE"
else
desktop-file-validate "$temp_desktop"
fi
fi

if [ "$APPLY" -eq 1 ] && command -v update-desktop-database >/dev/null 2>&1; then
plan "Update desktop database in $APPLICATIONS_DIR"
update-desktop-database "$APPLICATIONS_DIR" >/dev/null 2>&1 || true
else
plan "Optionally run update-desktop-database $(shell_quote "$APPLICATIONS_DIR")"
fi

if [ "$SET_DEFAULTS" -ne 1 ]; then
plan "Skip MIME default changes; pass --set-defaults to make $APP_ID the default image handler"
return
fi

for mime_type in $MIME_TYPES; do
plan "Set $APP_ID as default for $mime_type using xdg-mime"
if [ "$APPLY" -eq 1 ]; then
if command -v xdg-mime >/dev/null 2>&1; then
xdg-mime default "$APP_ID" "$mime_type"
else
printf 'xdg-mime not found; skipping default for %s\n' "$mime_type" >&2
fi
fi
done
}

if [ "$UNINSTALL" -eq 1 ]; then
remove_integration
else
install_integration
fi
