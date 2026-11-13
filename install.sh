#!/usr/bin/env bash
# ======================================================================
#  7 Days For Kids - installer for Linux (and Steam Deck)
#  Run:  ./install.sh                 (finds the game in your Steam libraries)
#   or:  ./install.sh "/path/to/7 Days To Die"
#  It copies the mod into the game's Mods folder. Only Mods/7DaysForKids
#  is touched.
# ======================================================================
set -euo pipefail

APP_ID=251570                 # Steam app id of 7 Days to Die
GAME_DIR_NAME='7 Days To Die'
MOD_NAME='7DaysForKids'

step() { printf '\033[36m==> %s\033[0m\n' "$*"; }
warn() { printf '\033[33mWARNING: %s\033[0m\n' "$*"; }
fail() { printf '\033[31mERROR: %s\033[0m\n' "$*" >&2; }

is_game_dir() {
    [ -n "${1:-}" ] && [ -d "$1/7DaysToDie_Data" ] && [ -d "$1/Mods" ]
}

steam_roots() {
    local r
    for r in \
        "$HOME/.local/share/Steam" \
        "$HOME/.steam/steam" \
        "$HOME/.steam/root" \
        "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam" \
        "$HOME/snap/steam/common/.local/share/Steam"; do
        [ -d "$r" ] && printf '%s\n' "$r"
    done
}

steam_libraries() {
    local root vdf
    while IFS= read -r root; do
        printf '%s\n' "$root"
        vdf="$root/steamapps/libraryfolders.vdf"
        if [ -f "$vdf" ]; then
            sed -n 's/^[[:space:]]*"path"[[:space:]]*"\(.*\)".*/\1/p' "$vdf"
        fi
    done < <(steam_roots)
}

find_game_dir() {
    local lib manifest installdir c
    while IFS= read -r lib; do
        manifest="$lib/steamapps/appmanifest_$APP_ID.acf"
        if [ -f "$manifest" ]; then
            installdir=$(sed -n 's/^[[:space:]]*"installdir"[[:space:]]*"\(.*\)".*/\1/p' "$manifest" | head -n1)
            c="$lib/steamapps/common/$installdir"
            if [ -n "$installdir" ] && is_game_dir "$c"; then
                (cd "$c" && pwd -P); return 0
            fi
        fi
        c="$lib/steamapps/common/$GAME_DIR_NAME"
        if is_game_dir "$c"; then
            (cd "$c" && pwd -P); return 0
        fi
    done < <(steam_libraries | awk '!seen[$0]++')
    return 1
}

# ---------------------------------------------------------------------
echo
printf '\033[32m7 Days For Kids - installer (Linux)\033[0m\n'
echo

# Mod source (next to this script)
REPO_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)
MOD_SRC="$REPO_DIR/Mods/$MOD_NAME"
if [ ! -f "$MOD_SRC/ModInfo.xml" ]; then
    fail "Mod files not found at: $MOD_SRC"
    echo 'Keep install.sh in the same folder as the "Mods" folder (extract the whole download first).'
    exit 1
fi
if [ ! -f "$MOD_SRC/$MOD_NAME.dll" ]; then
    fail "$MOD_NAME.dll is missing in $MOD_SRC"
    exit 1
fi

# Game folder
GAME_DIR=''
if [ -n "${1:-}" ]; then
    if is_game_dir "$1"; then
        GAME_DIR=$1
    else
        fail "That folder does not look like a 7 Days to Die install: $1"
        exit 1
    fi
else
    step 'Looking for 7 Days to Die...'
    if FOUND=$(find_game_dir); then
        echo "Found: $FOUND"
        read -r -p 'Install the mod here? [Y/n] ' ans || ans=''
        case "$ans" in ''|[YySs]*) GAME_DIR=$FOUND ;; esac
    else
        warn 'Could not find the game automatically.'
    fi
fi

while [ -z "$GAME_DIR" ]; do
    echo
    echo 'Please type (or paste) your 7 Days to Die folder.'
    echo "It is usually:  ~/.local/share/Steam/steamapps/common/$GAME_DIR_NAME"
    echo 'Tip: in Steam, right-click the game > Manage > Browse local files.'
    read -r -p 'Game folder (empty to cancel): ' typed || typed=''
    typed=${typed%\"}; typed=${typed#\"}
    typed=${typed/#\~/$HOME}
    if [ -z "$typed" ]; then echo 'Cancelled.'; exit 1; fi
    if is_game_dir "$typed"; then GAME_DIR=$typed; else fail "That folder does not look like a 7 Days to Die install: $typed"; fi
done

# Game must be closed
if pgrep -f '7DaysToDie(\.x86_64|\.exe|_EAC)' >/dev/null 2>&1; then
    fail 'The game is running. Close it and run install.sh again.'
    exit 1
fi

if [ ! -d "$GAME_DIR/Mods/0_TFP_Harmony" ]; then
    warn 'Mods/0_TFP_Harmony is missing. The mod needs it. In Steam, use "Verify integrity of game files" to restore it.'
fi

# Copy: make Mods/7DaysForKids identical to the download (only that folder is touched)
DEST="$GAME_DIR/Mods/$MOD_NAME"
step "Copying mod to: $DEST"
if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete \
        --exclude='/Scripts/' --exclude='bin/' --exclude='obj/' \
        --exclude='*.pdb' --exclude='.gitignore' \
        "$MOD_SRC/" "$DEST/"
else
    rm -rf -- "$DEST"
    mkdir -p -- "$DEST"
    (cd "$MOD_SRC" && tar --exclude='./Scripts' --exclude='bin' --exclude='obj' \
        --exclude='*.pdb' --exclude='.gitignore' -cf - .) | (cd "$DEST" && tar -xf -)
fi

echo
printf '\033[32mDone! The mod is installed.\033[0m\n'
echo 'IMPORTANT: start the game with EasyAntiCheat (EAC) turned OFF, or the mod will not load.'
echo "To uninstall, delete: $DEST"
