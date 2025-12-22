#!/bin/bash

workshop() {
    if command -v steamcmd >/dev/null 2>&1; then
        echo "steamcmd is installed."
    else
        echo "steamcmd is not installed."
        exit 1
    fi

    workshopItemsFile="RequiredWorkshopItems.txt"
    workshopItems=""

    if [ -f "$workshopItemsFile" ]; then
        while IFS= read -r line; do
            if [ -n "$line" ]; then
                workshopItems="$workshopItems +workshop_download_item 294100 $line"
            fi
        done < "$workshopItemsFile"
        echo "Workshop items: '$workshopItems'"
    else
        echo "RequiredWorkshopItems.txt file not found. Nothing to download."
        return
    fi

    # Set the download location to Workshop
    scriptDirectory="$(dirname "$(readlink -f "$0")")"
    downloadPath="$scriptDirectory/Workshop"
    if [ ! -d "$downloadPath" ]; then
        mkdir -p "$downloadPath"
    fi

    # Download workshop items for appid 294100
    steamcmd +force_install_dir "$downloadPath" +login anonymous $workshopItems +quit

    if [ $? -ne 0 ]; then
        echo "Failed to download workshop items."
        exit 1
    fi

    echo "Workshop items downloaded successfully."
}

# Call the function
workshop

echo "Setup complete. Solution should be buildable now."