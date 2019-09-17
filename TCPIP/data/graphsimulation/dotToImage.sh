#!/usr/bin/env bash
if [ -z "$1" ]; then
    echo "Supply path argument"
    exit 1
fi

dotconvert () {
    if [[ ! -e "$1.png" ]]
    then
        echo "Doing $1"
        dot -Tpng $1 -o $1.png;
        dot -Teps $1 -o $1.eps;
    fi
}

pushd $1;
export -f dotconvert

if [[ -e "out.mp4" ]]
then
    rm out.mp4
fi

SHELL=$(type -p bash) find . -name "*dot" | sort -n | parallel dotconvert
ffmpeg -r 10 -i %*.dot.png -r 10 -vf "pad=ceil(iw/2)*2:ceil(ih/2)*2:0:0:white" out.mp4;

#rm -- *.dot
#rm -- *.dot.png

popd;