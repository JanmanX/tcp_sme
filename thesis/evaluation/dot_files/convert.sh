#!/usr/bin/env bash
dotconvert () {
    if [[ ! -e "$1.png" ]]
    then
        echo "Doing $1"
        file=$1
        #dot -Tpng $1 -o $1.png;
        dot -Teps $1 -o "${file%.dot}.eps"
    fi
}
export -f dotconvert

SHELL=$(type -p bash) find . -name "*dot" | sort -n | parallel dotconvert
