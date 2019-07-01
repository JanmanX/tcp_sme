#!/usr/bin/env bash
if [ -z "$1" ]; then
    echo "Supply path argument"
    exit 1
fi
for filename in "$1"*.dot; do
    [ -e "$filename" ] || continue
    echo $filename
    if [[ ! -e "$filename.png" ]]
    then
        dot -Tpng $filename -o $filename.png;
    fi

done
pushd $1;
echo ls;
ffmpeg -r 10 -i %08d.dot.png out.mp4
popd;