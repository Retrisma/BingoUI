#!/bin/bash -e

dotnet build
VERSION=$(cat everest.yaml | grep '^  Version' | cut -d' ' -f 4)
mkdir -p dist
FILENAME=dist/BingoUI_${VERSION}${2}.zip
rm -f $FILENAME
cd BingoUI/bin/${1-Debug}
zip -r ../../../${FILENAME} *
echo Finished in ${FILENAME}
